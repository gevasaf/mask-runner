#!/usr/bin/env python3
"""
Speed up a video to a given playback speed.
Usage: python speed_up_video.py [input.mp4] [--output output.mp4] [--speed 1.622161]
"""

import argparse
from pathlib import Path

# Playback speed factor (1.622161 = ~62% faster)
DEFAULT_SPEED = 1.622161


def main():
    parser = argparse.ArgumentParser(description="Speed up a video file")
    parser.add_argument(
        "input",
        nargs="?",
        default="stitched.mp4",
        help="Input video path (default: stitched.mp4)",
    )
    parser.add_argument(
        "-o", "--output",
        default=None,
        help="Output video path (default: input name with _fast suffix)",
    )
    parser.add_argument(
        "-s", "--speed",
        type=float,
        default=DEFAULT_SPEED,
        help=f"Playback speed factor (default: {DEFAULT_SPEED})",
    )
    args = parser.parse_args()

    input_path = Path(args.input)
    if not input_path.is_file():
        raise SystemExit(f"Input file not found: {input_path}")

    output_path = Path(args.output) if args.output else input_path.with_stem(
        f"{input_path.stem}_fast"
    )

    try:
        from moviepy import VideoFileClip
        from moviepy.video.fx.MultiplySpeed import MultiplySpeed
    except ImportError:
        raise SystemExit(
            "moviepy is required. Install with: pip install moviepy"
        )

    print(f"Loading {input_path}...")
    clip = VideoFileClip(str(input_path))
    print(f"Speeding up by {args.speed}x...")
    fast = clip.with_effects([MultiplySpeed(factor=args.speed)])
    print(f"Writing {output_path}...")
    fast.write_videofile(
        str(output_path),
        codec="libx264",
        audio_codec="aac",
        preset="medium",
        threads=None,
        logger=None,
    )
    fast.close()
    clip.close()
    print(f"Done: {output_path}")


if __name__ == "__main__":
    main()
