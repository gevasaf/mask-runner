"""Shared config loading, validation, and segment helpers for Runway scripts."""

import json
from pathlib import Path

# Models that only support first frame (no last frame). Stitching requires first+last.
MODEL_FIRST_FRAME_ONLY = frozenset({"gen4_turbo", "veo3"})

# Models that support first + last frame (required for keyframe stitching).
MODELS_FIRST_LAST_SUPPORTED = frozenset({"veo3.1", "veo3.1_fast", "gen3a_turbo"})

# Allowed duration (seconds) per model. API rejects other values.
MODEL_ALLOWED_DURATIONS = {
    "gen4_turbo": None,  # int 2-10 per SDK
    "veo3": None,
    "veo3.1": (4, 6, 8),
    "veo3.1_fast": (4, 6, 8),
    "gen3a_turbo": (5, 10),
}

# Allowed ratio per model (use these or API may reject).
MODEL_ALLOWED_RATIOS = {
    "gen4_turbo": ("1280:720", "720:1280", "1104:832", "832:1104", "960:960", "1584:672"),
    "veo3": ("1280:720", "720:1280"),
    "veo3.1": ("1280:720", "720:1280", "1080:1920", "1920:1080"),
    "veo3.1_fast": ("1280:720", "720:1280", "1080:1920", "1920:1080"),
    "gen3a_turbo": ("768:1280", "1280:768"),
}


def load_config(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def has_end_frame(segment: dict) -> bool:
    """True if segment has an end frame (first+last); False for start-only."""
    if "end_index" in segment:
        return True
    if "end_image" in segment and segment["end_image"] is not None:
        return True
    return False


def get_image_path(
    segment: dict, index: int, images_list: list, image_dir: Path
) -> Path:
    """Get filesystem path for start (index=0) or end (index=1) image. For start-only segment, only index=0 is valid."""
    if index == 1 and not has_end_frame(segment):
        raise ValueError("Segment has no end frame")
    use_indices = "start_index" in segment or "end_index" in segment
    if use_indices:
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
        # Start image
        if "start_index" in seg or "end_index" in seg:
            si = seg["start_index"]
            name = images_list[si]
        else:
            name = seg["start_image"]
        path = (image_dir / name).resolve()
        path_str = str(path)
        if path_str not in seen:
            seen.add(path_str)
            out.append((path, path_str))
        # End image (if present)
        if has_end_frame(seg):
            if "start_index" in seg or "end_index" in seg:
                ei = seg["end_index"]
                name = images_list[ei]
            else:
                name = seg["end_image"]
            path = (image_dir / name).resolve()
            path_str = str(path)
            if path_str not in seen:
                seen.add(path_str)
                out.append((path, path_str))
    return out


def image_path_to_key(path: Path) -> str:
    return str(path.resolve())


def build_segment_prompt(segment: dict, general_prompt: str | None) -> str:
    """Combine general_prompt with segment prompt. Full string is sent to the API as prompt_text."""
    seg_prompt = (segment.get("prompt") or "").strip()
    gp = (general_prompt or "").strip()
    if not gp:
        return seg_prompt
    if not seg_prompt:
        return gp
    return f"{gp}. {seg_prompt}"


def validate_segment_schema(
    segment: dict, index: int, images_list: list, use_indices: bool
) -> list[str]:
    errors = []
    if "prompt" not in segment:
        errors.append(f"Segment {index}: missing 'prompt'.")
    duration = segment.get("duration")
    if duration is None:
        errors.append(f"Segment {index}: missing 'duration'.")
    elif not isinstance(duration, (int, float)) or duration <= 0:
        errors.append(f"Segment {index}: 'duration' must be a positive number, got {duration!r}.")
    elif duration > 10:
        errors.append(f"Segment {index}: 'duration' {duration} may exceed Runway max (e.g. 10s).")

    if use_indices:
        if "start_index" not in segment:
            errors.append(f"Segment {index}: missing 'start_index' (when using indices).")
        else:
            si = segment["start_index"]
            if not isinstance(si, int) or si < 0 or si >= len(images_list):
                errors.append(f"Segment {index}: invalid 'start_index' {si} (max {len(images_list) - 1}).")
        if has_end_frame(segment):
            if "end_index" not in segment:
                errors.append(f"Segment {index}: has end frame but missing 'end_index'.")
            else:
                ei = segment["end_index"]
                if not isinstance(ei, int) or ei < 0 or ei >= len(images_list):
                    errors.append(f"Segment {index}: invalid 'end_index' {ei} (max {len(images_list) - 1}).")
    else:
        if "start_image" not in segment:
            errors.append(f"Segment {index}: missing 'start_image' (when using image names).")
        if has_end_frame(segment) and ("end_image" not in segment or segment.get("end_image") is None):
            errors.append(f"Segment {index}: has end frame but missing 'end_image'.")

    return errors


def validate_config(config: dict, config_dir: Path) -> list[str]:
    errors = []
    if "segments" not in config:
        errors.append("Config missing 'segments' array.")
        return errors

    segments = config["segments"]
    if not segments:
        errors.append("'segments' is empty.")

    images_list = config.get("images", [])
    use_indices = any("start_index" in s or "end_index" in s for s in segments)

    if use_indices:
        if not images_list:
            errors.append("Using index-based segments but 'images' is missing or empty.")
        for i, seg in enumerate(segments):
            errors.extend(validate_segment_schema(seg, i, images_list, use_indices=True))
    else:
        for i, seg in enumerate(segments):
            errors.extend(validate_segment_schema(seg, i, images_list, use_indices=False))

    image_dir = config_dir / config.get("image_dir", ".")
    for i, seg in enumerate(segments):
        if use_indices:
            start_idx = seg.get("start_index", 0)
            if start_idx < len(images_list):
                p = (image_dir / images_list[start_idx]).resolve()
                if not p.is_file():
                    errors.append(f"Segment {i}: start image not found: {p}")
            if has_end_frame(seg):
                end_idx = seg.get("end_index", 0)
                if end_idx < len(images_list):
                    p = (image_dir / images_list[end_idx]).resolve()
                    if not p.is_file():
                        errors.append(f"Segment {i}: end image not found: {p}")
        else:
            name = seg.get("start_image")
            if name:
                p = (image_dir / name).resolve()
                if not p.is_file():
                    errors.append(f"Segment {i}: start image not found: {p}")
            if has_end_frame(seg):
                name = seg.get("end_image")
                if name:
                    p = (image_dir / name).resolve()
                    if not p.is_file():
                        errors.append(f"Segment {i}: end image not found: {p}")

    return errors


def validate_model_support(config: dict) -> list[str]:
    """Validate that the configured model supports first+last frame and segment durations/ratio. Returns list of errors."""
    errors = []
    model = config.get("model", "gen4_turbo")
    segments = config.get("segments", [])
    ratio = config.get("ratio", "1280:720")

    any_end_frame = any(has_end_frame(s) for s in segments)
    if any_end_frame and model in MODEL_FIRST_FRAME_ONLY:
        errors.append(
            f"Model '{model}' does not support first+last frame (only first frame). "
            f"Keyframe stitching requires first+last. Use one of: {', '.join(sorted(MODELS_FIRST_LAST_SUPPORTED))}."
        )

    allowed_durations = MODEL_ALLOWED_DURATIONS.get(model)
    if allowed_durations is not None:
        for i, seg in enumerate(segments):
            d = seg.get("duration")
            if d is not None:
                try:
                    sec = int(round(float(d)))
                except (TypeError, ValueError):
                    sec = None
                if sec is not None and sec not in allowed_durations:
                    errors.append(
                        f"Segment {i}: duration {d} is not allowed for model '{model}'. "
                        f"Allowed: {allowed_durations}."
                    )
    else:
        for i, seg in enumerate(segments):
            d = seg.get("duration")
            if d is not None:
                try:
                    sec = int(round(float(d)))
                except (TypeError, ValueError):
                    pass
                else:
                    if sec < 2 or sec > 10:
                        errors.append(f"Segment {i}: duration {d} outside 2–10 seconds for model '{model}'.")

    allowed_ratios = MODEL_ALLOWED_RATIOS.get(model)
    if allowed_ratios and ratio not in allowed_ratios:
        errors.append(
            f"Ratio '{ratio}' is not allowed for model '{model}'. Allowed: {allowed_ratios}."
        )

    return errors
