"""
에디터 「음성·대사 편집」 창이 부르는 생성기. barks_v1.json을 읽어 지정한 줄만 다시 만들고 게임 폴더에 바로 쓴다.

  --lines edwin:happy_0,fio:sad_1   그 줄만 다시(기준 목소리 refs/<인물>.wav를 복제)
  --redesign edwin                  목소리 설명으로 기준 목소리를 새로 만든 뒤 그 사람의 줄을 전부 다시
  --seed N                          씨앗(같은 대사인데 다른 느낌으로 다시 뽑을 때 바꾼다)

출력: Assets/Resources/Audio/Voice/<인물>/<감정>_<번호>.wav (덮어쓰기 — .meta는 그대로)
실행: ~/tts/qwen3/Scripts/python.exe Tools/voice/edit_barks.py ...
"""
import argparse
import json
import os
import sys

import soundfile as sf
import torch
from qwen_tts import Qwen3TTSModel

from make_barks import BASE_MODEL, DESIGN_MODEL, SPEC, trim_and_normalize

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
REFS = os.path.join(HERE, "refs")
GAME = os.path.join(PROJECT, "Assets", "Resources", "Audio", "Voice")


def say(text):
    print(text, flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--lines", default="")
    parser.add_argument("--redesign", default="")
    parser.add_argument("--seed", type=int, default=1234)
    args = parser.parse_args()
    spec = json.load(open(SPEC, encoding="utf-8"))["voices"]

    # 인물별로 다시 만들 줄 모으기
    work = {}

    for item in [s for s in args.lines.split(",") if s]:
        voice, file = item.split(":")
        work.setdefault(voice, []).append(file)

    if args.redesign:
        voice = args.redesign
        say(f"목소리 설계 모델을 불러오는 중… ({voice})")
        design = Qwen3TTSModel.from_pretrained(DESIGN_MODEL, device_map="cuda:0", dtype=torch.bfloat16)
        torch.manual_seed(args.seed)
        wavs, sr = design.generate_voice_design(text=spec[voice]["ref_text"], language="Korean",
                                                instruct=spec[voice]["design"])
        sf.write(os.path.join(REFS, f"{voice}.wav"), wavs[0], sr)
        say(f"기준 목소리 새로 만듦: {voice} ({len(wavs[0]) / sr:.1f}초)")
        del design
        torch.cuda.empty_cache()
        work[voice] = [f"{e}_{i}" for e, lines in spec[voice]["barks"].items() for i in range(len(lines))]

    if len(work) == 0:
        say("다시 만들 줄이 없습니다.")
        return

    say("복제 모델을 불러오는 중…")
    base = Qwen3TTSModel.from_pretrained(BASE_MODEL, device_map="cuda:0", dtype=torch.bfloat16)

    for voice, files in work.items():
        ref, ref_sr = sf.read(os.path.join(REFS, f"{voice}.wav"), dtype="float32")
        prompt = base.create_voice_clone_prompt(ref_audio=(ref, ref_sr), ref_text=spec[voice]["ref_text"],
                                                x_vector_only_mode=False)
        texts = []

        for file in files:
            emotion, index = file.rsplit("_", 1)
            texts.append(spec[voice]["barks"][emotion][int(index)])

        torch.manual_seed(args.seed)
        wavs, sr = base.generate_voice_clone(text=texts, language=["Korean"] * len(texts), voice_clone_prompt=prompt)
        os.makedirs(os.path.join(GAME, voice), exist_ok=True)

        for file, text, wav in zip(files, texts, wavs):
            clean, _ = trim_and_normalize(wav, sr)
            sf.write(os.path.join(GAME, voice, f"{file}.wav"), clean, sr)
            say(f"완료 {voice}/{file} 「{text}」 {len(clean) / sr:.2f}초")

    say("DONE")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
