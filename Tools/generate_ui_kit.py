#!/usr/bin/env python3
"""Generate Cosmic Horizon's point-filtered UI sprites, metadata and slice preview.

Requires Pillow 10.1+. Run from any directory; asset GUIDs survive regeneration.
"""
import argparse
from pathlib import Path
import re
import uuid

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "Assets/Sprites/UI"
OUTPUT = UI / "Kit"
NAVY = (11, 26, 43, 217)
CYAN = (56, 232, 255, 255)
INNER = (19, 65, 83, 255)
SPECS = {
    "ui_panel": ((96, 96), 24, (600, 400)),
    "ui_panel_small": ((48, 48), 12, (600, 128)),
    "ui_button": ((64, 32), 12, (300, 80)),
    "ui_button_hover": ((64, 32), 12, (300, 80)),
    "ui_button_pressed": ((64, 32), 12, (300, 80)),
    "ui_bar_frame": ((64, 16), 6, (600, 40)),
    "ui_bar_fill": ((16, 8), 2, (560, 24)),
    "ui_slider_track": ((64, 12), 5, (520, 24)),
    "ui_slider_knob": ((24, 24), 0, (96, 96)),
    "ui_toggle_box": ((24, 24), 0, (96, 96)),
    "ui_toggle_check": ((24, 24), 0, (96, 96)),
    "ui_dropdown": ((64, 32), 12, (300, 80)),
    "ui_slot": ((64, 64), 14, (160, 160)),
}


def chamfer(draw, box, cut, fill=None, outline=None):
    x0, y0, x1, y1 = box
    points = [(x0 + cut, y0), (x1 - cut, y0), (x1, y0 + cut),
              (x1, y1 - cut), (x1 - cut, y1), (x0 + cut, y1),
              (x0, y1 - cut), (x0, y0 + cut)]
    draw.polygon(points, fill=fill, outline=outline)


def frame(size, cut=4, fill=NAVY, border=CYAN, inset=0):
    image = Image.new("RGBA", size)
    draw = ImageDraw.Draw(image)
    w, h = size
    chamfer(draw, (inset, inset, w - 1 - inset, h - 1 - inset), cut,
            fill=fill, outline=border)
    chamfer(draw, (inset + 2, inset + 2, w - 3 - inset, h - 3 - inset),
            max(1, cut - 1), outline=INNER)
    return image


def sprite(name, size):
    if name == "ui_bar_fill":
        return Image.new("RGBA", size, (255, 255, 255, 255))
    if name == "ui_toggle_check":
        image = Image.new("RGBA", size)
        ImageDraw.Draw(image).line([(5, 12), (10, 17), (19, 7)], fill=CYAN, width=3)
        return image
    if name == "ui_slider_knob":
        image = Image.new("RGBA", size)
        draw = ImageDraw.Draw(image)
        draw.ellipse((1, 1, 22, 22), fill=NAVY, outline=CYAN, width=2)
        draw.ellipse((6, 6, 17, 17), fill=CYAN)
        draw.rectangle((8, 7, 10, 8), fill=(190, 255, 255, 255))
        return image
    if name == "ui_button_hover":
        image = frame(size, fill=(14, 48, 65, 230), border=(124, 255, 255, 255))
    elif name == "ui_button_pressed":
        image = frame(size, fill=(7, 18, 30, 230), border=(36, 150, 173, 255), inset=1)
    else:
        cut = 8 if name == "ui_panel" else 6 if name == "ui_slot" else 3 if size[1] <= 24 else 4
        image = frame(size, cut=cut)
    draw = ImageDraw.Draw(image)
    w, h = size
    if name == "ui_panel":
        draw.line((10, 17, w - 11, 17), fill=INNER)
        draw.line((24, 17, w - 25, 17), fill=CYAN)
        draw.rectangle((10, 8, 20, 9), fill=CYAN)
        draw.line((w - 20, 8, w - 11, 8), fill=CYAN)
    elif name in ("ui_panel_small", "ui_button", "ui_button_hover"):
        draw.line((6, 4, 10, 4), fill=CYAN)
        draw.line((w - 11, h - 5, w - 7, h - 5), fill=CYAN)
    elif name == "ui_button_pressed":
        draw.line((8, h - 4, w - 9, h - 4), fill=INNER)
    elif name == "ui_dropdown":
        draw.line((w - 12, 4, w - 12, h - 5), fill=INNER)
        # Keep the arrow entirely inside the fixed top-right slice.
        draw.polygon([(w - 10, 7), (w - 4, 7), (w - 7, 10)], fill=CYAN)
    elif name == "ui_slot":
        for x, dx in ((5, 1), (w - 6, -1)):
            for y, dy in ((5, 1), (h - 6, -1)):
                draw.line([(x, y + dy * 6), (x, y), (x + dx * 6, y)], fill=CYAN)
    return image


def texture_meta(template, path, border):
    previous = path.read_text() if path.exists() else ""
    match = re.search(r"^guid: ([0-9a-f]{32})$", previous, re.M)
    guid = match[1] if match else uuid.uuid4().hex
    changes = {
        "guid": guid, "textureType": "8", "spriteMode": "1", "filterMode": "0",
        "textureCompression": "0", "alphaIsTransparency": "1", "spritePixelsToUnits": "100",
        "spriteBorder": "{x: %d, y: %d, z: %d, w: %d}" % ((border,) * 4),
    }
    for field, value in changes.items():
        template, count = re.subn(r"(?m)^(\s*" + field + r":)[^\n]*", lambda m: m[1] + " " + value, template)
        if count == 0:
            raise ValueError("Missing importer field: " + field)
    return "\n".join(line.rstrip() for line in template.splitlines()) + "\n"


def nine_slice(image, size, border):
    if not border:
        return image.resize(size, Image.Resampling.NEAREST)
    w, h = image.size
    dw, dh = size
    if min(w, h, dw, dh) < border * 2:
        raise ValueError("Target and source must fit both fixed borders")
    source_x, source_y = (0, border, w - border, w), (0, border, h - border, h)
    target_x, target_y = (0, border, dw - border, dw), (0, border, dh - border, dh)
    result = Image.new("RGBA", size)
    for y in range(3):
        for x in range(3):
            tile = image.crop((source_x[x], source_y[y], source_x[x + 1], source_y[y + 1]))
            target = (target_x[x + 1] - target_x[x], target_y[y + 1] - target_y[y])
            result.paste(tile.resize(target, Image.Resampling.NEAREST), (target_x[x], target_y[y]))
    return result


def preview(sprites, path):
    font = ImageFont.load_default(size=17)
    small = ImageFont.load_default(size=14)
    heights = {name: max(size[1] * 3, target[1]) + 58 for name, (size, _, target) in SPECS.items()}
    sheet = Image.new("RGBA", (1140, 105 + sum(heights.values())), (5, 12, 22, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((28, 18), "COSMIC HORIZON / HOLOGRAPHIC PIXEL UI", font=font, fill=CYAN)
    draw.text((28, 47), "LEFT: original pixels at 3x     RIGHT: fixed-border 9-slice (icons: nearest 4x)", font=small, fill="#a0bccd")
    draw.text((28, 69), "No blur. White bar fill is intended for code tinting. Labels are preview-only.", font=small, fill="#a0bccd")
    y = 100
    for name, (size, border, target) in SPECS.items():
        image = sprites[name]
        draw.line((28, y, 1110, y), fill="#163044")
        draw.text((28, y + 10), f"{name}.png  {size[0]}x{size[1]}  border {border}", font=small, fill="#c4dce8")
        sheet.alpha_composite(image.resize((size[0] * 3, size[1] * 3), Image.Resampling.NEAREST), (40, y + 38))
        expanded = nine_slice(image, target, border)
        sheet.alpha_composite(expanded, (460, y + 38))
        label = "9-SLICE " if border else "POINT SCALE "
        draw.text((460 + target[0] + 12, y + 39), label.strip(), font=small, fill="#688fa3")
        draw.text((460 + target[0] + 12, y + 58), f"{target[0]}x{target[1]}", font=small, fill="#688fa3")
        if name.startswith("ui_button") or name == "ui_dropdown":
            text = "1920 x 1080" if name == "ui_dropdown" else "START DIVE"
            draw.text((460 + target[0] // 2, y + 38 + target[1] // 2), text, anchor="mm", font=font, fill="#c4f9ff")
        y += heights[name]
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--preview", type=Path, default=Path.home() / "work/kit_preview.png")
    args = parser.parse_args()
    template = (UI / "hud_control_button_slider_01.png.meta").read_text()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    folder_meta = OUTPUT.with_suffix(".meta")
    if not folder_meta.exists():
        folder = re.sub(r"(?m)^guid: .*", "guid: " + uuid.uuid4().hex, UI.with_suffix(".meta").read_text())
        folder_meta.write_text("\n".join(line.rstrip() for line in folder.splitlines()) + "\n")
    sprites = {}
    for name, (size, border, _) in SPECS.items():
        image = sprites[name] = sprite(name, size)
        image.save(OUTPUT / (name + ".png"))
        meta = OUTPUT / (name + ".png.meta")
        meta.write_text(texture_meta(template, meta, border))
    preview(sprites, args.preview)
    print(f"UI kit: {len(sprites)} RGBA PNGs + importer metadata generated in {OUTPUT}")
    print(f"Preview: {args.preview}")


if __name__ == "__main__":
    main()
