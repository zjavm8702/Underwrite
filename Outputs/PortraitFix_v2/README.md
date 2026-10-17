# PortraitFix_v2 — 엘프 코 수정 시안 배치 완료

2026-10-17. 사용자 시각 승인 전 시안. 지정 Assets/UI/Images/Faces/face_elf_*.png 7장을 교체했다. 기본 제공 image_gen으로 새 코 원화를 생성한 뒤 평상의 공통 코 패치만 7장에 동일하게 합성했다. 코 아래 넓은 곡선과 콧구멍처럼 보이는 획을 제거하고 가는 콧날·작은 코끝을 적용했다. 입을 옮기지 않고 생성 코 패치의 수직 배치를 압축해 코끝 아래 인중 여백을 늘렸다. 원화 생성은 코드 드로잉이 아니라 image_gen을 사용했다.

## 배치와 보존
- 820×1080 RGBA, 기존 7개 파일명 유지. Single Sprite 모드1, 유효 Sprite Rect 전체 캔버스(0,0,820,1080), 중앙 피봇(410,540), PPU100 유지. spriteSheet에 남은 rect는 현재 Single 모드의 활성 Rect가 아니다.
- 표정 순서: angry, anxious, happy, neutral, relieved, sad, shocked. 정적 초상화 7종이며 8방향 애니메이션이 아니다. 반복/일회·총 재생 시간은 해당 없음. Unity 연결·재생은 요청 범위 밖이다.
- before/: 교체 직전 v1 원본7장. final/: 배치된 v2 원본7장. before.json 및 preservation_before.json: PNG·메타 해시.

## 검증
7장 모두 알파 연결성분1(441775픽셀), 떨어진 성분0, 숨은RGB0, 알파1~31 픽셀0, 캔버스접촉0, 활성 Sprite Rect 밖 알파32 이상0, 각 경계 초과 폭0. 코 마스크 밖 RGBA 차이0, 전체 알파 차이0, 보호 눈 영역 차이0, 보호 안경 금색 영역 차이0. 마스크 내 공통 코 RGBA 불일치0. 모든 알파 bbox [33,66,790,856], 머리꼭대기66, 7장간 편차0. 얼굴 폴더 메타35개(대상7개 포함) 해시 유지, 변경0. 무관한 PNG28장 해시 유지. 눈선은 기존 픽셀을 보존했다. v1에서 기록한 기존 눈선 편차를 이번에 교정하지 않았다.

## 기록
- nose_compare.png: 원래 v0 / v1 / v2 세 칸, 원본 (475,315,95,90) 영역의 정확한 4배 최근접 확대.
- PortraitFix_v2_preview.png: 7표정 얼굴 비교. dark_light_edges.png: 검정·밝은 배경 실루엣 검사.
- generated_attempt1.png / generated_selected.png: 생성 원본. edit_target.png: 확대 입력. generation_prompts.json: 전체 프롬프트. patch_masks.json 및 nose_patch_mask.png: 합성 범위·금색 보호·눈 영역 보호·좌표 매핑.
- verification.json: 최종 Assets 바이트 검사. staging.json: 실제 배치 경로.

## 미해결·주의
사장님의 코 인상 시각 판정은 아직 받지 않은 시안이다. 규칙 충돌 없음: 이번 작업은 정적 초상화의 기존 연결을 보존하는 PNG 수정이며, 명시한 PNG까지 범위를 적용했다.
