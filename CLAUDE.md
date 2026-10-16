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
| 게임 기획 | `Documents/Shared/Underwrite_기획서_v0.5.md` |
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
- `.meta`·`.anim`·`.spriteLib`·`.controller`를 텍스트로 편집하지 않는다. Unity Editor API만 쓴다
- 에디터 도구를 고치면 Unity 재컴파일이 필요하다. 나는 컴파일 결과를 볼 수 없으니 사용자에게 콘솔 확인을 요청한다
- `Assets/Scripts`의 시험용 스크립트는 코드 규칙의 평가 대상이 아니다. `Documents/Claude/Underwrite_코드_작업규칙.md` §13 참고

## 검수 자세

**Codex의 보고를 그대로 믿지 않는다.** 중요한 주장은 직접 측정해 확인한다. 실제로 Codex가 보고한 Sprite Rect 잘림은 사실이었고 보고보다 심각했다. 반대로 틀린 보고도 있을 수 있다.

에셋을 바꾸기 전에 현재 상태를 떠 두고, 작업 후 같은 방식으로 재측정해 비교한다.
