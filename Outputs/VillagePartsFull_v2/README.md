# VillagePartsFull v2 — 성벽 배치·수치 검수 시안

**사용자 시각 승인 전 시안. 하단 주택 4채 충돌이 남아 전체 완료로 판정하지 않는다.** PNG·layout.json 범위만 작업했고 Assets·Unity는 쓰지 않았다.

|진행표|상태|
|---|---|
|① 성벽 조각 그림|완료 — 신규 RGBA PNG 10장, 원본 생성·수정 12회 기록|
|② 배치·v1 사물 정리|29개 성벽 배치, 소품 34개 제거·2개 이동. 하단 주택 4채 판단 대기|
|③ walkable·검수|마스크·다섯 크롭·이음매·해시 검사 완료. 실제 게임 화면 승인 미수행|

## 배치와 좌표

3840×2160 지도, part_scale=2. foot·다각형·anchors는 좌상단 원점 지도1배 좌표이고 size·pivot_in_png는 PNG2배 좌표다. 렌더링 위치는 foot-pivot_in_png/2, foot.y 오름차순 정렬이다. PNG 피봇을 Unity 아래쪽 정규화 피봇으로 바로 쓰면 안 된다.

layout.json: 기존 151개에서 소품 34개 제거 + 벽29개 = 146개. 기존 건물·장소·노점의 레코드는 전부 보존했다. 성벽에는 kind=wall. 북8, 서4, 동4, 남8, 모서리 탑4, 성문1. 북·남·서·동 각각 2변형, 탑1종·성문1종이다.

|신규 PNG|해상도(2배, 투명 패딩 포함)|pivot_in_png(상단 원점)|
|---|---|---|
|wall_north_a.png|976×676|(488, 668)|
|wall_north_b.png|976×676|(488, 668)|
|wall_west_a.png|186×976|(93, 968)|
|wall_west_b.png|186×976|(93, 968)|
|wall_east_a.png|186×976|(93, 968)|
|wall_east_b.png|186×976|(93, 968)|
|wall_south_a.png|976×216|(488, 208)|
|wall_south_b.png|976×216|(488, 208)|
|wall_corner_tower.png|436×896|(218, 888)|
|wall_gate.png|1016×676|(508, 668)|

실루엣 기준 지도1배: 북쪽330px, 옆 띠85px, 남쪽100px, 탑440px. 성문 중앙 아치 안쪽 바닥~꼭짓점은 최종 PNG 알파 실측238.5px(사람135px의1.767배)이다. 북벽은4px·옆벽은5px 겹쳐 연결하며 남벽은 중앙 성문과 탑 아래로 겹친다. 모든 PNG 파일의 8px 패딩은 본체 높이와 별개다. 생성 원화 bbox를 목표 규격으로 보간한 것이므로 네이티브 목표 해상도의 원화 세부를 보장한다는 뜻은 아니다. 원본 크기·crop·출력 크기는 processing.json에 남겼다.

성문은 열린 나무 문짝 그림이지만 기능상 막았다. **anchors.gate=[[1920,2135]]**를 성문 part와 layout 최상위에 함께 기록했다. 향후 개방 시 성문 바닥 차단 다각형을 함께 갱신해야 한다.

## 보존

v1 최상위 파일70개 SHA-256 모두 유지, 원본 사물 PNG33개와 ground.png 바이트 동일 복사. 원본 PNG는 모두 같은 이름으로 v2에 복사했다. 확인 가능한 Assets 메타585개는 검수 단계의 전후 해시 유지·변경0개이며, 작업 시작 시점 전체 메타 스냅샷이라고 주장하지 않는다. 이번 작업의 Assets 쓰기는0회다. before.json·verification.json·manifest.json 참조.

## 수치 검수

- 보행 가능3,494,278px / 8,294,400px = **42.128159%**. 저장 layout 다각형 합집합과 마스크 불일치0, 비이진 픽셀0.
- 장소6곳(inn775,704 / manor1949,637 / guild3149,823 / smith583,1548 / board3013,1532 / well1920,1224)과 노점3곳 문 앞 **9/9**가 광장과 연결된다. 4방향 flood fill 및 v1과 같은54×12px 발밑 직사각형 확보망 모두9/9 연결. 이 수치를162px 길 폭 확보 검사로 해석하지 않는다.
- 성문 앵커는 검정0. 북벽 뒤·옆벽 바깥·남쪽 경계 차단. 성벽 추가 후에도 주요 문 좌표 변경0개.
- 새 PNG10장 모두 본체 연결 성분1개·분리 성분0개, 알파1~31픽셀0, 숨은 RGB0, 캔버스 접촉0. 기존33장은 바이트 동일성과 v1 검증 기록을 함께 보존한다.
- 이음매19곳의 벽 몸체 띠2,628픽셀 중 투명 틈0. 검사 영역은 wall_seams.json에 명시했다. 여장 틈까지 꽉 찬 벽이어야 한다는 뜻은 아니다. 두 변형을 섞었고 동일 조각 연속은 최대2개다. 조각 재사용은 의도한 제작 방식이며 반복감의 주관적 시각 판정을 자동 통과로 보지 않는다.
- 정적 환경이라 애니메이션 재생·8방향 프레임 검사는 해당 없음. PNG 개별 캔버스가 잘리지 않는 것과 지도 경계에서 의도적으로 크롭되는 것은 구분한다.

## 1920×1080 검수 화면

screen_north.png, screen_south.png, screen_east.png, screen_west.png, screen_gate.png. 각 화면에 v1 검수용 실제 필드 인물(52×135)을 하나씩 합성했다. screen_positions.json에 인물 발밑·크롭을 기록했다. 화면은 실제 Unity 캡처가 아니라 layout과 같은 배치/정렬 규칙의 원배율 합성이다. screens_review.png는 축소 모음이므로 원배율 판정은 개별 파일로 한다.

남쪽 벽 바로 위 인물은 foot=(1210,2050), 벽 상단 y2056과6px 간격. 불투명 인물4,142px 중 성벽 가림0. 나머지 네 화면도 성벽에 의한 인물 가림0이다. 북쪽 인물 최종 foot=(1580,490)은 통행 가능·54×12 확보 지점이다.

전체 미리보기 VillagePartsFull_v2_preview.png, 이음매 표시 wall_seams.png, 통행 오버레이 walkable_overlay.png, 검은·밝은 배경 테두리 확인 wall_assets_black.png / wall_assets_light.png를 보관한다. v1에서 복사한 screen_inn 등 구버전 검수 PNG는 v2 성벽 검수 화면으로 사용하지 않는다.

## 옮기거나 뺀 소품 목록

- `tree_0` (`tree_a.png`): 제거, 기존 60, 650 — wall corridor / corner tower overlap
- `tree_1` (`tree_b.png`): 제거, 기존 525, 220 — wall corridor / corner tower overlap
- `tree_2` (`tree_a.png`): 제거, 기존 1430, 215 — wall corridor / corner tower overlap
- `tree_3` (`tree_b.png`): 제거, 기존 2370, 190 — wall corridor / corner tower overlap
- `tree_4` (`tree_a.png`): 제거, 기존 2730, 220 — wall corridor / corner tower overlap
- `tree_5` (`tree_b.png`): 제거, 기존 3300, 240 — wall corridor / corner tower overlap
- `tree_6` (`tree_a.png`): 제거, 기존 3790, 640 — wall corridor / corner tower overlap
- `tree_15` (`tree_b.png`): 제거, 기존 30, 2140 — wall corridor / corner tower overlap
- `tree_16` (`tree_a.png`): 제거, 기존 3830, 2140 — wall corridor / corner tower overlap
- `lamp_15` (`lamp.png`): 제거, 기존 3670, 2100 — wall corridor / corner tower overlap
- `hedge_0` (`hedge.png`): 제거, 기존 100, 100 — wall corridor / corner tower overlap
- `hedge_1` (`hedge.png`): 제거, 기존 800, 80 — wall corridor / corner tower overlap
- `hedge_2` (`hedge.png`): 제거, 기존 1150, 90 — wall corridor / corner tower overlap
- `hedge_3` (`hedge.png`): 제거, 기존 1510, 90 — wall corridor / corner tower overlap
- `hedge_4` (`hedge.png`): 제거, 기존 2480, 80 — wall corridor / corner tower overlap
- `hedge_5` (`hedge.png`): 제거, 기존 2860, 100 — wall corridor / corner tower overlap
- `hedge_6` (`hedge.png`): 제거, 기존 3200, 90 — wall corridor / corner tower overlap
- `hedge_7` (`hedge.png`): 제거, 기존 3750, 100 — wall corridor / corner tower overlap
- `hedge_8` (`hedge.png`): 제거, 기존 2470, 225 — wall corridor / corner tower overlap
- `hedge_9` (`hedge.png`): 제거, 기존 70, 810 — wall corridor / corner tower overlap
- `hedge_10` (`hedge.png`): 제거, 기존 70, 1080 — wall corridor / corner tower overlap
- `hedge_17` (`hedge.png`): 제거, 기존 3760, 1150 — wall corridor / corner tower overlap
- `hedge_18` (`hedge.png`): 제거, 기존 3760, 1450 — wall corridor / corner tower overlap
- `hedge_19` (`hedge.png`): 제거, 기존 1350, 2140 — wall corridor / corner tower overlap
- `hedge_20` (`hedge.png`): 제거, 기존 2250, 2140 — wall corridor / corner tower overlap
- `hedge_21` (`hedge.png`): 제거, 기존 2750, 2140 — wall corridor / corner tower overlap
- `hedge_22` (`hedge.png`): 제거, 기존 3050, 2140 — wall corridor / corner tower overlap
- `fence_0` (`fence.png`): 제거, 기존 710, 100 — wall corridor / corner tower overlap
- `fence_1` (`fence.png`): 제거, 기존 1060, 180 — wall corridor / corner tower overlap
- `fence_2` (`fence.png`): 제거, 기존 2610, 100 — wall corridor / corner tower overlap
- `fence_3` (`fence.png`): 제거, 기존 3050, 180 — wall corridor / corner tower overlap
- `fence_4` (`fence.png`): 제거, 기존 50, 1190 — wall corridor / corner tower overlap
- `fence_8` (`fence.png`): 제거, 기존 2900, 2150 — wall corridor / corner tower overlap
- `fence_9` (`fence.png`): 제거, 기존 640, 2140 — wall corridor / corner tower overlap
- `tree_10` (`tree_a.png`): 85, 1350 → 300, 1370 — side wall clearance; reuse v1 greenery
- `tree_14` (`tree_a.png`): 3770, 1820 → 3470, 1770 — side wall clearance; reuse v1 greenery

## 미해결·주의

1. **stone_1(555,2140), elf_2(2150,2140), fairy_3(3230,2145), fairy_4(3495,2145)**는 v1부터 지도 끝까지 내려와 있다. 건물 좌표를 고정한 채 남쪽 성벽100px와 중앙 성문을 배치하면 전면 하단이 가려지고 일부 앞쪽이 차단된다. 주요 장소6·노점3의 연결성 통과가 이 네 주택 문제의 해결을 뜻하지 않는다. lower_house_conflicts.json은 정확한 문 앵커가 없는 주택의 foot를 대리 검사점으로 쓴 결과이며 실제 문 좌표 측정으로 오인하지 않는다. 4채 이동 예외를 질문했으며 승인 없이 옮기지 않았다.
2. ground.png에는 v1에서 구운 고정 그림자가 들어 있다. 바이트 동일 복사 조건 때문에 제거·이동한 소품의 옛 그림자가 일부 남을 수 있다. 새 성벽의 접지 그림자를 사물 원본에 그리지 않았으며 바닥에도 추가하지 않았다.
3. 생성 원화의 타원 상판 비율·시점·반복감은 실제 게임에서 최종 시각 승인이 필요하다. 검수용 합성을 실제 게임 실행 확인으로 보고하지 않는다.
4. 버전이 가장 높은 폴더를 읽는 도구는 이 v2 시안을 선택할 수 있다. 하단 주택 충돌을 해결하거나 이 차이를 승인하기 전 자동 적용 완료로 취급하지 않는다.

## 재현과 기록

generation_prompts.json은 내장 image_gen 프롬프트 전문12건·시각·원본 경로, sources/는 생성 원본, processing.json은 정리·크기 변경, object_changes.json은 소품 변경 목록이다. 알파32 미만 제거, 작은 분리 성분 제거, 내부 불투명화와 숨은 RGB 정리를 수행했다.

prepare_walls.mjs는 원본에서 PNG를 재생성하는 보관 코드다. rebuild_previews.mjs의 refresh()는 현재 layout으로 전체·다섯 화면·이음매·오버레이를 재생성하며 실제 실행을 확인했다. 무거운 전체 렌더는 Node REPL에서60초를 넘을 수 있어180초 실행 한도를 사용했다. 스크립트는 v1의 읽기 전용 png_tools.mjs/helpers.mjs에 의존한다.

재사용 지식은 Documents/Codex/Underwrite_영지_그림배경_제작기준.md의 VillagePartsFull v2 절에 추가했다.


## Claude Code 조정 (2026-10-17)

남쪽 성벽에 앞부분과 문이 가리던 아래쪽 집 네 채를 옮겼다(건물 이동 허용). 원본 배치표는 `layout_codex_original.json`.
- 옮긴 집(dx, dy px): elf_2 (0, -120), fairy_3 (40, -120)
- 자리를 내주려고 뺀 소품·나무: tree_12, bench_3, planter_20, planter_28, planter_15
- 자리가 없어 뺀 집: fairy_4, stone_1
- walkable.png는 다시 그리지 않았다(Unity 도구는 배치표의 footprint·block으로 충돌을 만든다).
- 성문 오른쪽 탑에 가리던 elf_2를 한 번 더 옮김: (0, -220). 뺀 소품: lamp_9, planter_11, hedge_14
