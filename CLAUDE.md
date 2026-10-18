# Claude Code 작업 지침

Unity 6.5 비도트 2D 탑다운 게임 《지하거래소의 후원자》. 프로젝트명 `Underwrite`.

## 역할 구조

**사장 = 사용자 / 과장 = Claude Code / 대리 = Codex.**

- 내 담당: 코드, Unity 연결, 도구 제작, 검수, Codex에 아트 위임
- Codex 담당: 아트·애니메이션 제작과 `Documents/Codex/` 문서 관리
- Codex는 이미지 생성 도구가 있고 나는 없다. `Outputs/` 아래 산출물은 모두 Codex 납품물이다

## 문서 위치

구조와 소유 관계는 `Documents/README.md`에 있다. 문서가 어긋날 때의 우선순위도 거기 있다.

| 찾는 것 | 위치 |
|---|---|
| C# 코드 규칙 | `Documents/Claude/Underwrite_코드_작업규칙.md` (v1.0 확정) |
| 아트 규칙 | `Documents/Codex/Underwrite_아트_작업규칙.md` (아트의 최종 권한) |
| 게임 기획 | `Documents/Shared/Underwrite_기획서_v0.7.md` |
| 혼자 일할 때의 기준 | `Documents/Claude/Underwrite_사장님_인터뷰_2026-10-18.md` — 재량·금지선(돈 쓰기·외부 공개)·취향. 정한 것은 `Underwrite_과장_결정기록.md`에 쌓는다 |
| Codex 작업 범위 | 루트 `AGENTS.md` — **Codex용 파일이다. 내가 읽을 수는 있지만 내 지침은 아니다** |

**코드를 쓰기 전에 `Documents/Claude/Underwrite_코드_작업규칙.md`를 읽는다.** 서식 항목은 `.editorconfig`가 강제하지만, LINQ 제한·`== true` 명시·`switch` 식 금지·별칭 금지·Constants.cs 관리 규칙은 문서에만 있다.

## Codex에 위임하는 방법

아트 작업:

```bash
Tools/codex-art.sh "<요청>"        # 읽기 전용 — 조사·제안
Tools/codex-art.sh -w "<요청>"     # 쓰기 허용 — 실제 제작
Tools/codex-art.sh -w -u "<요청>"  # Unity 연결까지
Tools/codex-art.sh -n "<요청>"     # 호출 없이 보낼 프롬프트만 확인
```

`Tools/codex-art-brief.md`(상시 지시서)가 자동으로 앞에 붙어 아트 규칙 제약과 고정 보고 양식을 주입한다. 요청마다 제약을 다시 설명하지 않는다.

아트 규칙이 필요 없는 코드 작업은 공식 플러그인을 쓴다 — `/codex:rescue` 또는 `codex:codex-rescue` 에이전트.

## 운영상 함정

- **`codex exec`에는 `--ephemeral`이 필수다.** `~/.codex/thread_history_1.sqlite`가 400MB를 넘어 세션을 영속화하면 호출이 끝나지 않는다. `Tools/codex-run.sh`가 기본 적용하고, 이어서 질문하는 `-r`/`-R`에서만 영속화한다
- **Codex에 넘기는 경로는 Windows 형식이어야 한다.** Git Bash의 `/tmp/...`는 `os error 123`이 난다. `Tools/codex-run.sh`가 `cygpath -w`로 변환한다
- **Codex 납품물은 작업 완료 알림을 받은 뒤에 쓴다.** 파일이 폴더에 보여도 Codex는 아직 고치는 중일 수 있다. 시범 v1에서 마무리 전에 Unity로 옮겼다가 6장이 중간본으로 들어갔다
- `.meta`·`.anim`·`.spriteLib`·`.controller`를 텍스트로 편집하지 않는다. Unity Editor API만 쓴다
- **실행 중인 Unity 에디터를 `unity command`로 직접 다룬다** (`com.unity.pipeline` 패키지). 에디터가 열려 있어야 한다
  - 상태: `unity command --no-banner --json editor_status`
  - 컴파일 확인: `eval --code 'UnityEditor.AssetDatabase.Refresh(); return true;'`로 새 파일을 들이고, `console_status`의 `groundTruth.compilationFailed`·`consoleErrors`를 본다. `console --level warn`이면 내용까지 나온다
  - 그 밖에 `editor_play`/`editor_stop`, `screenshot`, `menu`(메뉴 실행), `eval`(C# 한 토막)이 있다. 목록은 `unity command --no-banner`
  - **테스트:** `unity command --no-banner --json run_tests --mode editor --filter Underwrite.Editor.Tests --filter_type assembly` (`Assets/Editor/Tests`). 판정 로직을 바꾸면 돌린다
  - **UI Toolkit 화면 캡처:** `screenshot`은 오버레이 UI가 안 찍힌다. Play 중 `capture_game_view --source screen --save_path Temp/x.png` → 실제로는 `Assets/Temp/`에 떨어지니 옮기고 지운다
  - **Play 시험은 진짜 저장 파일을 덮어쓴다**(영지 자동 저장). 시험 전 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Underwrite/underwrite_save.json`을 백업하고 끝나면 되돌린다
    - **백업은 날짜·시각을 붙인 새 파일로 남기고 덮어쓰지 않는다** (`save_YYYYMMDD_HHMMSS.json`). 같은 이름 하나로 돌려 쓰다가 이미 덮인 세이브를 백업해 사장님 진행을 잃었다(10/18)
  - 문서: `Library/PackageCache/com.unity.pipeline@*/Documentation~/`
- `Assets/Scripts`의 시험용 스크립트는 코드 규칙의 평가 대상이 아니다. `Documents/Claude/Underwrite_코드_작업규칙.md` §13 참고

## 사원과 검수 (2026-10-18 사장님)

- **사원 = 서브 에이전트**(Agent 도구). 넓게 훑기·기계적 점검을 맡긴다 — 코드 규칙 위반, 대사 설정 일관성(마을 사람은 지하를 모른다 등), 안 쓰는 코드·문구 찾기. 결과는 내가 코드에서 확인한 뒤 고친다
- **Codex 코드 리뷰는 기능 덩어리 하나를 끝낼 때 한 번**(`codex:codex-rescue` 에이전트, 읽기 전용). 사용량을 아트와 나눠 쓰므로 수시로 쓰지 않는다
- **설계·성능 기준:** SOLID에서 크게 벗어나지 않게(큰 클래스는 Handler로, 화면은 조립만), 메모리·최적화(매 프레임 할당·씬 검색·문자열 조립 피하기). **단 기능 구현이 먼저** — 덩어리를 끝낸 뒤 한꺼번에 정리해도 된다. 지금 덩어리: 온보딩(프롤로그·첫 지하·용사단 장면·미니맵) → 끝나면 정리 + Codex 리뷰

## 검수 자세

**Codex의 보고를 그대로 믿지 않는다.** 중요한 주장은 직접 측정해 확인한다. 실제로 Codex가 보고한 Sprite Rect 잘림은 사실이었고 보고보다 심각했다. 반대로 틀린 보고도 있을 수 있다.

에셋을 바꾸기 전에 현재 상태를 떠 두고, 작업 후 같은 방식으로 재측정해 비교한다.
