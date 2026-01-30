#!/usr/bin/env python3
"""
[LEGACY] Runway keyframe stitching (all-in-one).
Prefer: runway_generate_segments.py (generate) + runway_stitch.py (stitch).
See runway_generate_segments.py for --skip-existing, --audio, general_prompt, start-only segment.

Runway keyframe stitching: reads a JSON with prompts and durations,
generates video segments from Runway (first/last frame) in parallel,
then stitches them with FFmpeg.

Usage:
  python runway_keyframe_stitch.py <config.json> [--output final.mp4] [--max-workers N]
  set RUNWAYML_API_SECRET in env (or .env file) for Runway API auth.

JSON format:
  {
    "image_dir": "path/to/images",   // optional, default "."
    "images": ["a.png", "b.png"],    // ordered list for index-based segments
    "segments": [
      { "prompt": "...", "duration": 5, "start_index": 0, "end_index": 1 }
      // or: "start_image": "a.png", "end_image": "b.png"
    ],
    "model": "gen4_turbo",           // optional
    "ratio": "16:9"                  // optional, e.g. "16:9", "9:16"
  }
"""

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

import requests

# Optional: load .env for RUNWAYML_API_SECRET
try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass

try:
    from runwayml import RunwayML
    from runwayml.exceptions import TaskFailedError
except ImportError:
    print("Install the Runway SDK: pip install runwayml", file=sys.stderr)
    sys.exit(1)


def resolve_config_path(config_path: str, base_dir: Path) -> Path:
    """Resolve image_dir and file paths relative to config file dir or base_dir."""
    return base_dir


def load_config(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def get_image_path(segment: dict, index: int, images_list: list, image_dir: Path) -> Path:
    """Get filesystem path for start or end image of a segment (by index or by filename)."""
    if "start_index" in segment or "end_index" in segment:
        key = "start_index" if index == 0 else "end_index"
        i = segment[key]
        name = images_list[i]
    else:
        key = "start_image" if index == 0 else "end_image"
        name = segment[key]
    return image_dir / name


def collect_unique_images(config: dict, config_dir: Path) -> list[tuple[Path, str]]:
    """Return list of (absolute_path, key) for each unique image used in segments."""
    image_dir = config_dir / config.get("image_dir", ".")
    images_list = config.get("images", [])
    segments = config.get("segments", [])
    seen = set()
    out = []
    for seg in segments:
        for idx in (0, 1):
            if "start_index" in seg or "end_index" in seg:
                key = "start_index" if idx == 0 else "end_index"
                i = seg["start_index"] if idx == 0 else seg["end_index"]
                name = images_list[i]
            else:
                name = seg["start_image"] if idx == 0 else seg["end_image"]
            path = image_dir / name
            path_str = str(path.resolve())
            if path_str not in seen:
                seen.add(path_str)
                out.append((path.resolve(), path_str))
    return out


def upload_image(client: RunwayML, path: Path) -> str:
    """Upload one image to Runway; return runway URI."""
    r = client.uploads.create_ephemeral(file=path)
    return r.uri


def upload_all_images(client: RunwayML, unique_images: list[tuple[Path, str]], max_workers: int) -> dict[str, str]:
    """Upload unique images in parallel. Return mapping path_key -> runway uri."""
    path_to_uri = {}
    with ThreadPoolExecutor(max_workers=max_workers) as ex:
        future_to_key = {
            ex.submit(upload_image, client, p): k
            for p, k in unique_images
        }
        for future in as_completed(future_to_key):
            key = future_to_key[future]
            try:
                uri = future.result()
                path_to_uri[key] = uri
            except Exception as e:
                raise RuntimeError(f"Failed to upload image {key}: {e}") from e
    return path_to_uri


def image_path_to_key(path: Path) -> str:
    return str(path.resolve())


def get_segment_uris(segment: dict, config: dict, config_dir: Path, path_to_uri: dict) -> tuple[str, str]:
    """Return (first_frame_uri, last_frame_uri) for a segment."""
    image_dir = config_dir / config.get("image_dir", ".")
    images_list = config.get("images", [])
    start_path = get_image_path(segment, 0, images_list, image_dir).resolve()
    end_path = get_image_path(segment, 1, images_list, image_dir).resolve()
    start_uri = path_to_uri.get(image_path_to_key(start_path))
    end_uri = path_to_uri.get(image_path_to_key(end_path))
    if not start_uri or not end_uri:
        raise ValueError(f"Missing URI for segment images: {start_path}, {end_path}")
    return start_uri, end_uri


def generate_one_segment(
    client: RunwayML,
    segment: dict,
    config: dict,
    config_dir: Path,
    path_to_uri: dict,
    segment_index: int,
) -> str:
    """Create Runway task for one segment; wait for completion; return first output URL."""
    model = config.get("model", "gen4_turbo")
    ratio = config.get("ratio", "16:9")
    prompt_text = segment["prompt"]
    duration = int(segment.get("duration", 5))
    first_uri, last_uri = get_segment_uris(segment, config, config_dir, path_to_uri)

    prompt_image = [
        {"uri": first_uri, "position": "first"},
        {"uri": last_uri, "position": "last"},
    ]

    task = client.image_to_video.create(
        model=model,
        prompt_image=prompt_image,
        prompt_text=prompt_text,
        ratio=ratio,
        duration=duration,
    )
    result = task.wait_for_task_output()
    if getattr(result, "status", None) == "FAILED":
        raise TaskFailedError(result, getattr(result, "failure_reason", "Unknown"))
    outputs = getattr(result, "output", None) or []
    if not outputs:
        raise RuntimeError(f"Segment {segment_index}: no output URLs")
    return outputs[0]


def download_video(url: str, path: Path) -> None:
    r = requests.get(url, stream=True, timeout=60)
    r.raise_for_status()
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
) -> Path:
    """Generate one segment and download to out_path. Returns out_path."""
    url = generate_one_segment(client, segment, config, config_dir, path_to_uri, segment_index)
    download_video(url, out_path)
    return out_path


def concat_videos(ordered_paths: list[Path], output_path: Path) -> None:
    """Concatenate videos with FFmpeg (concat demuxer)."""
    if not ordered_paths:
        raise ValueError("No videos to concat")
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise FileNotFoundError("ffmpeg not found in PATH. Install FFmpeg to stitch videos.")
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False, encoding="utf-8") as f:
        for p in ordered_paths:
            # FFmpeg concat expects paths escaped for the list file
            line = f"file '{p.resolve().as_posix()}'\n"
            f.write(line)
        list_path = f.name
    try:
        cmd = [
            ffmpeg,
            "-y",
            "-f", "concat",
            "-safe", "0",
            "-i", list_path,
            "-c", "copy",
            str(output_path.resolve()),
        ]
        subprocess.run(cmd, check=True)
    finally:
        os.unlink(list_path)


def main() -> None:
    parser = argparse.ArgumentParser(description="Runway keyframe stitching from JSON config.")
    parser.add_argument("config", help="Path to JSON config (segments, prompts, durations, images)")
    parser.add_argument("--output", "-o", default="stitched_output.mp4", help="Final stitched video path")
    parser.add_argument("--max-workers", type=int, default=3, help="Max parallel Runway tasks (default 3)")
    parser.add_argument("--skip-stitch", action="store_true", help="Only generate segments, do not stitch")
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

    segments = config.get("segments", [])
    if not segments:
        print("No segments in config.", file=sys.stderr)
        sys.exit(1)

    client = RunwayML()

    # 1) Collect and upload unique images in parallel
    unique_images = collect_unique_images(config, config_dir)
    for path, _ in unique_images:
        if not path.is_file():
            print(f"Image not found: {path}", file=sys.stderr)
            sys.exit(1)
    print(f"Uploading {len(unique_images)} unique images...")
    path_to_uri = upload_all_images(client, unique_images, max_workers=min(args.max_workers, 8))
    print("Uploads done.")

    # 2) Generate segments in parallel
    output_dir = Path(tempfile.mkdtemp(prefix="runway_stitch_"))
    segment_paths: list[Path] = [output_dir / f"seg_{i:03d}.mp4" for i in range(len(segments))]
    max_workers = max(1, args.max_workers)
    errors = []
    with ThreadPoolExecutor(max_workers=max_workers) as ex:
        futures = {
            ex.submit(
                run_segment_and_download,
                client,
                seg,
                config,
                config_dir,
                path_to_uri,
                i,
                segment_paths[i],
            ): i
            for i, seg in enumerate(segments)
        }
        for future in as_completed(futures):
            idx = futures[future]
            try:
                future.result()
                print(f"Segment {idx + 1}/{len(segments)} done.")
            except Exception as e:
                errors.append((idx, e))
                print(f"Segment {idx + 1} failed: {e}", file=sys.stderr)
    if errors:
        print(f"{len(errors)} segment(s) failed. Aborting.", file=sys.stderr)
        shutil.rmtree(output_dir, ignore_errors=True)
        sys.exit(1)

    if args.skip_stitch:
        print(f"Segments saved in {output_dir}")
        return

    # 3) Stitch with FFmpeg
    valid_paths = [p for p in segment_paths if p.is_file()]
    if len(valid_paths) != len(segments):
        print("Some segment files missing.", file=sys.stderr)
        sys.exit(1)
    out_path = Path(args.output).resolve()
    print(f"Stitching {len(valid_paths)} clips to {out_path}...")
    concat_videos(valid_paths, out_path)
    shutil.rmtree(output_dir, ignore_errors=True)
    print("Done.")


if __name__ == "__main__":
    main()
