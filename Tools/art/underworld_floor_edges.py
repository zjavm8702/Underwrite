"""
지하 방 바닥 v5 가장자리 처리(과장, 2026-10-19 — 사장님 지적 「바닥이 위·옆 다 잘렸다」).

v5 바닥은 직사각형 마스크 안에만 그려져
  - 위 벽(나무 띠) 밑에 예전 흐린 자갈 띠(약 30px)가 남았고
  - 벽과 맞닿는 곳마다 판석이 칼로 자른 듯 일직선으로 끝났다.
고치는 것:
  1) 자갈 띠를 Codex 원본 판석(floor_native_mosaic, 바닥판과 상관 0.99)으로 채운다. 기둥 받침·계단 출입구는 그대로
  2) 벽에 닿는 모든 가장자리에 접지 그림자 — 판석이 벽 밑으로 들어가 보이게. 화면 아래 끝은 벽이 아니므로 제외

실행: ~/tts/asr/Scripts/python.exe Tools/art/underworld_floor_edges.py <v5 폴더> <출력 png>
"""
import sys

import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt

# 위 벽 밑 자갈 띠가 있는 가로 구간(기둥 받침 x 700~785, 1435~1520과 계단 출입구 x 135~390은 뺀다)
STRIP_SPANS = [(395, 700), (785, 1435), (1520, 1670)]
STRIP_BOTTOM = 245
BEAM_SEARCH = (203, 228)  # 이 안에서 나무 띠 밑선(가장 어두운 줄)을 찾는다
STRIP_SHADE = 0.6  # 바닥판의 벽 쪽 첫 줄이 원본의 약 0.63배
AO_WIDTH = 34.0
AO_STRENGTH = 0.42


def main():
    folder, out = sys.argv[1], sys.argv[2]
    plate = np.asarray(Image.open(f"{folder}/room_plate.png").convert("RGB"), dtype=np.float32)
    mosaic = np.asarray(Image.open(f"{folder}/floor_native_mosaic.png").convert("RGB"), dtype=np.float32)
    mask = np.asarray(Image.open(f"{folder}/floor_mask.png").convert("L")) > 0
    gray = plate.mean(axis=2)
    filled = mask.copy()

    for x0, x1 in STRIP_SPANS:
        for x in range(x0, x1):
            column = gray[BEAM_SEARCH[0]:BEAM_SEARCH[1], x]
            start = BEAM_SEARCH[0] + int(np.argmin(column)) + 2
            plate[start:STRIP_BOTTOM, x] = mosaic[start:STRIP_BOTTOM, x] * STRIP_SHADE
            filled[start:STRIP_BOTTOM, x] = True

    # 벽까지의 거리. 화면 아래 끝 밖은 바닥이 이어진다고 보고 한 줄 덧댄다.
    padded = np.vstack([filled, np.ones((1, filled.shape[1]), dtype=bool)])
    distance = distance_transform_edt(padded)[:-1]
    t = np.clip(distance / AO_WIDTH, 0.0, 1.0)
    shade = 1.0 - AO_STRENGTH * (1.0 - t) ** 2
    shade[~filled] = 1.0
    plate = plate * shade[:, :, None]

    Image.fromarray(np.clip(plate, 0, 255).astype(np.uint8)).save(out)
    changed = int(((np.asarray(Image.open(f"{folder}/room_plate.png").convert("RGB")) != np.clip(plate, 0, 255).astype(np.uint8)).any(axis=2)).sum())
    print("filled", int(filled.sum() - mask.sum()), "changed", changed)


if __name__ == "__main__":
    main()
