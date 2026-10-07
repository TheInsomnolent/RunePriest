from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
from pathlib import Path

from export import contained_path, export_images
from pipeline import ROOT, load_profile


def find_blender(explicit: str | None) -> str:
    candidate = explicit or os.environ.get("BLENDER_PATH") or shutil.which("blender")
    if candidate:
        return candidate
    installations = sorted(Path("C:/Program Files/Blender Foundation").glob("Blender */blender.exe"))
    if installations:
        return str(installations[-1])
    raise ValueError("Blender not found; set BLENDER_PATH or pass --blender.")


def publish(exports: Path, destination: Path, report: dict, force: bool) -> None:
    pairs = [(contained_path(exports, output["path"]), contained_path(destination, output["path"]))
             for output in report["outputs"]]
    for source, target in pairs:
        if not source.is_file():
            raise ValueError(f"Missing export: {source}")
        if target.exists() and not force:
            raise ValueError(f"Refusing to overwrite {target}; pass --force to replace approved art.")
    for source, target in pairs:
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)


def save_review(exports: Path, profile: str, slug: str, destination: Path = ROOT / "reviews") -> Path:
    target = contained_path(destination, f"{profile}/{slug}.png")
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(exports / "preview.png", target)
    return target


def main() -> None:
    parser = argparse.ArgumentParser(description="Build a procedural Blender asset and export its configured PNGs.")
    parser.add_argument("slug")
    parser.add_argument("--profile", default="relic")
    parser.add_argument("--settings", type=Path, help="JSON profile overrides, including custom sizes and outputs")
    parser.add_argument("--recipe", type=Path, help="Trusted Python recipe; defaults to recipes/<slug>.py")
    parser.add_argument("--blender")
    parser.add_argument("--build-dir", type=Path, default=ROOT / "build")
    parser.add_argument("--export-only", action="store_true", help="Re-export an existing master without rerendering")
    parser.add_argument("--publish", type=Path, help="Explicit destination asset directory; never inferred")
    parser.add_argument("--force", action="store_true", help="Allow overwriting published PNGs")
    args = parser.parse_args()
    try:
        profile = load_profile(args.profile, args.slug, args.settings)
        output = contained_path(args.build_dir.resolve(), f"{args.slug}/{args.profile}")
        output.mkdir(parents=True, exist_ok=True)
        resolved_profile = output / "profile.json"
        if not args.export_only:
            recipe = (args.recipe or ROOT / "recipes" / f"{args.slug}.py").resolve()
            if not recipe.is_file():
                raise ValueError(f"Recipe not found: {recipe}")
            resolved_profile.write_text(json.dumps(profile, indent=2) + "\n", encoding="utf-8")
            command = [find_blender(args.blender), "--background", "--factory-startup", "--python-exit-code", "1",
                       "--python", str(ROOT / "blender_scene.py"), "--", "--recipe", str(recipe),
                       "--profile", str(resolved_profile), "--output", str(output)]
            subprocess.run(command, check=True)
        else:
            original = json.loads(resolved_profile.read_text(encoding="utf-8"))
            render_settings = lambda settings: {key: value for key, value in settings.items() if key not in ("border", "outputs")}
            if render_settings(original) != render_settings(profile):
                raise ValueError("Render settings changed; rerender without --export-only.")
        exports = output / "exports"
        report = export_images(output / "master.png", profile, args.slug, exports)
        if args.publish:
            publish(exports, args.publish.resolve(), report, args.force)
            print(f"Published {len(report['outputs'])} PNGs to {args.publish.resolve()}")
        review = save_review(exports, args.profile, args.slug)
        print(f"Scene: {output / 'scene.blend'}")
        print(f"Review: {review}")
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        parser.exit(1, f"Asset build failed: {error}\n")


if __name__ == "__main__":
    main()