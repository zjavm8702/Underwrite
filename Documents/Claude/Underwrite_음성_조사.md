# 음성 조사 — 무료·로컬 한국어 TTS (2026-10-19, 과장 · 사장님 결정 대기)

데모 목표(「음성 추가」)를 돈을 쓰지 않고 할 수 있는지 사원(서브 에이전트)이 조사했다. 설치·다운로드는 하지 않았다. 품질은 공개 데모와 설명을 근거로 한 추정이며 직접 들어 보지 않았다.

## 후보

| 이름 | 한국어 | 음색 제어 | 상업 이용 | GPU | 비고 |
|---|---|---|---|---|---|
| **Qwen3-TTS** 1.7B | 높음(추정), 감정 지시 | 기본 화자 9명, 글로 음색 설계, 3초 참조 복제 | Apache-2.0 **가능** | 필요 | **1순위** — 남의 목소리 없이 캐릭터 음색을 만들 수 있다 |
| **Supertonic 3** (Supertone) | 중상(추정), 웃음·숨 태그 | 기본 스타일 몇 개 | OpenRAIL-M **가능** — AI 생성 표시 의무 | 불필요(CPU) | 2순위 |
| Fun-CosyVoice 3 | 중상 | 참조 복제 | Apache-2.0 가능 | 필요 | |
| GPT-SoVITS | 참조에 따라 좋음 | 복제·미세조정 | MIT 가능 | 필요 | |
| MeloTTS-Korean | 보통, 감정 약함 | 화자 1명 | MIT 가능 | 불필요 | |
| Fish Speech | 좋음 | 복제 | 연매출 50만 달러 미만만 · S2-Pro는 연구용 | — | **위험** |
| Coqui XTTS-v2 | 보통 | 복제 | 비상업 전용 | — | **불가** |
| Kokoro · Piper | 한국어 없음 | — | — | — | 불가 |

데이터: KSS는 CC BY-NC-SA(상업 불가 — KSS로 미세조정한 모델도 피한다). 가장 안전한 참조 음원은 직접 녹음·서면 동의를 받은 목소리.

## 과장 제안

- **데모는 바크(짧은 한마디)만** — 인물마다 감정별 한마디 3~5개(인사·놀람·기쁨·걱정·한숨). 대사 전부를 읽히면 억양 어색함이 오래 드러나고, 대사를 고칠 때마다 다시 뽑아야 한다. 대화창이 줄을 넘길 때 그 줄의 감정(`DialogueLine.Emotion`)에 맞는 한마디를 튼다
- 도구는 **Qwen3-TTS VoiceDesign**(글로 음색 설계, 복제 없음). 미리 wav로 뽑아 `Assets/Audio/Voice/`에 넣는다 — 게임에 모델을 싣지 않는다
- 스팀 출시 때 AI 생성 콘텐츠 공개, 크레딧에 모델·라이선스 표기(`Underwrite_에셋_출처.md`)

## 사장님께 확인할 것

1. 바크만으로 시작해도 되는가(전체 낭독은 나중)
2. 이 PC에 NVIDIA GPU가 있는가 — 없으면 Supertonic 3(CPU)으로
3. 모델 내려받기(수 GB, 무료)를 해도 되는가

출처: QwenLM/Qwen3-TTS, Supertone/supertonic-3(LICENSE), FunAudioLLM/CosyVoice, RVC-Boss/GPT-SoVITS, myshell-ai/MeloTTS, fishaudio/fish-speech(LICENSE), Coqui CPML, KSS 논문(arXiv 2107.02875). 출시 직전 각 LICENSE 원문을 다시 확인한다(법률 자문 아님).
