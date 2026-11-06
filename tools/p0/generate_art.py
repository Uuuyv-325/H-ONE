"""Rebuild the seven original technical P0 sprites (Pillow required)."""
import argparse
import math
from pathlib import Path
from PIL import Image, ImageDraw

args = argparse.ArgumentParser()
args.add_argument("--overwrite", action="store_true", help="Replace only these P0 PNG files")
overwrite = args.parse_args().overwrite
root = Path(__file__).resolve().parents[2] / "Assets/Game/Art/P0"
root.mkdir(parents=True, exist_ok=True)
names = ("star", "ring", "stone", "fragment", "empty_node", "human", "dais")
if not overwrite and any((root / (name + ".png")).exists() for name in names):
    raise SystemExit("P0 files exist; use --overwrite only when intentionally regenerating them.")
gold = "#f9d37b"


def star(cx, cy, radius, inner):
    return [(cx + math.cos(-math.pi / 2 + i * math.pi / 5) * (radius if i % 2 == 0 else inner),
             cy + math.sin(-math.pi / 2 + i * math.pi / 5) * (radius if i % 2 == 0 else inner)) for i in range(10)]


def make(name, paint, size=128):
    image = Image.new("RGBA", (size * 3, size * 3))
    paint(ImageDraw.Draw(image), 3)
    image.resize((size, size), Image.Resampling.LANCZOS).save(root / (name + ".png"))


make("star", lambda d, s: d.polygon([(x*s, y*s) for x, y in star(64, 64, 51, 23)], fill=gold))
make("ring", lambda d, s: d.ellipse((8*s, 8*s, 120*s, 120*s), outline=gold, width=4*s))
make("stone", lambda d, s: (d.rectangle((0, 0, 128*s, 128*s), fill="#394b64"),
    d.rectangle((0, 0, 128*s, 7*s), fill="#8c9aa8"), d.line((7*s, 123*s, 123*s, 123*s), fill="#25354f", width=3*s)))
make("fragment", lambda d, s: (d.polygon([(19*s, 43*s), (83*s, 15*s), (115*s, 74*s), (73*s, 117*s), (12*s, 94*s)], fill="#be6653"),
    d.polygon([(x*s, y*s) for x, y in star(64, 64, 34, 16)], fill=gold)))
make("empty_node", lambda d, s: d.polygon([(19*s, 43*s), (83*s, 15*s), (115*s, 74*s), (73*s, 117*s), (12*s, 94*s), (19*s, 43*s)], outline=gold, width=4*s))


def person(d, s):
    d.polygon([(87*s, 91*s), (165*s, 91*s), (186*s, 214*s), (58*s, 214*s)], fill="#be6653")
    d.polygon([(87*s, 91*s), (121*s, 106*s), (151*s, 211*s), (70*s, 207*s)], fill="#d2a583")
    d.ellipse((94*s, 26*s, 159*s, 92*s), fill="#f1d5aa")
    d.line((90*s, 46*s, 107*s, 24*s, 148*s, 24*s, 165*s, 46*s), fill=gold, width=6*s)
    d.rectangle((86*s, 211*s, 111*s, 224*s), fill=gold)
    d.rectangle((143*s, 211*s, 168*s, 224*s), fill=gold)


make("human", person, 256)
make("dais", lambda d, s: (d.polygon([(5*s, 87*s), (64*s, 66*s), (123*s, 87*s), (112*s, 111*s), (16*s, 111*s)], fill="#182743"),
    d.line((5*s, 87*s, 64*s, 66*s, 123*s, 87*s), fill=gold, width=5*s), d.arc((18*s, 8*s, 110*s, 92*s), 20, 160, fill=gold, width=5*s)))
