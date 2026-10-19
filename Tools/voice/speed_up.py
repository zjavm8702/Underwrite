"""
이미 만든 한마디의 빠르기를 올린다(음 높이 유지) — 다시 만들지 않고 파일만 고친다. 대사 표의 speed도 같이 적어
나중에 다시 만들 때 같은 빠르기가 되게 한다(edit_barks.py가 읽는다).

실행: ~/tts/qwen3/Scripts/python.exe Tools/voice/speed_up.py <빠르기> <인물...> [--extra 인물:감정_번호=빠르기,...]
"""
import argparse
import collections
import json
import os

import librosa
import soundfile as sf

from make_barks import trim_and_normalize

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = os.path.join(HERE, "barks_v1.json")
GAME = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Resources", "Audio", "Voice"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("speed", type=float)
    parser.add_argument("voices", nargs="+")
    parser.add_argument("--extra", default="")
    args = parser.parse_args()
    extra = {}

    for item in [s for s in args.extra.split(",") if s]:
        key, value = item.split("=")
        extra[key] = float(value)

    spec = json.load(open(SPEC, encoding="utf-8"), object_pairs_hook=collections.OrderedDict)

    for voice in args.voices:
        for emotion, lines in spec["voices"][voice]["barks"].items():
            for i, entry in enumerate(lines):
                key = f"{voice}:{emotion}_{i}"
                speed = extra.get(key, args.speed)
                text = entry if isinstance(entry, str) else entry["text"]
                path = os.path.join(GAME, voice, f"{emotion}_{i}.wav")
                wav, sr = librosa.load(path, sr=None)
                fast = librosa.effects.time_stretch(wav, rate=speed)
                clean, _ = trim_and_normalize(fast, sr)
                sf.write(path, clean, sr)
                line = collections.OrderedDict([("text", text), ("speed", speed)])

                if not isinstance(entry, str) and entry.get("style"):
                    line["style"] = entry["style"]

                lines[i] = line

        print("빠르게", voice, flush=True)

    json.dump(spec, open(SPEC, "w", encoding="utf-8"), ensure_ascii=False, indent=2)


if __name__ == "__main__":
    main()
