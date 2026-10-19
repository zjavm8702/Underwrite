"""
앉은 투자꾼을 의자 방석에 맞춘다(과장, 2026-10-19 — 사장님 지적 「위치·몸이 하나도 안 맞는다」).

v2 배치표는 사람의 발밑을 의자 발밑(seat_ground)에 두었는데, 앉은 그림은 엉덩이가 발에서 옆·위로 떨어져 있어
사람이 의자 옆 허공에 앉았다. 프레임에서 엉덩이 자리(허벅지 줄의 등 쪽 끝)를 재서 방석 가운데(seats)에 맞춘다.

엉덩이 자리: 그림 아래쪽 60% 안에서 가로로 가장 넓은 줄(수평 허벅지) 무리의 아래 끝 높이,
등 쪽 끝(SE를 보면 왼쪽, SW면 오른쪽)에서 허벅지 굵기의 1/4만큼 안쪽.

실행: python Tools/art/seat_fit.py [합성 미리보기 png]
출력: Tools/art/seat_fit.json — 사람마다 seat_px(캔버스 위 원점)와 방에 놓을 발밑 좌표
"""
import json
import sys

import numpy as np
from PIL import Image

PEOPLE = "Assets/Images/Underworld/People"
STAGING = "Outputs/UnderworldPeople_v2/staging.json"
ROOM = "Outputs/UnderworldRoom_v6"
PIVOT_FROM_BOTTOM = 13


def seat_point(path, direction):
    alpha = np.asarray(Image.open(path).convert("RGBA"))[:, :, 3] > 32
    rows = np.where(alpha.any(axis=1))[0]
    top, bottom = rows[0], rows[-1]
    widths = alpha.sum(axis=1)
    lower = range(int(top + (bottom - top) * 0.4), bottom + 1)
    widest = max(widths[y] for y in lower)
    band = [y for y in lower if widths[y] >= widest * 0.85]
    seat_y = max(band)
    xs = np.where(alpha[seat_y - 6])[0]
    thickness = xs[-1] - xs[0]

    if direction == "SE":
        seat_x = xs[0] + thickness * 0.18
    else:
        seat_x = xs[-1] - thickness * 0.18

    return float(seat_x), float(seat_y)


def main():
    staging = json.load(open(STAGING, encoding="utf-8"))
    result = {}

    for place in staging["placements"]:
        name = place["name"]

        if name == "Informant":
            continue

        direction = place["direction"]
        path = f"{PEOPLE}/{name}/{name}_Idle_{direction}_00.png"
        canvas_w, canvas_h = Image.open(path).size
        sx, sy = seat_point(path, direction)
        scale = place["display_scale"]
        cushion = place["seat"]
        # 발밑(피봇) → 엉덩이까지의 거리(방 그림 px). 엉덩이를 방석에 놓으면 발밑은 그만큼 아래·옆.
        dx = (canvas_w / 2 - sx) * scale
        dy = ((canvas_h - PIVOT_FROM_BOTTOM) - sy) * scale
        foot = [cushion[0] + dx, cushion[1] + dy]
        result[name] = {"seat_px": [sx, sy], "foot": foot, "cushion": cushion, "old_foot": place["seat_ground"]}
        print(name, direction, "seat_px", round(sx), round(sy), "foot", [round(v, 1) for v in foot], "was", place["seat_ground"])

    json.dump(result, open("Tools/art/seat_fit.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)

    if len(sys.argv) > 1:
        preview(staging, result, sys.argv[1])


def preview(staging, result, out):
    room = Image.open(f"{ROOM}/room_plate.png").convert("RGBA")
    layout = json.load(open(f"{ROOM}/layout.json", encoding="utf-8"))

    for part in layout["parts"]:
        if part["id"].startswith("table_") and "light" not in part["id"]:
            img = Image.open(f"{ROOM}/{part['file']}").convert("RGBA")
            img = img.resize((img.width // 2, img.height // 2))
            px, py = part["pivot_in_png"][0] / 2, part["pivot_in_png"][1] / 2
            room.alpha_composite(img, (int(part["foot"][0] - px), int(part["foot"][1] - py)))

    for place in staging["placements"]:
        name = place["name"]

        if name not in result:
            continue

        img = Image.open(f"{PEOPLE}/{name}/{name}_Idle_{place['direction']}_00.png").convert("RGBA")
        s = place["display_scale"]
        img = img.resize((int(img.width * s), int(img.height * s)), Image.LANCZOS)
        fx, fy = result[name]["foot"]
        room.alpha_composite(img, (int(fx - img.width / 2), int(fy - (img.height - PIVOT_FROM_BOTTOM * s))))

    room.crop((650, 480, 1700, 1260)).save(out)


if __name__ == "__main__":
    main()
