"""Key and defringe a magenta-backed grapple sprite; usage: script.py INPUT.png."""
import argparse
from pathlib import Path
import re
import uuid

from PIL import Image


def key_magenta(image):
    result = Image.new("RGBA", image.size)
    pixels = []
    source = image.convert("RGBA")
    source_pixels = source.load()
    for index in range(source.width * source.height):
        r, g, b, a = source_pixels[index % source.width, index // source.width]
        spill = max(0, min(r, b) - g)
        coverage = 1.0 - spill / 255.0
        if spill > 100 or a == 0:
            pixels.append((0, 0, 0, 0))
            continue
        removed = 255 * (1 - coverage)
        red = max(0, min(255, round((r - removed) / coverage)))
        green = max(0, min(255, round(g / coverage)))
        blue = max(0, min(255, round((b - removed) / coverage)))
        if coverage < 1 and red > green and blue > green:
            red = green
        pixels.append((red, green, blue, round(a * coverage)))
    result.putdata(pixels)
    bounds = result.getbbox()
    if bounds is None:
        raise ValueError("The source contains no foreground after keying")
    result = result.crop(bounds)
    return result.resize((max(1, round(result.width * 96 / result.height)), 96), Image.Resampling.NEAREST)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = root / "Assets/Sprites/Environment/Generated"
    output.mkdir(exist_ok=True)
    image = key_magenta(Image.open(args.source))
    image.save(output / "grapple_anchor.png")
    meta = output / "grapple_anchor.png.meta"
    template = (root / "Assets/Sprites/UI/Kit/ui_panel_small.png.meta").read_text()
    guid = re.search(r"^guid: (\w+)", meta.read_text(), re.M)[1] if meta.exists() else uuid.uuid4().hex
    template = re.sub(r"^guid: \w+", "guid: " + guid, template, flags=re.M)
    template = re.sub(r"spritePixelsToUnits: [\d.]+", "spritePixelsToUnits: 106.666667", template)
    template = re.sub(r"spriteBorder: \{[^}]+\}", "spriteBorder: {x: 0, y: 0, z: 0, w: 0}", template)
    meta.write_text(template)
    folder = output.with_suffix(".meta")
    if not folder.exists():
        folder.write_text("fileFormatVersion: 2\nguid: " + uuid.uuid4().hex + "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n")
    print(f"Grapple anchor: {image.width}x{image.height}, RGBA, 0.9 world units tall")


if __name__ == "__main__":
    main()
