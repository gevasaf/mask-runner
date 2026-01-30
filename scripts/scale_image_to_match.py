#!/usr/bin/env python3
"""Scale one image to match the exact dimensions of another."""

import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow is required. Install with: pip install Pillow")
    sys.exit(1)


def scale_to_match(source_path: Path, reference_path: Path, output_path: Path | None = None) -> None:
    """Scale source image to the exact dimensions of the reference image."""
    if output_path is None:
        output_path = source_path

    ref = Image.open(reference_path).convert("RGBA")
    target_size = ref.size  # (width, height)
    ref.close()

    src = Image.open(source_path).convert("RGBA")
    original_size = src.size
    scaled = src.resize(target_size, Image.Resampling.LANCZOS)
    src.close()

    scaled.save(output_path)
    print(f"Scaled {source_path.name} from {original_size} to {target_size}, saved to {output_path}")


def main() -> None:
    base = Path(__file__).resolve().parent.parent
    images_dir = base / "My project" / "Assets" / "images"

    source = images_dir / "1.png"
    reference = images_dir / "2.png"

    if not source.exists():
        print(f"Error: Source image not found: {source}")
        sys.exit(1)
    if not reference.exists():
        print(f"Error: Reference image not found: {reference}")
        sys.exit(1)

    scale_to_match(source, reference)


if __name__ == "__main__":
    main()
