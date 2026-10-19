# 외부 에셋 출처

게임에 들어간 외부 에셋과 라이선스. 상업 출시 전에 크레딧 화면을 만들 때 이 표를 쓴다.
**들일 때 라이선스 원문을 확인하고, CC0이거나 상업 사용이 명시된 것만 쓴다.** 돈이 드는 에셋은 사장님 확인 후(2026-10-18 인터뷰).

## 소리 (2026-10-18)

| 파일 (`Assets/Resources/Audio/`) | 원본 | 만든 이 | 라이선스 | 출처 |
|---|---|---|---|---|
| `Music/music_village.wav` | Loop_The_Old_Tower_Inn.wav (「Medieval: The Old Tower Inn」 반복판) | RandomMind | CC0 1.0 | https://opengameart.org/content/medieval-the-old-tower-inn |
| `Music/music_underground.ogg` | Unsolved-Investigation-isaiah658.ogg (「Unsolved Investigation」) | isaiah658 | CC0 1.0 | https://opengameart.org/content/unsolved-investigation |
| `Sfx/sfx_click.ogg` | click3.ogg (UI Audio) | Kenney | CC0 1.0 | https://kenney.nl/assets/ui-audio |
| `Sfx/sfx_page.ogg` | bookFlip2.ogg (RPG Audio) | Kenney | CC0 1.0 | https://kenney.nl/assets/rpg-audio |
| `Sfx/sfx_coins.ogg` | handleCoins.ogg (RPG Audio) | Kenney | CC0 1.0 | https://kenney.nl/assets/rpg-audio |
| `Sfx/sfx_door.ogg` | doorOpen_1.ogg (RPG Audio) | Kenney | CC0 1.0 | https://kenney.nl/assets/rpg-audio |
| `Sfx/sfx_close.ogg` | bookClose.ogg (RPG Audio) | Kenney | CC0 1.0 | https://kenney.nl/assets/rpg-audio |

CC0이라 표기 의무는 없지만, 크레딧에 이름을 넣는다(만든 이들이 고마워한다고 적어 둠).

## 그림

그림은 Codex(대리) 납품물이다(`Outputs/`). 외부 에셋을 바탕으로 쓴 것은 아래뿐이다(상시 지시서 §2-1 — 무료 에셋 먼저).

| 쓰인 곳 | 원본 | 만든 이 | 라이선스 | 출처 · 확인 |
|---|---|---|---|---|
| ~~지하 방 바닥(v3)~~ — **v5(10/19)에서 Codex가 직접 그린 바닥으로 바뀌어 지금은 쓰지 않음**. 기록용(`Outputs/UnderworldRoom_v3`) | Tileable Bricks Ground Textures – Set 1 (Ground_01~04, 무료판) — 줄이고 세로로 누르고 색을 낮춰 섞음 | Cethiel | CC0 1.0 | https://opengameart.org/content/tileable-bricks-ground-textures-set-1 · 과장이 10/18 제작자 페이지에서 CC0 확인, 원본 ZIP은 `work/cethiel_set1/`(커밋 안 함) |

## 목소리 (2026-10-19)

인물의 짧은 한마디(`Assets/Resources/Audio/Voice/`, 11명 × 감정 7 × 2 = 154개)는 과장이 로컬 GPU로 만들었다. 실존 인물의 목소리를 참조하지 않았다 — 인물마다 글로 설계한 기준 목소리(VoiceDesign)를 복제해 뽑았다.

| 쓰인 곳 | 모델 | 만든 이 | 라이선스 | 출처 |
|---|---|---|---|---|
| 기준 목소리 | Qwen3-TTS-12Hz-1.7B-VoiceDesign | Alibaba Qwen | Apache-2.0 | https://huggingface.co/Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign |
| 한마디 | Qwen3-TTS-12Hz-1.7B-Base | Alibaba Qwen | Apache-2.0 | https://huggingface.co/Qwen/Qwen3-TTS-12Hz-1.7B-Base |
| 검수(받아쓰기) | faster-whisper medium | SYSTRAN / OpenAI Whisper | MIT | https://github.com/SYSTRAN/faster-whisper |

스팀 출시 때 「AI 생성 콘텐츠」 공개 항목에 음성을 적는다. 생성 사양·대사: `Tools/voice/barks_v1.json`, 스크립트 `make_barks.py`·`regen_barks.py`·`check_barks.py`(가상환경은 저장소 밖 `~/tts`).
