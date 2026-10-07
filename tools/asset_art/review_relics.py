from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

from export import review_sheet
from pipeline import ROOT


def relic_catalog() -> list[tuple[str, str]]:
    source = ROOT.parent.parent / "RunePriest/localization/eng/relics.json"
    localization = json.loads(source.read_text(encoding="utf-8"))
    return sorted(((key.removeprefix("RUNEPRIEST-").removesuffix(".title").lower(), title)
                   for key, title in localization.items()
                   if key.startswith("RUNEPRIEST-") and key.endswith(".title")), key=lambda item: item[1])


def load_icons(slug: str, installed: bool) -> tuple[Image.Image, Image.Image, Image.Image]:
    exports = ROOT / "build" / slug / "relic/exports"
    source = ROOT.parent.parent / "RunePriest/images/relics" if installed else exports
    images = []
    for relative, size in ((f"big/{slug}.png", (256, 256)), (f"{slug}.png", (94, 94)),
                           (f"{slug}_outline.png", (94, 94))):
        path = source / relative
        with Image.open(path) as original:
            if original.mode != "RGBA" or original.size != size:
                raise ValueError(f"Invalid icon format: {path}")
            image = original.copy()
        alpha = image.getchannel("A")
        bounds = alpha.getbbox()
        if alpha.getextrema() != (0, 255) or bounds is None:
            raise ValueError(f"Missing transparency or solid pixels: {path}")
        if bounds[0] <= 0 or bounds[1] <= 0 or bounds[2] >= size[0] or bounds[3] >= size[1]:
            raise ValueError(f"Clipped icon: {path}")
        if installed and (exports / relative).is_file() and path.read_bytes() != (exports / relative).read_bytes():
            raise ValueError(f"Installed art differs from reviewed export: {path}")
        images.append(image)
    if ImageChops.subtract(images[1].getchannel("A"), images[2].getchannel("A")).getbbox():
        raise ValueError(f"Outline does not cover the icon: {slug}")
    visible_outline = images[2].getchannel("A").point(lambda value: 255 if value else 0)
    for channel in images[2].convert("RGB").split():
        if ImageChops.multiply(ImageChops.invert(channel), visible_outline).getbbox():
            raise ValueError(f"Outline is not white: {slug}")
    return images[0], images[1], images[2]


def review(installed: bool = False) -> None:
    catalog = relic_catalog()
    output = ROOT / "reviews/relic"
    output.mkdir(parents=True, exist_ok=True)
    columns = 4
    rows = (len(catalog) + columns - 1) // columns
    large = Image.new("RGBA", (columns * 288, rows * 310 + 60), "#24292C")
    small = Image.new("RGBA", (columns * 288, rows * 158 + 60), "#24292C")
    large_draw = ImageDraw.Draw(large)
    small_draw = ImageDraw.Draw(small)
    font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 17)
    heading = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 23)
    for draw, label in ((large_draw, "Rune Priest relics | 256px review"),
                        (small_draw, "Rune Priest relics | 94px on light / dark")):
        draw.text((20, 14), label, font=heading, fill="#EFF4E9")
    for index, (slug, title) in enumerate(catalog):
        icon, thumbnail, outline = load_icons(slug, installed)
        review_sheet([("large", icon), ("small", thumbnail), ("outline", outline)], output / f"{slug}.png")
        column, row = index % columns, index // columns
        left, top = column * 288, 60 + row * 310
        large_draw.rectangle((left + 3, top, left + 284, top + 302), fill="#303639")
        large.alpha_composite(icon, (left + 16, top + 6))
        large_draw.text((left + 144, top + 272), title, font=font, fill="#F0EEDF", anchor="mt")
        top = 60 + row * 158
        small_draw.rectangle((left + 8, top + 8, left + 143, top + 116), fill="#E9EAE4")
        small_draw.rectangle((left + 144, top + 8, left + 279, top + 116), fill="#181C20")
        small.alpha_composite(thumbnail, (left + 29, top + 15))
        small.alpha_composite(thumbnail, (left + 165, top + 15))
        small_draw.text((left + 144, top + 128), title, font=font, fill="#F0EEDF", anchor="mt")
        icon.close()
        thumbnail.close()
        outline.close()
    large.convert("RGB").save(output / "large.png")
    small.convert("RGB").save(output / "small.png")
    print(f"Validated {len(catalog)} relics / {len(catalog) * 3} PNGs ({'installed' if installed else 'exports'}).")
    print(f"Large review: {output / 'large.png'}")
    print(f"Small review: {output / 'small.png'}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Validate every localized relic icon and create labeled review sheets.")
    parser.add_argument("--installed", action="store_true", help="Review published PNGs; compare with rendered exports when available")
    args = parser.parse_args()
    review(args.installed)