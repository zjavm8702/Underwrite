"""
검수에서 대사와 덜 맞은 한마디(check.json의 low_match)를 씨앗을 바꿔 여러 벌 다시 뽑는다.
후보는 {감정}_{번호}.cand{k}.wav로 두고, check_barks.py --pick 이 가장 잘 맞는 것으로 바꿔 끼운다.

실행: ~/tts/qwen3/Scripts/python.exe Tools/voice/regen_barks.py <출력폴더> [후보 수]
"""
import json
import os
import sys

import soundfile as sf
import torch
from qwen_tts import Qwen3TTSModel

from make_barks import BASE_MODEL, SPEC, trim_and_normalize


def main():
    root = sys.argv[1]
    count = int(sys.argv[2]) if len(sys.argv) > 2 else 4
    check = json.load(open(os.path.join(root, "check.json"), encoding="utf-8"))
    spec = json.load(open(SPEC, encoding="utf-8"))["voices"]
    base = Qwen3TTSModel.from_pretrained(BASE_MODEL, device_map="cuda:0", dtype=torch.bfloat16)

    for voice, result in check.items():
        if len(result["low_match"]) == 0:
            continue

        folder = os.path.join(root, voice)
        ref, ref_sr = sf.read(os.path.join(folder, "ref.wav"), dtype="float32")
        prompt = base.create_voice_clone_prompt(ref_audio=(ref, ref_sr), ref_text=spec[voice]["ref_text"],
                                                x_vector_only_mode=False)

        for file in result["low_match"]:
            text = result["lines"][file]["text"]

            for k in range(count):
                torch.manual_seed(5000 + k * 17)
                wavs, sr = base.generate_voice_clone(text=text, language="Korean", voice_clone_prompt=prompt)
                clean, _ = trim_and_normalize(wavs[0], sr)
                sf.write(os.path.join(folder, file.replace(".wav", f".cand{k}.wav")), clean, sr)

            print("regen", voice, file, flush=True)


if __name__ == "__main__":
    main()
