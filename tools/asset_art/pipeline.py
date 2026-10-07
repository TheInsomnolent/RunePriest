from __future__ import annotations

import copy
import json
import math
import re
from pathlib import Path, PurePosixPath


ROOT = Path(__file__).resolve().parent


def dimensions(value: object) -> bool:
    return (
        isinstance(value, list)
        and len(value) == 2
        and all(type(component) is int and 0 < component <= 16384 for component in value)
    )


def validate_profile(profile: dict, slug: str) -> dict:
    if not isinstance(profile, dict):
        raise ValueError("Profile must be a JSON object.")
    if not re.fullmatch(r"[a-z][a-z0-9_]*", slug):
        raise ValueError("Slug must use lowercase letters, digits, and underscores.")
    if not dimensions(profile.get("size")):
        raise ValueError("size must contain two positive integer dimensions <= 16384.")
    sample = profile.get("supersample", 1)
    if type(sample) is not int or not 1 <= sample <= 8:
        raise ValueError("supersample must be an integer from 1 to 8.")
    if max(profile["size"]) * sample > 16384:
        raise ValueError("Supersampled dimensions must not exceed 16384.")
    if type(profile.get("transparent", True)) is not bool:
        raise ValueError("transparent must be a boolean.")
    if profile.get("camera", "ORTHO") not in ("ORTHO", "PERSP"):
        raise ValueError("camera must be ORTHO or PERSP.")
    if profile.get("framing", "auto") not in ("auto", "manual"):
        raise ValueError("framing must be auto or manual.")
    if profile.get("camera", "ORTHO") == "PERSP" and profile.get("framing", "auto") != "manual":
        raise ValueError("Perspective cameras require manual framing.")
    padding = profile.get("padding", 0.12)
    if type(padding) not in (int, float) or not math.isfinite(padding) or not 0 <= padding < 0.45:
        raise ValueError("padding must be in [0, 0.45).")
    for key, default in (("lens", 50), ("ortho_scale", 4), ("world_strength", 0.35)):
        value = profile.get(key, default)
        if type(value) not in (int, float) or not math.isfinite(value) or value < 0 or (key != "world_strength" and value == 0):
            raise ValueError(f"{key} must be a finite {'nonnegative' if key == 'world_strength' else 'positive'} number.")
    if type(profile.get("seed", 0)) is not int:
        raise ValueError("seed must be an integer.")
    for key, default in (("camera_position", [3, -6, 4]), ("camera_target", [0, 0, 0])):
        vector = profile.get(key, default)
        if not isinstance(vector, list) or len(vector) != 3 or any(
                type(value) not in (int, float) or not math.isfinite(value) for value in vector):
            raise ValueError(f"{key} must contain three finite numbers.")
    if profile.get("camera_position", [3, -6, 4]) == profile.get("camera_target", [0, 0, 0]):
        raise ValueError("Camera position and target must differ.")
    if not isinstance(profile.get("world_color", "343943"), str) or not re.fullmatch(r"[0-9a-fA-F]{6}", profile.get("world_color", "343943")):
        raise ValueError("world_color must be a six-digit RGB hex string without #.")
    lights = profile.get("lights", [])
    if not isinstance(lights, list):
        raise ValueError("lights must be an array.")
    for light in lights:
        if not isinstance(light, dict) or not isinstance(light.get("name"), str) or not light["name"]:
            raise ValueError("Each light needs a name.")
        for key, default in (("position", None), ("target", [0, 0, 0])):
            vector = light.get(key, default)
            if not isinstance(vector, list) or len(vector) != 3 or any(
                    type(value) not in (int, float) or not math.isfinite(value) for value in vector):
                raise ValueError(f"Light {key} must contain three finite numbers.")
        if light["position"] == light.get("target", [0, 0, 0]):
            raise ValueError("Light position and target must differ.")
        for key, default in (("energy", None), ("size", 4)):
            value = light.get(key, default)
            if type(value) not in (int, float) or not math.isfinite(value) or value < 0 or (key == "size" and value == 0):
                raise ValueError(f"Invalid light {key}.")
        color = light.get("color", "FFFFFF")
        if not isinstance(color, str) or not re.fullmatch(r"[0-9a-fA-F]{6}", color):
            raise ValueError("Light color must be a six-digit RGB hex string without #.")
    if type(profile.get("border", 0)) is not int or not 0 <= profile.get("border", 0) <= 16:
        raise ValueError("border must be an integer from 0 to 16.")
    postprocess = profile.get("postprocess", {})
    if not isinstance(postprocess, dict):
        raise ValueError("postprocess must be an object.")
    if type(postprocess.get("enabled", False)) is not bool:
        raise ValueError("postprocess.enabled must be a boolean.")
    controls = (("radius", 3.0, 0.1, 16), ("strength", 0.75, 0, 1),
                ("pigment_strength", 0.35, 0, 1), ("pigment_scale", 45.0, 1, 500),
                ("brush_strength", 0, 0, 1), ("brush_width", 2.5, 1, 32),
                ("brush_length", 8.0, 1, 32), ("brush_angle", 25.0, -180, 180),
                ("tone_strength", 0, 0, 1), ("glow_strength", 0, 0, 2),
                ("vignette_strength", 0, 0, 1), ("vignette_x", 0.5, 0, 1),
                ("vignette_y", 0.5, 0, 1), ("vignette_width", 0.7, 0.1, 2),
                ("vignette_height", 0.7, 0.1, 2))
    if type(postprocess.get("tone_steps", 8)) is not int or not 2 <= postprocess.get("tone_steps", 8) <= 32:
        raise ValueError("postprocess.tone_steps must be an integer from 2 to 32.")
    unknown = postprocess.keys() - {"enabled", "tone_steps", *(control[0] for control in controls)}
    if unknown:
        raise ValueError(f"Unknown postprocess settings: {', '.join(sorted(unknown))}")
    for key, default, minimum, maximum in controls:
        value = postprocess.get(key, default)
        if type(value) not in (int, float) or not math.isfinite(value) or not minimum <= value <= maximum:
            raise ValueError(f"postprocess.{key} must be a finite number in [{minimum}, {maximum}].")
    outputs = profile.get("outputs")
    if not isinstance(outputs, list) or not outputs:
        raise ValueError("At least one output is required.")
    paths = set()
    names = set()
    for output in outputs:
        if not isinstance(output, dict):
            raise ValueError("Each output must be an object.")
        if not dimensions(output.get("size")):
            raise ValueError("Every output needs a valid size.")
        width, height = output["size"]
        if width * profile["size"][1] != height * profile["size"][0]:
            raise ValueError("Outputs must preserve the master aspect ratio; use a separate profile for another crop.")
        name = output.get("name", "")
        if not re.fullmatch(r"[a-z][a-z0-9_]*", name) or name in names:
            raise ValueError("Output names must be unique lowercase identifiers.")
        names.add(name)
        path = output.get("path", "").replace("{slug}", slug)
        if not path or "\\" in path or ":" in path or "{" in path or "}" in path:
            raise ValueError("Output paths must be relative POSIX paths with only {slug} substitution.")
        relative = PurePosixPath(path)
        if relative.is_absolute() or ".." in relative.parts or relative.suffix != ".png":
            raise ValueError("Output paths must stay inside the output directory and end in .png.")
        if relative.as_posix() == "preview.png":
            raise ValueError("preview.png is reserved for the review sheet.")
        if relative.as_posix() in paths:
            raise ValueError("Duplicate output path.")
        paths.add(relative.as_posix())
        if output.get("kind", "color") not in ("color", "outline"):
            raise ValueError("Output kind must be color or outline.")
        radius = output.get("radius", 2)
        if type(radius) is not int or not 0 <= radius <= 16:
            raise ValueError("Outline radius must be an integer from 0 to 16.")
        if output.get("kind") == "outline" and not profile.get("transparent", True):
            raise ValueError("Outline outputs require a transparent render.")
    return profile


def load_profile(name: str, slug: str, override: Path | None = None) -> dict:
    profiles = json.loads((ROOT / "profiles.json").read_text(encoding="utf-8"))
    if name not in profiles:
        raise ValueError(f"Unknown profile: {name}")
    profile = copy.deepcopy(profiles[name])
    if override:
        settings = json.loads(override.read_text(encoding="utf-8"))
        if not isinstance(settings, dict):
            raise ValueError("Settings must be a JSON object.")
        profile.update(settings)
    visited = {name}
    while isinstance(profile.get("postprocess"), str):
        reference = profile["postprocess"]
        if reference not in profiles or reference in visited:
            raise ValueError(f"Unknown or cyclic postprocess profile reference: {reference}")
        visited.add(reference)
        profile["postprocess"] = copy.deepcopy(profiles[reference].get("postprocess", {}))
    return validate_profile(profile, slug)