# 8방향 애니메이션 자동 생성 툴

Unity 메뉴: **Tools → Underwrite → Animation → 8 Direction Builder**.

## 사용 순서

1. **동작 이미지 폴더**에 `Cheer`, `Idle`, `Walking`처럼 바로 아래에 S/SE/E/NE/N/NW/W/SW 폴더가 있는 폴더를 넣는다.
2. **동작 / 상태 이름**을 확인한다. 예: `Cheer`. Library 카테고리는 `Cheer_South`, `Cheer_Southeast`처럼 기존 프로젝트 규칙으로 만들어진다.
3. 사용할 **Sprite Library**와 **Animator Controller**를 선택한다. Hierarchy에서 캐릭터를 선택하고 **선택 오브젝트에서 가져오기**를 눌러도 된다. Resolver가 여러 개라면 사용할 Resolver 오브젝트를 선택한다.
4. **클립 저장 루트 폴더**를 확인한다. 기본은 `Assets/Animations/Generated/Protagonist`이며, 그 안에 `Cheer/Cheer_S.anim`처럼 저장한다. 기존 수동 클립을 바꾸려는 경우에만 기존 클립 루트를 직접 지정한다.
5. 시간 프리셋 또는 FPS를 고르고 반복 여부를 정한다. 프레임별 시간 목록이 있으면 FPS보다 우선한다.
6. **1. 8방향 검사 / 생성 미리보기**에서 방향별 프레임 수·첫/마지막 라벨·저장 위치·갱신 범위를 확인한다.
7. **2. Library + Clip + Blend Tree 생성**을 누른다.

Sprite Library에 8개 카테고리와 프레임 라벨을 등록하고, SpriteResolver를 기록한 클립 8개를 만든 뒤 Controller의 해당 상태에 2D Simple Directional Blend Tree를 연결한다. Animation 창에서 따로 녹화할 필요가 없다.

새 상태의 **진입/복귀 전환 조건은 기존 작업과 마찬가지로 Animator에서 설정**한다. 툴은 기존 전환 조건을 유지한다. 새 캐릭터는 SpriteLibrary 컴포넌트에 생성된 Library, Animator에 생성된 Controller를 지정해야 한다.

## 새 캐릭터

인간 여성처럼 다른 캐릭터 폴더를 선택하면 Library·Controller 선택을 비우고 캐릭터별 새 저장 폴더를 제안한다. 두 항목을 비워둔 채 생성하면 해당 폴더에 캐릭터 이름의 `.spriteLib`와 `.controller`를 만든다. 다른 캐릭터에 주인공 Library를 실수로 덮어쓰지 않도록 선택 항목을 다시 확인한다.

## 입력·호환 방식

- 폴더별 이미지는 Sprite로 임포트되어 있어야 한다. Multiple 이미지는 먼저 Slice한다. 단일 PNG 한 장당 Sprite 한 개인 경우와 한 이미지에 여러 Sprite가 있는 경우를 모두 읽는다.
- 방향 폴더 바로 안의 png/psd/psb/tga/jpg를 읽는다. 다른 하위 폴더는 읽지 않는다. 파일과 Sprite 이름을 숫자 순으로 정렬한다. 프레임 폴더에 미리보기·원본 시트를 같이 넣지 않는다.
- Sprite 이름을 그대로 라벨로 쓴다. 같은 방향 안에서 Sprite 이름이 중복되면 중단한다.
- 8방향이 모두 있어야 하고 방향별 프레임 수가 같아야 한다. 한 방향 누락이나 프레임 수 차이는 생성 전에 표시한다.
- 파라미터 기본값은 Float `DirectionX`, `DirectionY`. 대각선 좌표는 정규화한 ±0.7071068이다. 기존 파라미터 타입이 다르면 자동 변경하지 않는다.
- 기록 대상은 기존 클립과 동일한 `SpriteResolver.m_SpriteHash`의 Discrete Curve다. Renderer Sprite를 직접 교체하는 별도 구조로 바꾸지 않는다. 마지막 프레임 유지 시간까지 키를 기록한다.
- Animator와 Resolver가 같은 오브젝트면 상대 경로는 비워둔다. 자식이면 `Visual/Body`처럼 입력하거나 선택 오브젝트에서 가져오기로 계산한다. 기존 클립의 Resolver 경로가 다르면 새 출력 폴더를 사용한다.

## 재생 프리셋

| 프리셋 | 프레임 시간(ms) | 반복 기본 |
|---|---|---|
| 균등 FPS | 목록을 비워 FPS 사용 | 현재 선택 유지 |
| 작은 호흡 6장 | 200,200,200,200,200,200 | 켜짐 |
| 기존 눈깜빡임 6장 | 900,350,80,110,120,700 | 켜짐 |
| 걷기 8장 | 100,100,100,100,100,100,100,100 | 켜짐 |
| 환호 8장 | 240,120,120,120,300,180,180,300 | 꺼짐 |

## 갱신과 보존

동일한 카테고리·클립·상태가 있으면 기본적으로 생성을 막는다. 검사 결과를 확인하고 **같은 동작 갱신 허용**을 켜면 그 동작을 갱신한다. 해당 카테고리의 기존 라벨 목록은 새 프레임 목록으로 교체된다. 이외 카테고리·상태·전환 조건과 기존 GUID는 유지한다. 다른 상태가 공유하는 Blend Tree는 지우지 않는다.

PNG·텍스처 `.meta`·Trim·피봇·PPU는 변경하지 않는다. 씬·프리팹에도 자동으로 컴포넌트를 추가하지 않는다. Controller·Clip에 저장하지 않은 변경이 있으면 먼저 저장하도록 중단한다. `.spriteLib`의 임포트 결과에 붙은 dirty 플래그는 미저장 편집으로 취급하지 않고 현재 카테고리를 병합한다. 실제 Sprite Library 편집 창에 미저장 변경이 있을 때는 해당 창에서 Save한 뒤 다시 생성한다.

생성 전 기존 Library·Controller·클립의 원본 파일을 `Library/UnderwriteAnimationBuilder/날짜_시간_식별자/Assets/...`에 복사한다. 생성 도중 실패하면 새 에셋은 정리하고 기존 파일은 사본으로 복구한다. 이 사본은 Unity의 Library 폴더 정리 시 사라질 수 있으므로 장기 백업은 버전 관리로 한다.

## 검증

Unity 6000.5.10f1의 실제 편집기에서 현재 환호64장으로 임시 에셋을 생성하고 다음을 확인했다.

- 8개 카테고리·64개 라벨·8개 클립·2D Blend Tree 및 정규화한 방향 좌표.
- **64개 프레임을 실제 SampleAnimation으로 재생했을 때 예상 Sprite와 모두 일치**.
- 총1.56초·마지막 프레임 유지·반복 설정.
- 갱신 시 기존 GUID·수동 카테고리·별도 상태·전환 조건·공유 Blend Tree 유지, 반복 갱신 시 불필요한 트리 누적 없음.
- 잘못된 시간 목록·파라미터 타입·없는 폴더·중복 생성 사전 차단.
- `Visual/Body` 자식 Resolver 경로의 실제 재생과 Loop 옵션.
- 테스트 에셋 정리, 기존 캐릭터 이미지 메타·주인공 Library·Controller·수동 클립 보존.

검증 기록: `Outputs/DirectionalAnimationBuilder/verification.txt`.

## 최신 작업 기준과 적용 상태 (2026-10-16)

사용자 지정에 따라 앞으로 에이전트가 스프라이트 제작 후 이 툴을 사용한 **Library 등록·클립 생성·Blend Tree 연결·재생 검증까지 수행**한다. 사용자에게 마지막 생성 버튼을 누르도록 넘기는 것을 기본 완료 방식으로 삼지 않는다. 기존 PNG 수정만으로 현재 연결을 유지할 수 있는 경우에는 불필요하게 재생성하지 않는다. 전체 지침은 프로젝트 루트 `AGENTS.md`와 아트 작업 규칙의 ‘최신 완료 범위’를 따른다.

현재 주인공 Cheer는 실제 편집기에서 다음 연결을 확인했다.
- `Assets/SpriteLibrarys/Protagonist.spriteLib`: Cheer 방향별 카테고리8개.
- `Assets/Animations/Clib/Cheer/Cheer_{방향}.anim`: 방향별 클립8개.
- `Assets/Animations/Controller/Controller.controller`: `Cheer` 상태의 `Cheer_8Directions` Blend Tree, 자식8개.

기존 Idle·Walking 상태도 각자8방향 연결이 유지되어 있다. 새로운 진입·복귀 조건이나 플레이 입력은 별도 요청 또는 기존 규칙에 맞춰 연결한다.
