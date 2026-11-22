import sys
import unittest
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from generate_grapple_anchor import key_magenta


class GrappleArtTests(unittest.TestCase):
    def test_crop_and_scale(self):
        source = Image.new("RGBA", (16, 24), (255, 0, 255, 255))
        ImageDraw.Draw(source).rectangle((4, 4, 11, 19), fill=(32, 50, 70, 255))
        result = key_magenta(source)
        self.assertEqual(result.size, (48, 96))
        self.assertEqual(result.getpixel((24, 48)), (32, 50, 70, 255))

    def test_noisy_matte_holes_and_fringe(self):
        source = Image.new("RGBA", (24, 24), (240, 28, 240, 255))
        draw = ImageDraw.Draw(source)
        draw.rectangle((4, 4, 19, 19), fill=(77, 40, 107, 255))
        draw.rectangle((6, 6, 17, 17), fill=(32, 50, 70, 255))
        draw.rectangle((9, 9, 14, 14), fill=(255, 0, 255, 255))
        result = key_magenta(source)
        self.assertEqual(result.getpixel((48, 48)), (0, 0, 0, 0))
        self.assertLess(result.getpixel((0, 0))[3], 255)
        for y in range(result.height):
            for x in range(result.width):
                r, g, b, a = result.getpixel((x, y))
                if a:
                    self.assertFalse(r > g + 8 and b > g + 8)

    def test_empty_foreground(self):
        for color in [(255, 0, 255, 255), (0, 0, 0, 0)]:
            with self.assertRaises(ValueError):
                key_magenta(Image.new("RGBA", (8, 8), color))


if __name__ == "__main__":
    unittest.main()
