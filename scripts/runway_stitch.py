#!/usr/bin/env python3
"""
Stitch Runway segment videos (seg_000.mp4, seg_001.mp4, ...) into one file with FFmpeg.

Usage:
  python runway_stitch.py <config.json> [--output-dir DIR] [--output FILE] [--audio]

Segment order is taken from config (same number and order as segments in JSON).
By default audio is stripped from the final video; use --audio to keep it.
"""

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from _runway_common import load_config


def concat_videos(
    ordered_paths: list[Path],
    output_path: Path,
    include_audio: bool,
) -> None:
    if not ordered_paths:
        raise ValueError("No videos to concat")
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise FileNotFoundError("ffmpeg not found in PATH. Install FFmpeg to stitch videos.")
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False, encoding="utf-8") as f:
        for p in ordered_paths:
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
        ]
        if not include_audio:
            cmd.extend(["-an"])
        cmd.append(str(output_path.resolve()))
        subprocess.run(cmd, check=True)
    finally:
        Path(list_path).unlink(missing_ok=True)


def main() -> None:
    parser = argparse.ArgumentParser(description="Stitch Runway segment videos into one.")
    parser.add_argument("config", help="Path to same JSON config used for generation (defines segment count/order)")
    parser.add_argument(
        "--output-dir",
        "-d",
        default="runway_segments",
        help="Directory containing seg_000.mp4, seg_001.mp4, ... (default: runway_segments)",
    )
    parser.add_argument(
        "--output",
        "-o",
        default="stitched_output.mp4",
        help="Final stitched video path (default: stitched_output.mp4)",
    )
    parser.add_argument(
        "--audio",
        action="store_true",
        default=False,
        help="Keep audio in final video (default: strip audio)",
    )
    args = parser.parse_args()

    config_path = Path(args.config).resolve()
    if not config_path.is_file():
        print(f"Config not found: {config_path}", file=sys.stderr)
        sys.exit(1)
    config = load_config(str(config_path))
    segments = config.get("segments", [])
    if not segments:
        print("No segments in config.", file=sys.stderr)
        sys.exit(1)

    output_dir = Path(args.output_dir).resolve()

    def resolve_segment_path(index: int) -> Path | None:
        """Prefer seg_N.mp4; else latest seg_N_vK.mp4."""
        base = output_dir / f"seg_{index:03d}.mp4"
        if base.is_file():
            return base
        versioned = list(output_dir.glob(f"seg_{index:03d}_v*.mp4"))
        best = None
        best_v = -1
        for p in versioned:
            try:
                v = int(p.stem.split("_v")[-1])
                if v > best_v:
                    best_v = v
                    best = p
            except ValueError:
                pass
        return best

    segment_paths = []
    for i in range(len(segments)):
        p = resolve_segment_path(i)
        if p is None:
            print(f"Missing segment file for segment {i} (seg_{i:03d}.mp4 or seg_{i:03d}_v*.mp4)", file=sys.stderr)
            sys.exit(1)
        segment_paths.append(p)

    out_path = Path(args.output).resolve()
    out_path.parent.mkdir(parents=True, exist_ok=True)
    print(f"Stitching {len(segment_paths)} clips to {out_path}...")
    concat_videos(segment_paths, out_path, include_audio=args.audio)
    print("Done.")


if __name__ == "__main__":
    main()
