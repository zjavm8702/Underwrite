"""
바크 검수 — 들을 수 없는 쪽(과장)이 숫자로 본다.
1) 음성인식(faster-whisper)으로 다시 받아 적어 대사와 얼마나 같은지(글자 유사도)
2) 목소리 높이(중앙 기본 주파수, pyin) — 인물 설계(낮은 목소리·높은 목소리)와 맞는지, 한 인물 안에서 들쭉날쭉하지 않은지
3) 길이

실행: ~/tts/asr/Scripts/python.exe Tools/voice/check_barks.py <출력폴더...>
"""
import difflib
import json
import os
import re
import sys

import librosa
import numpy as np
from faster_whisper import WhisperModel


def norm(text):
    return re.sub(r"[^가-힣a-zA-Z0-9]", "", text)


def heard_ratio(model, path, text):
    wav, _ = librosa.load(path, sr=16000)
    segments, _ = model.transcribe(wav, language="ko", beam_size=5)
    heard = "".join(s.text for s in segments).strip()
    return difflib.SequenceMatcher(None, norm(text), norm(heard)).ratio(), heard


def pick(model, root):
    """regen_barks.py가 뽑은 후보 가운데 대사와 가장 잘 맞는 것을 원래 자리에 넣고 후보는 지운다."""
    check = json.load(open(os.path.join(root, "check.json"), encoding="utf-8"))

    for voice, result in check.items():
        folder = os.path.join(root, voice)

        for file in result["low_match"]:
            text = result["lines"][file]["text"]
            best = (result["lines"][file]["match"], None, result["lines"][file]["heard"])

            for cand in sorted(os.listdir(folder)):
                if cand.startswith(file.replace(".wav", ".cand")) == False:
                    continue

                ratio, heard = heard_ratio(model, os.path.join(folder, cand), text)

                if ratio > best[0]:
                    best = (ratio, cand, heard)

            if best[1] is not None:
                os.replace(os.path.join(folder, best[1]), os.path.join(folder, file))

            for cand in os.listdir(folder):
                if ".cand" in cand:
                    os.remove(os.path.join(folder, cand))

            print("pick", voice, file, round(best[0], 2), best[2], flush=True)


def main():
    model = WhisperModel("medium", device="cpu", compute_type="int8")

    if sys.argv[1] == "--pick":
        pick(model, sys.argv[2])
        return
    result = {}

    for root in sys.argv[1:]:
        report = json.load(open(os.path.join(root, "report.json"), encoding="utf-8"))

        for voice, files in report.items():
            pitches = []
            rows = {}

            for file, info in files.items():
                path = os.path.join(root, voice, file)
                wav, sr = librosa.load(path, sr=16000)
                segments, _ = model.transcribe(wav, language="ko", beam_size=5)
                heard = "".join(s.text for s in segments).strip()
                ratio = difflib.SequenceMatcher(None, norm(info["text"]), norm(heard)).ratio()
                f0, voiced, _ = librosa.pyin(wav, fmin=60, fmax=700, sr=sr)
                f0 = f0[voiced] if voiced is not None else np.array([])
                pitch = float(np.median(f0)) if len(f0) > 0 else 0.0
                pitches.append(pitch)
                rows[file] = {"text": info["text"], "heard": heard, "match": round(ratio, 2),
                              "pitch_hz": round(pitch), "seconds": info["seconds"]}

            valid = [p for p in pitches if p > 0]
            result[voice] = {"median_pitch_hz": round(float(np.median(valid))) if valid else 0,
                             "pitch_spread_hz": round(float(np.std(valid))) if valid else 0,
                             "low_match": [f for f, r in rows.items() if r["match"] < 0.6],
                             "lines": rows}
            print(voice, result[voice]["median_pitch_hz"], "Hz ±", result[voice]["pitch_spread_hz"],
                  "low:", result[voice]["low_match"], flush=True)

    out = os.path.join(sys.argv[1], "check.json")
    json.dump(result, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
