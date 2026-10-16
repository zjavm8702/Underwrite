# AGENTS.md — Codex 작업 지침

**이 파일은 Codex 전용이다.** Codex CLI가 작업 폴더의 `AGENTS.md`를 자동으로 읽으므로 프로젝트 루트에 둔다. 위치를 옮기면 자동 로딩이 끊긴다.

Claude Code의 지침은 루트 `CLAUDE.md`에 있다. 문서 폴더 구조와 소유 관계는 `Documents/README.md`를 본다.

역할 구조: **사장 = 사용자 / 과장 = Claude Code / 대리 = Codex.** `Documents/Codex/`의 아트 문서는 Codex가 관리한다.

## 캐릭터 아트·애니메이션 작업

작업 전에 다음 문서를 읽고 최신 결정부터 적용한다.
- `Documents/Codex/Underwrite_아트_작업규칙.md`
- `Documents/Codex/Underwrite_8방향_애니메이션_자동생성툴.md`

### 사용자가 정한 완료 범위 (2026-10-16)

캐릭터 애니메이션 제작 요청은 **스프라이트 제작 → 알파·비율·방향 검사 → 프로젝트의 방향별 폴더에 PNG 배치 → Sprite Library 카테고리·라벨 등록 → SpriteResolver 애니메이션 클립 생성 → Animator 상태와 8방향 Blend Tree 연결 → 실제 프레임 재생 검증**까지 수행한다. PNG나 미리보기만 전달하고 사용자가 등록·녹화·Blend Tree 연결을 마무리하도록 넘기지 않는다. 해당 범위는 사용자가 이후 작업에도 계속 수행하도록 지정했으므로 매번 적용 여부를 재확인하지 않는다.

- 자동 생성은 `Underwrite.Editor.AnimationTools.DirectionalAnimationBuilder.Analyze/Build`를 Unity Editor에서 사용한다. 창 메뉴는 `Tools/Underwrite/Animation/8 Direction Builder`다.
- 현재 주인공 Library: `Assets/SpriteLibrarys/Protagonist.spriteLib`.
- 현재 주인공 Controller: `Assets/Animations/Controller/Controller.controller`.
- 현재 수동·환호 클립 루트: `Assets/Animations/Clib`. 이미 등록된 동작은 실제 저장 경로를 확인하여 갱신한다. 무조건 기본 `Generated` 폴더에 중복 생성하지 않는다.
- Float 파라미터 `DirectionX`, `DirectionY`, 2D Simple Directional Blend Tree와 기존 방향 좌표를 유지한다. Resolver 경로는 실제 Animator 계층을 확인하여 계산한다.
- PNG 수정은 기존 방향별 파일명·크기·프레임 수를 유지하고 PNG만 교체한다. 기존 텍스처 `.meta`·GUID·Trim·피봇·PPU를 무단 변경하지 않는다. 프레임 수 변경 등 구조 변경이 필요하면 기존 연결 보존 방법을 먼저 마련한다.
- 애니메이션 연결에 필요한 `.spriteLib`, `.anim`, `.controller` 변경은 이번 완료 범위에 포함된다. Unity Editor API와 위 툴로 수행하며 직렬화 파일을 직접 텍스트 편집하지 않는다. 다른 캐릭터나 다른 동작의 카테고리·상태·전환 조건은 유지한다.
- 다른 캐릭터는 해당 캐릭터의 Library·Controller·저장 경로를 확인하거나 별도로 생성한다. 주인공 에셋을 다른 캐릭터용으로 덮어쓰지 않는다.
- 반복/일회 재생, 프레임별 유지 시간과 최종 프레임 길이를 설정한다. 각 방향의 각 프레임을 SampleAnimation 등으로 검사하여 실제 Sprite가 예상과 일치하는지 확인한다.
- 게임플레이의 새로운 입력·발동 조건·상태 진입/복귀 조건은 요청 또는 기존 명확한 규칙이 있을 때 연결한다. 임의의 키나 전환 규칙을 새로 정하지 않는다. 해당 부분이 미연결이면 최종 보고에 명시한다.
- 기존 파일 백업과 관련 없는 에셋 보존을 확인한다. 임포트된 SpriteLibraryAsset의 dirty 플래그만으로 미저장 편집이라고 판단하지 않는다. 실제 Sprite Library 편집 창의 미저장 변경은 별도로 보존한다.
- 생성 원본·프롬프트·시간·미리보기·검증 결과와 적용 경로를 프로젝트 안에 기록한다. 이미 적용된 작업은 중복 생성하지 않고 상태부터 확인한다.

## 현재 확인된 적용 상태

2026-10-16 Unity 편집기에서 주인공 Cheer의 Library 8개 카테고리, `Assets/Animations/Clib/Cheer/Cheer_{방향}.anim` 8개와 Controller의 `Cheer` 상태/`Cheer_8Directions` 트리(자식8개)가 연결된 것을 확인했다. 과거 결과물 README의 ‘Animator 연결 전’ 문구는 당시 기록이며 현재 상태를 뜻하지 않는다.
