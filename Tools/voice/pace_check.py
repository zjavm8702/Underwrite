"""
한마디의 빠르기 검사 — 글자(한글 음절)당 걸린 시간과 목소리 높이를 인물별로 본다(사장님: 말이 느리면 어색하다, 너무 낮으면 안 된다).
느린 줄(음절당 0.32초 넘음)을 「인물:감정_번호」 쉼표 목록으로 마지막 줄에 찍는다.

실행: ~/tts/asr/Scripts/python.exe Tools/voice/pace_check.py <인물...>
"""
import json
import os
import re
import sys

import librosa
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Resources", "Audio", "Voice"))
SLOW_PER_SYLLABLE = 0.32


def text_of(entry):
    return entry if isinstance(entry, str) else entry.get("text", "")


def main():
    spec = json.load(open(os.path.join(HERE, "barks_v1.json"), encoding="utf-8"))["voices"]
    slow = []

    for voice in sys.argv[1:]:
        paces = []
        pitches = []

        for emotion, lines in spec[voice]["barks"].items():
            for i, entry in enumerate(lines):
                syllables = max(1, len(re.findall(r"[가-힣]", text_of(entry))))
                wav, sr = librosa.load(os.path.join(GAME, voice, f"{emotion}_{i}.wav"), sr=16000)
                seconds = len(wav) / sr
                pace = seconds / syllables
                paces.append(pace)
                f0, voiced, _ = librosa.pyin(wav, fmin=60, fmax=700, sr=sr)
                f0 = f0[voiced] if voiced is not None else np.array([])

                if len(f0) > 0:
                    pitches.append(float(np.median(f0)))

                if pace > SLOW_PER_SYLLABLE and syllables >= 2:
                    slow.append(f"{voice}:{emotion}_{i}")

        print(f"{voice}: 음절당 {np.median(paces):.2f}초 · 높이 {np.median(pitches) if pitches else 0:.0f}Hz", flush=True)

    print(",".join(slow))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
