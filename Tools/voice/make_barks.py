"""
짧은 한마디(바크) 생성기 — Qwen3-TTS(Apache-2.0), 로컬 GPU.

1) VoiceDesign 모델로 인물마다 기준 목소리 한 줄(ref.wav)을 만든다 — 글로 설계, 남의 목소리를 베끼지 않는다.
2) Base 모델로 그 기준 목소리를 복제해 감정별 한마디를 뽑는다 — 한 인물의 목소리가 줄마다 달라지지 않게.
3) 앞뒤 무음을 자르고 최대 음량을 맞춘다.

실행(저장소 밖 가상환경): ~/tts/qwen3/Scripts/python.exe Tools/voice/make_barks.py [출력폴더] [인물...]
"""
import json
import os
import sys

import numpy as np
import soundfile as sf
import torch
from qwen_tts import Qwen3TTSModel

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = os.path.join(HERE, "barks_v1.json")
DESIGN_MODEL = "Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign"
BASE_MODEL = "Qwen/Qwen3-TTS-12Hz-1.7B-Base"
SEED = 1234
PEAK = 10 ** (-3 / 20)  # -3 dBFS
SILENCE = 10 ** (-40 / 20)  # -40 dBFS 아래는 무음으로 본다
PAD_SECONDS = 0.04


def trim_and_normalize(wav, sr):
    wav = np.asarray(wav, dtype=np.float32)
    loud = np.where(np.abs(wav) > SILENCE)[0]

    if len(loud) == 0:
        return wav, 0.0

    pad = int(PAD_SECONDS * sr)
    wav = wav[max(0, loud[0] - pad): min(len(wav), loud[-1] + pad)]
    peak = float(np.max(np.abs(wav)))
    wav = wav * (PEAK / peak)
    return wav, peak


def main():
    out_root = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "..", "..", "Outputs", "Voice_v1")
    only = set(sys.argv[2:])
    spec = json.load(open(SPEC, encoding="utf-8"))["voices"]
    names = [n for n in spec if len(only) == 0 or n in only]
    os.makedirs(out_root, exist_ok=True)
    report = {}

    # 1) 기준 목소리
    design = Qwen3TTSModel.from_pretrained(DESIGN_MODEL, device_map="cuda:0", dtype=torch.bfloat16)

    for name in names:
        torch.manual_seed(SEED)
        wavs, sr = design.generate_voice_design(text=spec[name]["ref_text"], language="Korean",
                                                instruct=spec[name]["design"])
        folder = os.path.join(out_root, name)
        os.makedirs(folder, exist_ok=True)
        sf.write(os.path.join(folder, "ref.wav"), wavs[0], sr)
        print("ref", name, round(len(wavs[0]) / sr, 2), "s", flush=True)

    del design
    torch.cuda.empty_cache()

    # 2) 복제로 한마디들
    base = Qwen3TTSModel.from_pretrained(BASE_MODEL, device_map="cuda:0", dtype=torch.bfloat16)

    for name in names:
        folder = os.path.join(out_root, name)
        ref, ref_sr = sf.read(os.path.join(folder, "ref.wav"), dtype="float32")
        prompt = base.create_voice_clone_prompt(ref_audio=(ref, ref_sr), ref_text=spec[name]["ref_text"],
                                                x_vector_only_mode=False)
        texts = []
        files = []

        for emotion, lines in spec[name]["barks"].items():
            for i, text in enumerate(lines):
                texts.append(text)
                files.append(f"{emotion}_{i}.wav")

        torch.manual_seed(SEED)
        wavs, sr = base.generate_voice_clone(text=texts, language=["Korean"] * len(texts), voice_clone_prompt=prompt)
        report[name] = {}

        for text, file, wav in zip(texts, files, wavs):
            clean, peak = trim_and_normalize(wav, sr)
            sf.write(os.path.join(folder, file), clean, sr)
            report[name][file] = {"text": text, "seconds": round(len(clean) / sr, 2), "raw_peak": round(peak, 3)}

        print("barks", name, len(files), flush=True)

    json.dump(report, open(os.path.join(out_root, "report.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
