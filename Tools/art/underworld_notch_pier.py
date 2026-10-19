"""
지하 방 오른쪽 위 꺾인 곳의 돌기둥 옆면(지도 x 1670~1751)을 한 그림으로 맞춘다(과장, 2026-10-19 — 사장님 지적 「어긋나 있는 부분」).

v6에서 이 칸에 흐린 예전 자갈(y 260~383)과 Codex가 새로 그린 선명한 깎은 돌 덩어리(y 383~515)가 위아래로 붙어 어긋나 보였다.
새 돌 덩어리의 몸통을 줄눈(y 416·494)에 맞춰 위로 이어 쌓고, 맨 위(나무 띠 바로 밑)에는 덩어리의 갓돌 줄을 얹는다.
돌이 줄눈이 아닌 곳에서 잘리지 않게 줄눈과 줄눈 사이 구간만 되풀이한다.

실행: python Tools/art/underworld_notch_pier.py <입력 png> <출력 png>
"""
import sys

import numpy as np
from PIL import Image

X0, X1 = 1670, 1752          # 기둥 옆면(왼쪽 어두운 면 + 앞면). 오른쪽 나무 기둥(x 1752~)은 건드리지 않는다
TOP = 260                    # 나무 띠 밑선 바로 아래
CAP = (380, 392)             # 새 덩어리의 갓돌 줄
COURSE = (416, 494)          # 줄눈에서 줄눈까지 — 돌 두 단
KEEP_FROM = 416              # 이 아래(원래 새 덩어리)는 그대로


def main():
    src, out = sys.argv[1], sys.argv[2]
    plate = np.asarray(Image.open(src).convert("RGB")).copy()
    course = plate[COURSE[0]:COURSE[1], X0:X1].copy()
    cap = plate[CAP[0]:CAP[1], X0:X1].copy()

    # 아래에서 위로 돌 두 단씩 쌓는다.
    bottom = KEEP_FROM

    while bottom > TOP:
        top = max(TOP, bottom - course.shape[0])
        plate[top:bottom, X0:X1] = course[course.shape[0] - (bottom - top):]
        bottom = top

    # 나무 띠 밑에 갓돌 줄을 얹는다 — 위에서 잘린 돌 단을 덮는다.
    plate[TOP:TOP + cap.shape[0], X0:X1] = cap

    # 갓돌 바로 밑에 나무 띠가 드리우는 얕은 그늘.
    shade = np.linspace(0.72, 1.0, 14)[:, None, None]
    region = plate[TOP + cap.shape[0]:TOP + cap.shape[0] + 14, X0:X1].astype(np.float32)
    plate[TOP + cap.shape[0]:TOP + cap.shape[0] + 14, X0:X1] = np.clip(region * shade, 0, 255).astype(np.uint8)

    Image.fromarray(plate).save(out)


if __name__ == "__main__":
    main()
