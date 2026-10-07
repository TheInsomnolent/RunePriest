from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

from export import color_icon, review_sheet
from pipeline import ROOT


def asset_catalog(profile: str = "relic") -> list[tuple[str, str]]:
    source = ROOT.parent.parent / f"RunePriest/localization/eng/{profile}s.json"
    localization = json.loads(source.read_text(encoding="utf-8"))
    return sorted(((key.removeprefix("RUNEPRIEST-").removesuffix(".title").lower(), title)
                   for key, title in localization.items()
                   if key.startswith("RUNEPRIEST-") and key.endswith(".title")), key=lambda item: item[1])


def load_icons(slug: str, installed: bool, profile: str = "relic") -> tuple[Image.Image, Image.Image, Image.Image]:
    exports = ROOT / "build" / slug / profile / "exports"
    source = ROOT.parent.parent / f"RunePriest/images/{profile}s" if installed else exports
    images = []
    outputs = ((f"big/{slug}.png", (256, 256)), (f"{slug}.png", (94, 94)),
               (f"{slug}_outline.png", (94, 94))) if profile == "relic" else (
                   (f"{slug}.png", (256, 256)), (f"outline/{slug}.png", (256, 256)))
    for relative, size in outputs:
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
    if ImageChops.subtract(images[-2].getchannel("A"), images[-1].getchannel("A")).getbbox():
        raise ValueError(f"Outline does not cover the icon: {slug}")
    visible_outline = images[-1].getchannel("A").point(lambda value: 255 if value else 0)
    for channel in images[-1].convert("RGB").split():
        if ImageChops.multiply(ImageChops.invert(channel), visible_outline).getbbox():
            raise ValueError(f"Outline is not white: {slug}")
    if profile == "potion":
        return images[0], color_icon(images[0], (64, 64), 0), images[1]
    return images[0], images[1], images[2]


def review(installed: bool = False, profile: str = "relic") -> None:
    catalog = asset_catalog(profile)
    output = ROOT / "reviews" / profile
    output.mkdir(parents=True, exist_ok=True)
    columns = 4
    rows = (len(catalog) + columns - 1) // columns
    large = Image.new("RGBA", (columns * 288, rows * 310 + 60), "#24292C")
    small = Image.new("RGBA", (columns * 288, rows * 158 + 60), "#24292C")
    large_draw = ImageDraw.Draw(large)
    small_draw = ImageDraw.Draw(small)
    font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 17)
    heading = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 23)
    thumbnail_size = 94 if profile == "relic" else 64
    for draw, label in ((large_draw, f"Rune Priest {profile}s | 256px review"),
                        (small_draw, f"Rune Priest {profile}s | {thumbnail_size}px readability on light / dark")):
        draw.text((20, 14), label, font=heading, fill="#EFF4E9")
    for index, (slug, title) in enumerate(catalog):
        icon, thumbnail, outline = load_icons(slug, installed, profile)
        images = [("large", icon), ("small", thumbnail), ("outline", outline)] if profile == "relic" else [
            ("icon", icon), ("outline", outline)]
        review_sheet(images, output / f"{slug}.png")
        column, row = index % columns, index // columns
        left, top = column * 288, 60 + row * 310
        large_draw.rectangle((left + 3, top, left + 284, top + 302), fill="#303639")
        large.alpha_composite(icon, (left + 16, top + 6))
        large_draw.text((left + 144, top + 272), title, font=font, fill="#F0EEDF", anchor="mt")
        top = 60 + row * 158
        small_draw.rectangle((left + 8, top + 8, left + 143, top + 116), fill="#E9EAE4")
        small_draw.rectangle((left + 144, top + 8, left + 279, top + 116), fill="#181C20")
        small.alpha_composite(thumbnail, (left + 8 + (136 - thumbnail_size) // 2,
                          top + 8 + (108 - thumbnail_size) // 2))
        small.alpha_composite(thumbnail, (left + 144 + (136 - thumbnail_size) // 2,
                          top + 8 + (108 - thumbnail_size) // 2))
        small_draw.text((left + 144, top + 128), title, font=font, fill="#F0EEDF", anchor="mt")
        icon.close()
        thumbnail.close()
        outline.close()
    large.convert("RGB").save(output / "large.png")
    small.convert("RGB").save(output / "small.png")
    count = len(catalog) * (3 if profile == "relic" else 2)
    print(f"Validated {len(catalog)} {profile}s / {count} PNGs ({'installed' if installed else 'exports'}).")
    print(f"Large review: {output / 'large.png'}")
    print(f"Small review: {output / 'small.png'}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Validate localized relic or potion icons and create labeled review sheets.")
    parser.add_argument("--profile", choices=("relic", "potion"), default="relic")
    parser.add_argument("--installed", action="store_true", help="Review published PNGs; compare with rendered exports when available")
    args = parser.parse_args()
    review(args.installed, args.profile)