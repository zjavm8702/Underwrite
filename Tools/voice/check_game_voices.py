"""
게임에 들어간 한마디(Assets/Resources/Audio/Voice)를 받아쓰기로 검사한다 — 대사(barks_v1.json)와 얼마나 같은지.
맞음 0.6 미만인 줄을 「인물:감정_번호」 쉼표 목록으로 마지막 줄에 찍는다(edit_barks.py --lines 에 그대로 넣는다).

실행: ~/tts/asr/Scripts/python.exe Tools/voice/check_game_voices.py <인물...>
"""
import difflib
import json
import os
import re
import sys

import librosa
from faster_whisper import WhisperModel

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Resources", "Audio", "Voice"))


def norm(text):
    return re.sub(r"[^가-힣a-zA-Z0-9]", "", text)


def text_of(entry):
    return entry if isinstance(entry, str) else entry.get("text", "")


def main():
    spec = json.load(open(os.path.join(HERE, "barks_v1.json"), encoding="utf-8"))["voices"]
    model = WhisperModel("medium", device="cpu", compute_type="int8")
    low = []

    for voice in sys.argv[1:]:
        scores = []

        for emotion, lines in spec[voice]["barks"].items():
            for i, entry in enumerate(lines):
                text = text_of(entry)
                wav, _ = librosa.load(os.path.join(GAME, voice, f"{emotion}_{i}.wav"), sr=16000)
                segments, _ = model.transcribe(wav, language="ko", beam_size=5)
                heard = "".join(s.text for s in segments).strip()
                ratio = difflib.SequenceMatcher(None, norm(text), norm(heard)).ratio()
                scores.append(ratio)

                if ratio < 0.6:
                    low.append(f"{voice}:{emotion}_{i}")
                    print(f"  낮음 {voice}/{emotion}_{i} 「{text}」 → 「{heard}」 {ratio:.2f}", flush=True)

        print(f"{voice}: 평균 맞음 {sum(scores) / len(scores):.2f}", flush=True)

    print(",".join(low))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
