from __future__ import annotations

import argparse
import importlib.util
import json
import random
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))

from studio import configure_scene, frame_collection
from compositor import configure_compositor


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--recipe", type=Path, required=True)
    parser.add_argument("--profile", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    profile = json.loads(args.profile.read_text(encoding="utf-8"))
    random.seed(profile.get("seed", 0))
    scene = configure_scene(profile)
    spec = importlib.util.spec_from_file_location("asset_recipe", args.recipe)
    recipe = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(recipe)
    collection = recipe.build(scene, profile)
    if profile.get("framing", "auto") == "auto":
        frame_collection(scene, collection, profile.get("padding", 0.12))
    configure_compositor(scene, profile)
    scene.render.filepath = str(args.output / "master.png")
    bpy.ops.wm.save_as_mainfile(filepath=str(args.output / "scene.blend"))
    bpy.ops.render.render(write_still=True)
    metadata = {"blender_version": bpy.app.version_string, "engine": scene.render.engine,
                "recipe": str(args.recipe), "camera_scale": scene.camera.data.ortho_scale}
    (args.output / "render.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()