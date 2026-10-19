"""
기준 목소리 오디션 — 인물마다 목소리 설명으로 후보를 여러 개 만들고, 목표 음 높이(barks_v1.json의 "pitch": [낮음, 높음])에
가장 맞는 것을 refs/<인물>.wav로 고른다. 과장은 소리를 들을 수 없어 높이로 고른다 — 나이·자연스러움은 사장님이 듣고 판단.
후보는 Outputs/Voice_v2/auditions/<인물>/ 에 남긴다(들어 보고 다른 것을 고를 수 있게).

실행: ~/tts/qwen3/Scripts/python.exe Tools/voice/audition_voices.py <인물...> [--count 6]
"""
import argparse
import json
import os
import shutil
import sys

import librosa
import numpy as np
import soundfile as sf
import torch
from qwen_tts import Qwen3TTSModel

from make_barks import DESIGN_MODEL, SPEC

HERE = os.path.dirname(os.path.abspath(__file__))
REFS = os.path.join(HERE, "refs")
AUDITIONS = os.path.normpath(os.path.join(HERE, "..", "..", "Outputs", "Voice_v2", "auditions"))


def median_pitch(wav, sr):
    wav16 = librosa.resample(np.asarray(wav, dtype=np.float32), orig_sr=sr, target_sr=16000)
    f0, voiced, _ = librosa.pyin(wav16, fmin=60, fmax=700, sr=16000)
    f0 = f0[voiced] if voiced is not None else np.array([])
    return float(np.median(f0)) if len(f0) > 0 else 0.0


def score(pitch, low, high):
    # 범위 안이면 가운데에 가까울수록, 밖이면 벗어난 만큼 나쁘다.
    middle = (low + high) / 2.0
    if low <= pitch <= high:
        return abs(pitch - middle) / (high - low)
    return 1.0 + min(abs(pitch - low), abs(pitch - high)) / (high - low)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("voices", nargs="+")
    parser.add_argument("--count", type=int, default=6)
    args = parser.parse_args()
    spec = json.load(open(SPEC, encoding="utf-8"))["voices"]
    design = Qwen3TTSModel.from_pretrained(DESIGN_MODEL, device_map="cuda:0", dtype=torch.bfloat16)
    report = {}

    for voice in args.voices:
        low, high = spec[voice]["pitch"]
        folder = os.path.join(AUDITIONS, voice)
        os.makedirs(folder, exist_ok=True)
        results = []

        for k in range(args.count):
            seed = 9000 + k * 31
            torch.manual_seed(seed)
            wavs, sr = design.generate_voice_design(text=spec[voice]["ref_text"], language="Korean",
                                                    instruct=spec[voice]["design"])
            path = os.path.join(folder, f"cand{k}_seed{seed}.wav")
            sf.write(path, wavs[0], sr)
            pitch = median_pitch(wavs[0], sr)
            results.append((score(pitch, low, high), pitch, path))
            print(f"{voice} cand{k} {pitch:.0f}Hz (목표 {low}~{high})", flush=True)

        results.sort()
        best = results[0]
        shutil.copyfile(best[2], os.path.join(REFS, f"{voice}.wav"))
        report[voice] = {"target": [low, high], "chosen": os.path.basename(best[2]), "pitch": round(best[1]),
                         "candidates": [{"file": os.path.basename(r[2]), "pitch": round(r[1])} for r in results]}
        print(f"선택 {voice}: {os.path.basename(best[2])} {best[1]:.0f}Hz", flush=True)

    json.dump(report, open(os.path.join(AUDITIONS, "report.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
