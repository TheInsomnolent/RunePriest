from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

from pipeline import validate_profile


def contained_path(root: Path, relative: str) -> Path:
    target = (root / relative).resolve()
    if not target.is_relative_to(root.resolve()):
        raise ValueError(f"Output escapes destination: {relative}")
    return target


def dilate(alpha: Image.Image, radius: int) -> Image.Image:
    return alpha.filter(ImageFilter.MaxFilter(radius * 2 + 1)) if radius else alpha.copy()


def color_icon(source: Image.Image, size: tuple[int, int], border: int) -> Image.Image:
    resized = source.convert("RGBa").resize(size, Image.Resampling.LANCZOS).convert("RGBA")
    if not border:
        return resized
    backing = Image.new("RGBA", size, (34, 26, 35, 0))
    backing.putalpha(dilate(resized.getchannel("A"), border))
    return Image.alpha_composite(backing, resized)


def review_sheet(images: list[tuple[str, Image.Image]], destination: Path) -> None:
    tile_width = max(300, max(image.width for _, image in images) + 24)
    tile_height = max(300, max(image.height for _, image in images) + 48)
    sheet = Image.new("RGB", (tile_width * 3, tile_height * len(images)), (45, 45, 45))
    draw = ImageDraw.Draw(sheet)
    for row, (name, image) in enumerate(images):
        for column, background in enumerate(((235, 235, 235), (38, 38, 38), None)):
            left, top = column * tile_width, row * tile_height
            if background:
                draw.rectangle((left, top, left + tile_width - 1, top + tile_height - 1), fill=background)
            else:
                for vertical in range(0, tile_height, 16):
                    for horizontal in range(0, tile_width, 16):
                        shade = 110 if (horizontal // 16 + vertical // 16) % 2 else 155
                        draw.rectangle((left + horizontal, top + vertical,
                                        min(left + horizontal + 15, left + tile_width - 1),
                                        min(top + vertical + 15, top + tile_height - 1)),
                                       fill=(shade, shade, shade))
            draw.text((left + 12, top + 10), f"{name}  {image.width} x {image.height}",
                      fill=(25, 25, 25) if column == 0 else (255, 255, 255))
            sheet.paste(image, (left + (tile_width - image.width) // 2, top + 36), image)
    sheet.save(destination)


def export_images(master: Path, profile: dict, slug: str, destination: Path) -> dict:
    validate_profile(profile, slug)
    destination.mkdir(parents=True, exist_ok=True)
    with Image.open(master) as opened:
        source = opened.convert("RGBA")
    sample = profile.get("supersample", 1)
    expected = tuple(component * sample for component in profile["size"])
    if source.size != expected:
        raise ValueError(f"Master dimensions {source.size} do not match {expected}.")
    alpha = source.getchannel("A")
    if not alpha.getbbox():
        raise ValueError("Render is empty.")
    transparent = profile.get("transparent", True)
    if transparent and alpha.getextrema()[0] == 255:
        raise ValueError("Transparent profile produced a fully opaque render.")
    if not transparent and alpha.getextrema()[0] != 255:
        raise ValueError("Opaque profile produced transparent pixels.")
    images = []
    records = []
    for output in profile["outputs"]:
        size = tuple(output["size"])
        border = profile.get("border", 0) if transparent else 0
        icon = color_icon(source, size, border)
        if output.get("kind", "color") == "outline":
            mask = dilate(icon.getchannel("A"), output.get("radius", 2))
            icon = Image.new("RGBA", size, (255, 255, 255, 0))
            icon.putalpha(mask)
        bounds = icon.getchannel("A").getbbox()
        if not bounds:
            raise ValueError(f"Output {output['name']} is empty.")
        if transparent and (bounds[0] == 0 or bounds[1] == 0 or bounds[2] == size[0] or bounds[3] == size[1]):
            raise ValueError(f"Output {output['name']} touches the canvas edge; increase padding.")
        relative = output["path"].replace("{slug}", slug)
        path = contained_path(destination, relative)
        path.parent.mkdir(parents=True, exist_ok=True)
        icon.save(path)
        images.append((output["name"], icon))
        records.append({"name": output["name"], "path": relative, "size": list(size), "bounds": bounds})
    review_sheet(images, destination / "preview.png")
    report = {"slug": slug, "profile": profile, "outputs": records}
    (destination / "manifest.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    return report