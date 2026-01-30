#!/usr/bin/env python3
"""
Generate Runway video segments from a keyframe JSON config.
Writes seg_000.mp4, seg_001.mp4, ... to an output folder. Use runway_stitch.py to concat.

Usage:
  python runway_generate_segments.py <config.json> [--output-dir DIR] [--no-skip-existing] [--audio] [--max-workers N]
  set RUNWAYML_API_SECRET in env (or .env) for Runway API auth.

Flags:
  --skip-existing (default): skip generating a segment if its output file already exists.
  --no-skip-existing: regenerate all segments.
  --audio (default off): request audio in generated clips (if supported by model).
"""

import argparse
import os
import sys
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

import requests

try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass

try:
    from runwayml import RunwayML, TaskFailedError
except ImportError:
    print("Install the Runway SDK: pip install runwayml", file=sys.stderr)
    sys.exit(1)

sys.path.insert(0, str(Path(__file__).resolve().parent))
from _runway_common import (
    MODEL_ALLOWED_DURATIONS,
    MODEL_FIRST_FRAME_ONLY,
    build_segment_prompt,
    collect_unique_images,
    get_image_path,
    has_end_frame,
    image_path_to_key,
    load_config,
    validate_config,
    validate_model_support,
)


def upload_image(client: RunwayML, path: Path) -> str:
    r = client.uploads.create_ephemeral(file=path)
    return r.uri


def upload_all_images(
    client: RunwayML,
    unique_images: list[tuple[Path, str]],
    max_workers: int,
    on_progress=None,
) -> dict[str, str]:
    path_to_uri = {}
    total = len(unique_images)
    with ThreadPoolExecutor(max_workers=max_workers) as ex:
        future_to_key = {
            ex.submit(upload_image, client, p): (k, i)
            for i, (p, k) in enumerate(unique_images)
        }
        done = 0
        for future in as_completed(future_to_key):
            key, idx = future_to_key[future]
            try:
                uri = future.result()
                path_to_uri[key] = uri
                done += 1
                if on_progress:
                    on_progress(done, total, "upload")
            except Exception as e:
                raise RuntimeError(f"Failed to upload image {key}: {e}") from e
    return path_to_uri


def get_segment_uris(
    segment: dict,
    config: dict,
    config_dir: Path,
    path_to_uri: dict,
) -> tuple[str, str | None]:
    """Return (first_frame_uri, last_frame_uri or None for start-only)."""
    image_dir = config_dir / config.get("image_dir", ".")
    images_list = config.get("images", [])
    start_path = get_image_path(segment, 0, images_list, image_dir).resolve()
    start_uri = path_to_uri.get(image_path_to_key(start_path))
    if not start_uri:
        raise ValueError(f"Missing URI for start image: {start_path}")
    if not has_end_frame(segment):
        return start_uri, None
    end_path = get_image_path(segment, 1, images_list, image_dir).resolve()
    end_uri = path_to_uri.get(image_path_to_key(end_path))
    if not end_uri:
        raise ValueError(f"Missing URI for end image: {end_path}")
    return start_uri, end_uri


def _resolve_duration(segment: dict, model: str) -> int:
    """Return duration in seconds; must match model's allowed set (validation already ran)."""
    d = segment.get("duration", 5)
    sec = max(2, min(10, int(round(float(d)))))
    allowed = MODEL_ALLOWED_DURATIONS.get(model)
    if allowed is not None:
        if sec not in allowed:
            sec = min(allowed, key=lambda x: abs(x - sec))
    return sec


def generate_one_segment(
    client: RunwayML,
    segment: dict,
    config: dict,
    config_dir: Path,
    path_to_uri: dict,
    segment_index: int,
    audio: bool,
) -> str:
    model = config.get("model", "gen4_turbo")
    ratio = config.get("ratio", "1280:720")
    general_prompt = config.get("general_prompt", "").strip() or None
    prompt_text = build_segment_prompt(segment, general_prompt)
    duration = _resolve_duration(segment, model)
    first_uri, last_uri = get_segment_uris(segment, config, config_dir, path_to_uri)

    if last_uri is not None and model in MODEL_FIRST_FRAME_ONLY:
        raise ValueError(
            f"Model '{model}' does not support first+last frame. "
            "Use a model that supports last frame (veo3.1, veo3.1_fast, gen3a_turbo)."
        )
    if last_uri is None:
        prompt_image = [{"uri": first_uri, "position": "first"}]
    else:
        prompt_image = [
            {"uri": first_uri, "position": "first"},
            {"uri": last_uri, "position": "last"},
        ]

    create_kw: dict = {
        "model": model,
        "prompt_image": prompt_image,
        "prompt_text": prompt_text,
        "ratio": ratio,
        "duration": duration,
    }
    if audio:
        create_kw["include_audio"] = True  # if SDK/model supports it

    task = client.image_to_video.create(**create_kw)
    result = task.wait_for_task_output()
    if getattr(result, "status", None) == "FAILED":
        raise TaskFailedError(result, getattr(result, "failure_reason", "Unknown"))
    outputs = getattr(result, "output", None) or []
    if not outputs:
        raise RuntimeError(f"Segment {segment_index}: no output URLs")
    return outputs[0]


def segment_exists(output_dir: Path, index: int) -> bool:
    """True if this segment already has an output file (with or without version suffix)."""
    base = output_dir / f"seg_{index:03d}.mp4"
    if base.is_file():
        return True
    for p in output_dir.glob(f"seg_{index:03d}_v*.mp4"):
        if p.is_file():
            return True
    return False


def get_output_path(output_dir: Path, index: int, use_versioning: bool) -> Path:
    """Path to write segment output. If use_versioning, use seg_N_vK.mp4 (next K)."""
    if not use_versioning:
        return output_dir / f"seg_{index:03d}.mp4"
    existing = list(output_dir.glob(f"seg_{index:03d}_v*.mp4"))
    versions = []
    for p in existing:
        try:
            v = int(p.stem.split("_v")[-1])
            versions.append(v)
        except ValueError:
            pass
    next_v = max(versions, default=0) + 1
    return output_dir / f"seg_{index:03d}_v{next_v}.mp4"


def download_video(url: str, path: Path) -> None:
    r = requests.get(url, stream=True, timeout=60)
    r.raise_for_status()
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "wb") as f:
        for chunk in r.iter_content(chunk_size=8192):
            f.write(chunk)


def run_segment_and_download(
    client: RunwayML,
    segment: dict,
    config: dict,
    config_dir: Path,
    path_to_uri: dict,
    segment_index: int,
    out_path: Path,
    audio: bool,
    total_segments: int,
    start_time: float,
    on_progress=None,
) -> Path:
    if on_progress:
        on_progress(segment_index, total_segments, "start", start_time)
    url = generate_one_segment(
        client, segment, config, config_dir, path_to_uri, segment_index, audio
    )
    if on_progress:
        on_progress(segment_index, total_segments, "downloading", start_time)
    download_video(url, out_path)
    if on_progress:
        on_progress(segment_index, total_segments, "done", start_time)
    return out_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate Runway segments from keyframe config.")
    parser.add_argument("config", help="Path to JSON config")
    parser.add_argument(
        "--output-dir",
        "-o",
        default="runway_segments",
        help="Directory for seg_000.mp4, seg_001.mp4, ... (default: runway_segments)",
    )
    parser.add_argument(
        "--skip-existing",
        action="store_true",
        default=True,
        help="Skip segments that already have an output file (default: on).",
    )
    parser.add_argument(
        "--no-skip-existing",
        action="store_false",
        dest="skip_existing",
        help="Regenerate all segments even if output files exist.",
    )
    parser.add_argument(
        "--audio",
        action="store_true",
        default=False,
        help="Request audio in generated clips (default: off).",
    )
    parser.add_argument("--max-workers", type=int, default=3, help="Max parallel Runway tasks (default 3)")
    args = parser.parse_args()

    if not os.environ.get("RUNWAYML_API_SECRET"):
        print("Set RUNWAYML_API_SECRET in environment (or .env).", file=sys.stderr)
        sys.exit(1)

    config_path = Path(args.config).resolve()
    if not config_path.is_file():
        print(f"Config not found: {config_path}", file=sys.stderr)
        sys.exit(1)
    config_dir = config_path.parent
    config = load_config(str(config_path))

    print("Validating config...")
    errs = validate_config(config, config_dir)
    errs.extend(validate_model_support(config))
    if errs:
        for e in errs:
            print(e, file=sys.stderr)
        sys.exit(1)
    print("Config and model support OK.")

    segments = config.get("segments", [])
    output_dir = Path(args.output_dir).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    use_versioning = not args.skip_existing

    to_run = []
    for i, seg in enumerate(segments):
        if args.skip_existing and segment_exists(output_dir, i):
            print(f"  Segment {i + 1}/{len(segments)} skipped (output already exists).")
            continue
        to_run.append((i, seg))

    if not to_run:
        print("All segments already generated. Use --no-skip-existing to regenerate (writes versioned files).")
        return

    print(f"Segments to generate: {len(to_run)}/{len(segments)}")
    client = RunwayML()
    unique_images = collect_unique_images(config, config_dir)
    for path, _ in unique_images:
        if not path.is_file():
            print(f"Image not found: {path}", file=sys.stderr)
            sys.exit(1)

    def upload_progress(done, total, _):
        print(f"  Uploading images... {done}/{total}", end="\r")

    print("Uploading images...")
    path_to_uri = upload_all_images(
        client, unique_images, max_workers=min(args.max_workers, 8), on_progress=upload_progress
    )
    print(f"  Uploaded {len(unique_images)} images.")

    start_time = time.time()
    max_workers = max(1, args.max_workers)
    errors = []

    def progress_callback(seg_idx, total, phase, start_t):
        elapsed = int(time.time() - start_t)
        if phase == "start":
            print(f"  Segment {seg_idx + 1}/{total}: started (elapsed {elapsed}s)")
        elif phase == "downloading":
            print(f"  Segment {seg_idx + 1}/{total}: Runway done, downloading... ({elapsed}s)")
        elif phase == "done":
            print(f"  Segment {seg_idx + 1}/{total}: complete ({elapsed}s)")

    with ThreadPoolExecutor(max_workers=max_workers) as ex:
        futures = {}
        for idx, seg in to_run:
            out_path = get_output_path(output_dir, idx, use_versioning)
            fut = ex.submit(
                run_segment_and_download,
                client,
                seg,
                config,
                config_dir,
                path_to_uri,
                idx,
                out_path,
                args.audio,
                len(segments),
                start_time,
                progress_callback,
            )
            futures[fut] = idx
        for future in as_completed(futures):
            idx = futures[future]
            try:
                future.result()
            except Exception as e:
                errors.append((idx, e))
                print(f"  Segment {idx + 1}/{len(segments)} failed: {e}", file=sys.stderr)

    if errors:
        print(f"{len(errors)} segment(s) failed.", file=sys.stderr)
        sys.exit(1)
    elapsed = int(time.time() - start_time)
    print(f"All requested segments saved in {output_dir} (total {elapsed}s)")


if __name__ == "__main__":
    main()
