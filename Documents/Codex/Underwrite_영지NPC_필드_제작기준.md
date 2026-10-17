# Underwrite 영지 NPC 필드 제작 기준

작성: 2026-10-17 / 관리: Codex

## 적용 범위

영지 NPC 1차 요청의 제작·검수 기록을 다음 작업에 재사용하기 위한 문서다. 상위 문서는 Underwrite_아트_작업규칙.md다. 이번 요청은 정면 S 한 방향, PNG까지만, Assets 쓰기 금지라는 명시적 예외가 있으므로 기본 8방향·Unity 자동 연결 범위를 이번 납품에 적용하지 않았다. 일반 기준을 변경한 것은 아니다.

## 납품 구조와 공통 규격

- Outputs/NpcField_v1/{innkeeper,archivist,smith,steward,broker,apprentice,guard,vendor_fruit,vendor_cloth,vendor_goods}/{Idle,Work,Give}/S_00.png부터 S_05.png까지.
- 필드 362×724 RGBA, 캔버스 기준점 Unity 아래쪽 (181,13), PNG 위쪽 (181,711). PNG 단계에서 Unity 피봇·PPU는 실제 설정하지 않았다.
- 이번 신규 Work는 허용된 6~8프레임 중 6프레임으로 제작했다. Idle·Work 반복, Give 일회. guard의 Give는 물건 전달 대신 고개 끄덕임이다.
- timing.json의 프레임별 ms를 우선하며 APNG에는 같은 지연·반복 횟수를 넣는다. 검수용 파일은 캐릭터 폴더 바로 아래에 두어 동작별 S_*.png 검색과 섞이지 않게 한다.
- 지도는 Outputs/VillagePartsFull_v1/layout.json의 인물 실루엣 높이 135px 기준이다. 주인공 현재 원본 bbox 높이 699px에 대해 135/699라는 공통 배율을 사용한다. 모든 NPC를 각각 135px로 맞추지 않는다.

## 분할·정렬에서 확인한 함정

- 6열×3행 생성 시트가 항상 정사각형으로 나오거나 정확한 격자 간격을 갖는 것은 아니다. 알파 연결 성분으로 인물의 실제 bbox와 순서를 확인하고, 다른 프레임의 손·옷이 붙으면 간단한 등분으로 해결하지 않는다.
- 몸통을 기준으로 한 공통 배율을 인물 전체 프레임에 적용한다. 팔을 뻗은 bbox를 기준으로 매 프레임 다시 크기를 맞추지 않는다. 폭이 넘으면 팔꿈치를 접은 동작을 이미지 생성 도구로 다시 그린다.
- 모든 프레임에 동일하게 적용하는 캔버스 배치와 프레임마다 달라지는 재정렬을 구분한다. 소품·내민 손의 bbox 중심으로 골반을 옮기지 않는다.
- 움직이지 않는 하체는 해당 **동작 자체의 기준 프레임**을 사용한다. 다른 동작의 하체를 긴 로브·코트 중간에 수평으로 붙이면 윤곽에 단차가 생길 수 있다. 확대 프레임 띠로 확인한다.
- 모루·숫돌은 발 앞 소품이다. 소품 밑면을 신발 접지점으로 오인하지 않는다. 연결 성분1 통과만으로 신발 위치·도구 모양·망치질의 무게를 검증한 것으로 취급하지 않는다.
- 브라우저 미연결 상태에서는 APNG의 순서·지연·RGBA 매칭 검증과 실제 화면 재생 육안 검증을 구분해서 보고한다.

## 머리·얼굴 재사용

기존 갑옷 초상은 Assets/UI/Images/Faces/face_folk_*이며 삭제하거나 교체하지 않는다. 새 의상은 이미지 생성 도구로 그리고, 원본 머리·얼굴을 인물별 보존 마스크로 그대로 합성한다. 마스크는 reference/*_head_preservation_mask.png, 좌표는 portraits_verification.json 및 build_portraits.mjs에 보관한다. 같은 사람의 4표정은 한 벌의 생성 몸·옷을 공유한다.

머리 전체를 큰 사각형으로 복사하면 턱 아래의 옛 망토나 갑옷까지 들어온다. 반대로 마스크를 좁히면 새로 생성된 머리 끝이 바깥에 남아 분리 조각이 생긴다. 얼굴·머리의 보존 범위와 목 아래 옷을 구별하고 원본 대비 보존 픽셀 차이, 몸의 4표정 일치, 전체 알파 일치, 분리 성분을 동시에 검사한다.

## 재현 자료

Outputs/NpcField_v1의 pipeline.mjs·split_*.mjs·build_portraits.mjs·build_reviews.mjs·finalize.mjs와 before.json, generation_prompts.json, generation_manifest.json, verification.json, staging.json을 함께 본다. 재생 원본은 각 프레임 PNG이며 APNG는 검수용이다. 재실행 시 원본 변경 여부와 기록된 선택 원본을 먼저 확인한다. 작업 중간의 verification_working.json보다 최종 verification.json을 우선한다.
