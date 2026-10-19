"""
에디터 「음성·대사 편집」 창이 부르는 생성기. barks_v1.json을 읽어 지정한 줄만 다시 만들고 게임 폴더에 바로 쓴다.

  --lines edwin:happy_0,fio:sad_1   그 줄만 다시
  --redesign edwin                  목소리 설명으로 기준 목소리를 새로 만든 뒤 그 사람의 줄을 전부 다시
  --seed N                          씨앗(같은 대사인데 다른 느낌으로 다시 뽑을 때 바꾼다)

줄마다(barks_v1.json의 줄이 객체일 때):
  speed  음 높이는 두고 빠르기만(1.3 = 1.3배 빠르게)
  style  연기 지시(영어, 예: "laugh heartily, a quick natural chuckle").
         복제 모델은 지시를 못 받으므로 이 줄은 목소리 설계 모델로 「목소리 설명 + 지시」를 읽혀 만든다.
         글자를 끊어 읽던 웃음(「허.허」)을 사람 웃음처럼 낼 때 쓴다. 목소리가 기준과 조금 다를 수 있다
지시가 없는 줄은 기준 목소리 refs/<인물>.wav를 복제한다.

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

from make_barks import BASE_MODEL, DESIGN_MODEL, SPEC, change_speed, line_of, trim_and_normalize

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
REFS = os.path.join(HERE, "refs")
GAME = os.path.join(PROJECT, "Assets", "Resources", "Audio", "Voice")


def say(text):
    print(text, flush=True)


def entry_of(spec, voice, file):
    emotion, index = file.rsplit("_", 1)
    return line_of(spec[voice]["barks"][emotion][int(index)])


def write(voice, file, text, wav, sr, speed):
    clean, _ = trim_and_normalize(change_speed(wav, speed), sr)
    os.makedirs(os.path.join(GAME, voice), exist_ok=True)
    sf.write(os.path.join(GAME, voice, f"{file}.wav"), clean, sr)
    say(f"완료 {voice}/{file} 「{text}」 {len(clean) / sr:.2f}초")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--lines", default="")
    parser.add_argument("--redesign", default="")
    parser.add_argument("--seed", type=int, default=1234)
    args = parser.parse_args()
    spec = json.load(open(SPEC, encoding="utf-8"))["voices"]

    work = {}

    for item in [s for s in args.lines.split(",") if s]:
        voice, file = item.split(":")
        work.setdefault(voice, []).append(file)

    if args.redesign:
        work[args.redesign] = [f"{e}_{i}" for e, lines in spec[args.redesign]["barks"].items() for i in range(len(lines))]

    if len(work) == 0:
        say("다시 만들 줄이 없습니다.")
        return

    # 지시가 붙은 줄과 복제할 줄을 나눈다.
    styled = {}
    cloned = {}

    for voice, files in work.items():
        for file in files:
            target = styled if entry_of(spec, voice, file)[2] else cloned
            target.setdefault(voice, []).append(file)

    if args.redesign or styled:
        say("목소리 설계 모델을 불러오는 중…")
        design = Qwen3TTSModel.from_pretrained(DESIGN_MODEL, device_map="cuda:0", dtype=torch.bfloat16)

        if args.redesign:
            voice = args.redesign
            torch.manual_seed(args.seed)
            wavs, sr = design.generate_voice_design(text=spec[voice]["ref_text"], language="Korean",
                                                    instruct=spec[voice]["design"])
            sf.write(os.path.join(REFS, f"{voice}.wav"), wavs[0], sr)
            say(f"기준 목소리 새로 만듦: {voice} ({len(wavs[0]) / sr:.1f}초)")

        for voice, files in styled.items():
            for file in files:
                text, speed, style = entry_of(spec, voice, file)
                torch.manual_seed(args.seed)
                wavs, sr = design.generate_voice_design(text=text, language="Korean",
                                                        instruct=spec[voice]["design"] + " " + style)
                write(voice, file, text, wavs[0], sr, speed)

        del design
        torch.cuda.empty_cache()

    if cloned:
        say("복제 모델을 불러오는 중…")
        base = Qwen3TTSModel.from_pretrained(BASE_MODEL, device_map="cuda:0", dtype=torch.bfloat16)

        for voice, files in cloned.items():
            ref, ref_sr = sf.read(os.path.join(REFS, f"{voice}.wav"), dtype="float32")
            prompt = base.create_voice_clone_prompt(ref_audio=(ref, ref_sr), ref_text=spec[voice]["ref_text"],
                                                    x_vector_only_mode=False)
            entries = [entry_of(spec, voice, file) for file in files]
            torch.manual_seed(args.seed)
            wavs, sr = base.generate_voice_clone(text=[e[0] for e in entries], language=["Korean"] * len(entries),
                                                 voice_clone_prompt=prompt)

            for file, entry, wav in zip(files, entries, wavs):
                write(voice, file, entry[0], wav, sr, entry[1])

    say("DONE")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
