# Documents 폴더 구조

문서를 **읽는 쪽**이 아니라 **관리하는 쪽**으로 나눈다. 읽기는 누구나 제한 없이 한다.

| 폴더 | 관리 주체 | 내용 |
|---|---|---|
| `Shared/` | 사용자 (사장) | 게임 기획. 두 에이전트가 모두 따른다 |
| `Codex/` | Codex (대리) | 아트·애니메이션 규칙과 아트 조사 기록 |
| `Claude/` | Claude Code (과장) | 코드 규칙과 개발 문서 |

## Shared/ — 기획

| 문서 | 비고 |
|---|---|
| `Shared/Underwrite_기획서_v0.7.md` | **현행판.** §11이 시간 구조(40계절), §17·§18이 캐릭터·애니메이션 |
| `Shared/Underwrite_시간구조_설계안.md` | 10년·계절·휴장 갭·계절 계획·멸망의 층·거품 시장·정보 모으기의 근거 (v0.7의 바탕) |
| `Shared/Underwrite_다음_작업목록.md` | **다음 할 일** |
| `Shared/Underwrite_작업기록_2026-10.md` | 10월 작업 기록(지난 결정과 경위) |
| `Shared/Underwrite_레퍼런스_조사.md` | 비슷한 게임과 교훈 (2026-10-17) |
| `Shared/Underwrite_기획서_v0.3.md` | 이전판. 4방향·리깅 기준이며 v0.4에서 대체됨 |

## Codex/ — 아트

| 문서 | 비고 |
|---|---|
| `Codex/Underwrite_아트_작업규칙.md` | **아트의 최종 권한.** 기획서 §18보다 이 문서가 우선 |
| `Codex/Underwrite_8방향_애니메이션_자동생성툴.md` | 8 Direction Builder 사용법 |
| `Codex/Underwrite_영지_배경에셋_후보_2026-10-16_v2.md` | 배경 에셋 2차 조사 (현행) |
| `Codex/Underwrite_영지_배경에셋_후보_2026-10-16.md` | 1차 조사. 추천 철회됨 |

## Claude/ — 코드

| 문서 | 비고 |
|---|---|
| `Claude/Underwrite_코드_작업규칙.md` | C# 규칙 v1.0. `.editorconfig`가 서식 항목을 강제한다 |
| `Claude/Underwrite_사장님_인터뷰_2026-10-18.md` | **사장님이 안 계실 때 과장이 판단하는 기준** — 목표·재량·금지선·취향 |
| `Claude/Underwrite_과장_결정기록.md` | 과장이 혼자 정한 것(정한 것 · 왜 · 되돌리려면) |
| `Claude/Underwrite_에셋_출처.md` | 외부 에셋(소리 등)의 출처와 라이선스. 크레딧 화면용 |
| `Claude/Underwrite_엔딩_설계안.md` | 엔딩 구조 초안 — 세계의 결말 4 × 관계의 마무리 4(여성 셋 + 혼자) |
| `Claude/Underwrite_상장흐름_설계안.md` | 거래 단위·용사 지수 종목·이탈. 상장 경로(§4)는 v0.7에서 자동 상장으로 대체 |
| `Claude/Underwrite_후원흐름_설계안.md` | 익명 의뢰주 |
| `Claude/Underwrite_루틴_설계안.md` | 하루·관계의 리듬. 주기 길이는 v0.7 계절 구조로 대체 |
| `Claude/Underwrite_시장모델_실제자료_샘플.md` | 가격 모델 참고 자료 |

## 루트의 에이전트 지침

| 파일 | 읽는 주체 | 비고 |
|---|---|---|
| `AGENTS.md` | **Codex** | Codex CLI가 작업 폴더의 이 파일을 자동으로 읽는다. 위치를 옮기면 자동 로딩이 끊긴다 |
| `CLAUDE.md` | **Claude Code** | Claude Code가 자동으로 읽는다 |
| `Tools/codex-art-brief.md` | Codex (Claude가 주입) | `Tools/codex-art.sh`가 아트 요청 앞에 붙인다 |

## 소유가 겹치는 지점

- **8 Direction Builder와 Batch Trim**: 문서는 `Codex/`에 있지만 **도구 코드는 Claude Code가 관리**한다(`Assets/Editor/`). 코드를 고치면 `Codex/Underwrite_8방향_애니메이션_자동생성툴.md`도 함께 갱신해야 한다.
- **기획서 §17·§18**: 아트 내용이지만 기획서는 `Shared/`에 있다. 아트 기준이 바뀌면 `Codex/Underwrite_아트_작업규칙.md`를 먼저 고치고, 기획서는 사용자 승인 후 갱신한다.

## 문서가 서로 어긋날 때

1. `Codex/Underwrite_아트_작업규칙.md` (아트)
2. `Claude/Underwrite_코드_작업규칙.md` (코드)
3. `Shared/Underwrite_기획서_v0.6.md` (기획)

더 최신 날짜의 전문 문서를 따르고, 어긋난 지점을 보고에 남긴다. 기획서는 전문 문서보다 갱신이 늦을 수 있다.
