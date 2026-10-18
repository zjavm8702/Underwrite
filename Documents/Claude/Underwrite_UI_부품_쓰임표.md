# UI 부품 쓰임표 (2026-10-18, 과장)

작업목록 5-1의 1번 — `Assets/UI/Images`의 그림이 어느 화면 어디에 쓰이는지. 다음 UI 키트 요청(5-1의 4번)의 근거다.
조사 방법: 이름으로 USS·UXML·C#을 찾고, 안 잡힌 것은 `.meta`의 GUID로 씬·프리팹·에셋을 찾았다. USS는 `MarketScreen.uss`(공용)와 `MarketScreen.Parchment.uss`(양피지 테마) 둘이다.

## 1. 화면별

| 화면 | 쓰는 그림 | USS 클래스 |
|---|---|---|
| **거래 화면** (키트 v1 적용) | `MarketKit/kit_market_header` · `frame_main` · `frame_sub` · `ledger_strip` · `row_selected` · `tab_on/off` · `btn_normal/pressed/disabled/buy/sell` | `.market-root` 아래 `.header` `.board` `.panel` `.asset-row` `.stock-row--selected` `.tab` `.type-tab` `.btn` `.cta` |
| 거래 화면 (키트 이전 것이 남음) | `market_background` · `market_panel` · `market_selection` · `ui_button_*` · `ui_tab_*` · `ui_field` | `.market-root` 바탕, `.header`·`.holdings-popup`, `.stock-row--selected`, `.qty-box` 등 — **키트 규칙이 `.market-root` 아래에서 덮어써서 대부분 안 보인다** |
| 거래 화면 아이콘 | `icon_rise` · `fall` · `flat` · `collapse` · `expand` · `star_on/off` · `warning` | `.icon--*` `.star` |
| **대화창** (영지·지하 공통) | `dialogue_box` · `dialogue_nameplate` · `dialogue_next` · `choice_normal/hover/pressed/locked` | `.dialogue-box` `.dialogue-speaker` `.dialogue-next` `.dialogue-choice` |
| 대화 배경 | `place_tavern`(여관 **+ 대장간이 빌려 씀**) · `place_guild`(길드 **+ 영주관·장부 맞추기**) · `place_board` · `underworld_room` · `underworld_room_dim` | `.dialogue-root--*` `.ledger` `.scene` |
| **팝업·미니게임** (장부·엿듣기·주문 나르기·주문서 읽기·시찰·파산) | `popup_panel` · `popup_button_normal/pressed` | `.popup` `.popup-button` `.popup-button--main` — **미니게임 넷이 모두 같은 양피지 한 장** |
| 장면 문구(몽타주·편지) | `scene_box` | `.scene-box` |
| 영지 HUD | `village_button_normal/pressed` · `village_prompt` | `.village-button` `.village-prompt` |
| 캐릭터 발밑 그림자 | `character_shadow` | Character 프리팹·Village·Underworld 씬(GUID) |
| 초상 | `Faces/face_*` 77장 · `Heroes/*` 6장 | `Assets/Data/Portraits/*.asset`(GUID) |

## 2. 쓰이지 않는 것

| 그림 | 상태 |
|---|---|
| `MarketKit/kit_market_divider` | 키트 v1 납품물, 아직 쓸 자리를 안 만듦 — 거래판 안 구분선에 쓸 수 있다 |
| `icon_cancel` · `icon_locked` | 어디서도 안 씀(지정가 취소 단추·잠긴 선택지가 글자로 대신함) |
| `village_block_mask` | 안 씀 — 영지 충돌은 이제 파츠 콜라이더 |
| `village_ground` | 시험 씬 `VillageTest.unity`에서만 |

지우지는 않는다(사장님 승인 없이 에셋 삭제 안 함). 키트 요청 때 정리 대상으로 넘긴다.

## 3. 키트 요청에 넘길 진단

1. **[일부 해결 10/18]** 팝업·미니게임 단추를 키트 나무 판으로 바꾸고 밀랍은 주 행동(`--main`) 하나에만 남김 — 그림 없이 USS만. 영지 HUD 단추(밝은 나무)는 같은 만듦새의 따뜻한 톤이라 둠. `ui_button_*`은 덮여서 안 보임
   **단추 계열이 넷이다** — `kit_market_btn_*`(거래), `ui_button_*`(키트 이전 거래), `popup_button_*`(팝업·미니게임), `village_button_*`(영지 HUD). 테두리 두께·장식량이 서로 다르다 — 슬롭 점검의 「테두리 두께·장식량 기준 없음」이 여기서 나온다. **한 계열(4상태)로 합치는 것**이 키트의 첫째 목표
2. **미니게임 넷이 같은 양피지 한 장**(`popup_panel`) — 장부(책장), 주문서(꽂이의 쪽지), 주문 나르기(쟁반·주문표), 엿듣기(테이블)가 모두 같은 종이라 「놀이마다 손에 쥔 물건」이 안 느껴진다. 미니게임마다 종이 하나씩: 장부 펼침면, 주문서 쪽지, 주문표 — 9-slice 가능하게
3. **대화 배경 빌려 쓰기 둘** — 대장간(여관 그림), 영주관(길드 그림). 작업목록 5의 「대장간·영주관 대화 배경」. 주문서 읽기가 대장간 배경 위에서 열리므로 대장간 배경 우선순위가 올라갔다
4. 키트 이전 거래 화면 그림(`market_panel` 등)은 덮여서 안 보이지만 클래스가 남아 있다 — 키트가 한 벌로 확정되면 Parchment USS에서 정리

## 4. 순서 제안 (Codex 사용량이 차면)

지하 방 v3 통과 → 대기열 18·19 → **단추 한 계열 + 미니게임 종이 셋 + 대장간 배경**(§2-1대로 무료 에셋 먼저 — UI 틀·종이 질감은 찾을 만하다)
