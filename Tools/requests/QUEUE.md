# Codex 아트 작업 대기열

사용자가 "정지"라고 할 때까지 계속 돌린다. 한 건이 끝나면 다음 건을 바로 건다.

진행 방식: `Tools/codex-art.sh -w -f Tools/requests/<파일>` 으로 보내고, 납품은 `Outputs/<이름>/`에 받는다. `Assets/`에 쓰는 것은 내가 한다.

| 순서 | 요청서 | 납품 폴더 | 상태 |
|---|---|---|---|
| 1 | `ui_controls_v1.md` | `MarketUI_Controls_v1` | **완료** — 적용함 |
| 2 | `underworld_scene_v1.md` | `UnderworldScene_v1` | **완료** — 검수함, 적용은 연출 작업 때 |
| 3 | `ui_icons_v1.md` | `MarketUI_Icons_v1` | **완료** — 적용 중 |
| 4 | `info_source_marks_v1.md` | `InfoSourceMarks_v1` | **완료** — 적용 대기 |
| 5 | `hero_cutout_v1.md` | `HeroCutout_v1` | **반려** — 갇힌 흰 영역이 남음 |
| 6 | `gather_places_v1.md` | `GatherPlaces_v1` | **완료** — 들임 |
| 7 | `hero_cutout_v2.md` | `HeroCutout_v2` | **완료** — 검수 통과, 적용함 |
| 8 | `portrait_faces_v1.md` | `PortraitFaces_v1` | **완료** — 검수 통과, Unity 연결까지 끝 |
| 9 | `village_ground_v1.md` | `VillageGround_v1` | **완료** — 들임. 충돌 마스크까지 적용 |
| 10 | `portrait_faces_v2.md` | `PortraitFaces_v2` | 진행 중 — 나머지 세 인물 표정 21장 |
| 11 | `village_ground_v2.md` | `VillageGround_v2` | **부분 통과** — 시점 0.66→0.44, 연결성·면적 통과. 밀도와 종족 미달 |
| 12 | `village_ground_v3.md` | `VillageGround_v3` | **취소** — 파츠 방식으로 바꿈 |
| 13 | `village_parts_pilot_v1~v3.md` | `VillagePartsPilot_v1~v3` | **완료** — v3 바닥(판석 45px) 통과, 영지 전체의 기준 |
| 14 | `ui_frames_v1.md` | `UIFrames_v1` | **완료** — 적용함 |
| 15 | `portrait_fix_v1·v2.md` | `PortraitFix_v1·v2` | **완료** — 엘프 코는 추후 재검토 |
| 16 | `ui_market_kit_v1.md` | `MarketUI_Kit_v1` | **부분 통과 — 적용함(10/18).** 교차 검수: 과장·Codex 모두 「매수/매도·주문 단추 혼재」 지적 → 배치·USS로 고침(거래판 하나, 단추 한 체계). 실제 화면 `ingame_v2.png` |
| 17 | `underworld_room_v1.md` | `UnderworldRoom_v1` | 진행 중(10/18) — 지하 방 바닥판·사물. 사람·정보상 초상은 v2 |

**다음 목록은 `ART_BACKLOG.md`** (2026-10-17 정리, 우선순위 순). Codex 사용량이 적어 한 건씩 확인받고 건다.

영지는 **타일셋을 쓰지 않는다.** 도트 타일셋은 비도트 캐릭터와 해상도가 섞인다. 대신 그려진 배경 한 장 위를 걸어 다닌다. 화풍이 맞는 것은 시험으로 확인했다.

## 왜 이 순서인가

2번이 먼저인 이유는 **지금 거래 화면 말고 보여줄 장면이 하나도 없기 때문**이다. 정보 타임 연출을 만들려면 배경이 먼저 있어야 한다.

3·4번은 거래 화면의 빈 구석을 채운다. 6번은 장 사이 "정보 모으기" 단계가 쓸 장소다. 그 작업과 함께 들어간다.

종목 초상은 뺐다. 용사 일러스트를 종목에 붙여서 당분간 중복이다.
