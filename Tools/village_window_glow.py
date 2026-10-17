"""영지 건물의 「밤 창문」 그림을 만든다 (2026-10-17, 밤 분위기).

색으로 창유리를 찾는 방식은 지붕·이끼·노란 버섯까지 켜 버려서, 창 자리를 그림을 보고 손으로 적었다.
아래 WINDOWS의 상자는 **건물 PNG(2배 그림) 픽셀**이다. 상자 안에서 창틀(갈색 나무)은 빼고 유리만 따뜻하게 칠하고,
둘레를 은은하게 번지게 한 그림을 원본과 같은 크기로 Night/에 저장한다. Unity는 같은 피벗으로 건물 위에 겹치고 밤에만 켠다.

그림이 바뀌면 상자를 다시 잰다. 쓰는 법: python -I Tools/village_window_glow.py
"""
import colorsys
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join('Assets', 'Images', 'Village', 'Full')
OUT = os.path.join(ROOT, 'Night')
WARM = (255, 196, 110)

# 잰 화면이 원본보다 작게 보였던 건물은 (표시 너비, 상자들)로 적고 원본 너비에 맞춰 늘린다.
# 'e'가 붙은 상자는 둥근 창이다.
WINDOWS = {
    'inn.png': (1100, [(355, 270, 420, 360), (620, 330, 675, 410), (160, 375, 210, 440), (150, 570, 205, 660),
                       (615, 560, 670, 655), (985, 345, 1020, 430)]),
    'manor.png': (801, [(350, 245, 385, 305), (110, 425, 145, 525), (170, 425, 205, 525), (310, 395, 350, 490),
                        (380, 395, 420, 490), (545, 410, 580, 515), (610, 410, 648, 515), (100, 650, 140, 770),
                        (165, 650, 205, 770), (545, 640, 580, 765), (610, 640, 650, 765)]),
    'guild.png': (981, [(345, 185, 405, 255), (270, 375, 440, 470), (615, 370, 670, 470), (230, 650, 365, 765),
                        (860, 405, 885, 505), (830, 675, 855, 760)]),
    'smith.png': (1100, [(340, 270, 375, 335), (975, 545, 1000, 655)]),
    'human_a.png': (833, [(325, 155, 370, 215), (135, 440, 180, 495), (555, 445, 605, 500)]),
    'human_b.png': (1100, [(165, 410, 235, 505), (675, 415, 745, 510)]),
    'human_c.png': (693, [(255, 225, 295, 285), (110, 485, 145, 545), (455, 475, 490, 540), (620, 490, 640, 560)]),
    'stone_0.png': (864, [(545, 445, 610, 530)]),
    'stone_1.png': (630, [(100, 335, 133, 378), (540, 380, 560, 418)]),
    'stone_2.png': (1100, [(175, 425, 210, 500), (725, 420, 770, 500), (965, 455, 990, 535)]),
    'elf_0.png': (644, [(365, 300, 410, 430, 'e'), (85, 690, 135, 800, 'e'), (520, 700, 560, 805, 'e'),
                        (495, 520, 525, 585, 'e')]),
    'elf_1.png': (629, [(225, 365, 305, 445, 'e'), (80, 615, 140, 715, 'e'), (405, 490, 450, 555, 'e'),
                        (520, 640, 550, 710, 'e')]),
    'elf_2.png': (714, [(150, 590, 215, 700, 'e'), (485, 580, 560, 700, 'e'), (330, 395, 430, 440, 'e')]),
    'fairy_0.png': (None, [(200, 165, 240, 222, 'e')]),
    'fairy_1.png': (None, [(216, 194, 264, 256, 'e')]),
    'fairy_2.png': (None, [(247, 141, 297, 206, 'e'), (145, 169, 182, 206, 'e')]),
    'fairy_3.png': (None, [(77, 185, 127, 237, 'e')]),
    'fairy_4.png': (None, [(222, 181, 260, 237, 'e')]),
}


def is_wood(r, g, b):
    hue, sat, val = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return hue < 0.12 and sat > 0.35 and val < 0.5


def build(name, shown_width, boxes):
    im = Image.open(os.path.join(ROOT, name)).convert('RGBA')
    w, h = im.size
    scale = 1.0 if shown_width is None else w / shown_width
    shape = Image.new('L', (w, h), 0)
    draw = ImageDraw.Draw(shape)
    centers = []
    for box in boxes:
        x0, y0, x1, y1 = [round(v * scale) for v in box[:4]]
        if len(box) > 4:
            draw.ellipse((x0, y0, x1, y1), fill=255)
        else:
            draw.rounded_rectangle((x0, y0, x1, y1), radius=max(2, (x1 - x0) // 8), fill=255)
        centers.append([(x0 + x1) // 2, (y0 + y1) // 2])
    # 창틀·창살(갈색 나무)과 그림 밖은 켜지 않는다. 유리만 밝힌다.
    px = im.load()
    sp = shape.load()
    for y in range(h):
        for x in range(w):
            if sp[x, y] == 0:
                continue
            r, g, b, a = px[x, y]
            if a < 250 or is_wood(r, g, b) is True:
                sp[x, y] = 0
    halo = shape.filter(ImageFilter.MaxFilter(7)).filter(ImageFilter.GaussianBlur(12)).point(lambda v: int(v * 0.5))
    alpha = Image.composite(shape.point(lambda v: int(v * 0.92)), halo, shape)
    alpha = Image.composite(alpha, Image.new('L', (w, h), 0), im.getchannel('A'))
    glow = Image.new('RGBA', (w, h), WARM + (0,))
    glow.putalpha(alpha)
    return glow, centers


def main():
    os.makedirs(OUT, exist_ok=True)
    summary = {}
    for name, (shown, boxes) in WINDOWS.items():
        glow, centers = build(name, shown, boxes)
        glow.save(os.path.join(OUT, name.replace('.png', '_night.png')))
        summary[name] = {'windows': len(centers), 'centers': centers}
    with open(os.path.join(OUT, 'windows.json'), 'w', encoding='utf-8') as f:
        json.dump(summary, f, ensure_ascii=False, indent=1)
    print(len(summary), 'buildings')


if __name__ == '__main__':
    sys.exit(main())
