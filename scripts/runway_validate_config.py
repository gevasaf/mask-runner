#!/usr/bin/env python3
"""
Validate Runway keyframe config JSON before generation/stitch.
Exits with 0 if valid, 1 and error messages if invalid.
"""

import sys
from pathlib import Path

# Allow importing _runway_common when run from project root
sys.path.insert(0, str(Path(__file__).resolve().parent))
from _runway_common import load_config, validate_config, validate_model_support


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: python runway_validate_config.py <config.json>", file=sys.stderr)
        return 1
    config_path = Path(sys.argv[1]).resolve()
    if not config_path.is_file():
        print(f"Config not found: {config_path}", file=sys.stderr)
        return 1
    config_dir = config_path.parent
    try:
        config = load_config(str(config_path))
    except Exception as e:
        print(f"Failed to load config: {e}", file=sys.stderr)
        return 1

    errs = validate_config(config, config_dir)
    errs.extend(validate_model_support(config))
    if errs:
        for e in errs:
            print(e, file=sys.stderr)
        return 1
    print("Config is valid.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
