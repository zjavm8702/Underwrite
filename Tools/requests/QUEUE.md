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
| 12 | `village_ground_v3.md` | `VillageGround_v3` | 진행 중 — 가운데 채우기 + 종족 건축 |

영지는 **타일셋을 쓰지 않는다.** 도트 타일셋은 비도트 캐릭터와 해상도가 섞인다. 대신 그려진 배경 한 장 위를 걸어 다닌다. 화풍이 맞는 것은 시험으로 확인했다.

## 왜 이 순서인가

2번이 먼저인 이유는 **지금 거래 화면 말고 보여줄 장면이 하나도 없기 때문**이다. 정보 타임 연출을 만들려면 배경이 먼저 있어야 한다.

3·4번은 거래 화면의 빈 구석을 채운다. 6번은 장 사이 "정보 모으기" 단계가 쓸 장소다. 그 작업과 함께 들어간다.

종목 초상은 뺐다. 용사 일러스트를 종목에 붙여서 당분간 중복이다.
