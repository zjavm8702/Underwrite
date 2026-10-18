# Underwrite UI 9-slice 제작·납품 기준
작성: 2026-10-17. 적용 근거: UIFrames_v1 요청과 검수. 상위 아트 규칙을 변경하지 않는 정적 UI 제작·전달 기록이다.

## 원본 Border와 표시 배율
PNG 파일은 Border·피봇·PPU를 저장하지 않는다. 원본 픽셀 단위 Left/Bottom/Right/Top과 화면에서의 slice scale을 따로 기록한다. 원본 Border 값만 작게 바꿔 모서리 장식을 자르지 않는다.
- UIFrames_v1의 대화/선택지/패널/간판 검수 배율: 0.5.
- popup_button/village_button 검수 배율: 0.25. Border48이면 표시 시12px이며, 높이48px에서도 중앙24px를 확보한다.
- 이 값은 이번 PNG 검수의 전달값이다. 실제 USS/UI Toolkit 렌더링·글자 패딩·화면 배치는 코드 담당자가 적용 검증한다. 다른 작업의 보편 기본값으로 강제하지 않는다.

## 원본 생성의 알파 함정
투명 배경 image_gen 원본은 본체 내부도 알파247~254인 경우가 있었다. 실루엣 바깥 알파0과 잡성분만 검사하면 이 문제를 놓친다. 중앙 사각형의 알파255 여부도 검사한다.
UIFrames_v1은 알파32 미만 제거·분리 잡성분 제거 후, 3×3 이웃 모두 알파32 이상인 본체 픽셀만255로 복원했다. 실루엣 경계의 1px 띠는 원래 AA를 유지했다. 투명 RGB0과 가장자리·작은 장식을 밝은/검은 바탕에서 다시 확인한다. 기준값을 높여 잘못된 원화를 숨기지 않는다.

## 상태쌍의 고정 경계
별도 생성한 normal/pressed가 동일 픽셀 위치를 보장하지 않는다. UIFrames_v1에서는 normal의 알파와 Border RGBA를 공통으로 사용하고, 도구로 생성한 pressed의 안쪽 면만 feather 합성했다. Border 픽셀·알파 불일치0과 위치 차이0px를 수치 검사했다. 본체를 코드로 새로 그린 것이 아니라 생성 원본의 크기·등록·알파·합성을 정리한 것이다. 중앙 얼룩처럼 보이는 면은 새 이미지 편집 생성으로 재작업했다.

## 검수·보존
borders_overlay.png, 요청한 실제 표시 크기의 stretched_preview.png, 검정/밝은 바탕 alpha_edges_preview.png, normal/pressed state_comparison.png를 함께 남긴다. CPU 검수와 Unity 실행 검수를 구분해 쓴다. 정적 UI에 캐릭터 8방향·재생 시간을 억지로 부여하지 않는다.
기존 PNG와 .meta의 SHA256은 배치 전후 비교한다. 새 경로에만 복사하며 충돌이 있으면 기존 파일을 덮어쓰지 않는다. Unity 연결을 요청하지 않은 PNG 작업에서는 .meta·USS를 편집하지 않는다.

재현: Outputs/UIFrames_v1/processing.mjs → finish.mjs(앞 단계를 import함) → audit_and_stage.mjs. 마지막 스크립트는 새 배치 전용으로 이미 목적 파일이 있으면 중단한다. 검수 재생성은 node Outputs/UIFrames_v1/finish.mjs. 기존 Assets 파일을 자동 덮어쓰지 않는다.

## 거래 화면 키트 v1 검수 전달값 — 2026-10-18

- 기록: Outputs/MarketUI_Kit_v1. PNG만 납품했으며 Assets·USS·메타·Unity 연결은 수정하지 않았다. 13종과 경계선·확대·모의 화면을 납품한다. 승인 전 시안이다.
- 원본 Border는 요청 표를 유지한다. 이번 키트의 검수 slice scale은 **0.25**이며, UI Toolkit에서는 해당 표시 배율을 적용하는 것을 권장한다. uGUI의 동등한 표시값은 Image pixelsPerUnitMultiplier=4(참조 PPU와 Sprite PPU가 같은 경우)이다. 현행 거래 화면은 MarketScreen.uxml의 UI Toolkit이므로 uGUI 설정을 그대로 지시하지 않는다. 실제 USS 연결은 코드 담당자 검증 대상이다.
- 높이96인 간판의 원본 상하Border 합은144, 높이48~56인 단추는96이다. 배율1이면 중앙이 사라질 수 있어 원본Border와 표시배율을 함께 전달한다. Border 수치를 줄여 해결하지 않는다.
- 왼쪽 가장자리의 작은 밀랍도 좌측 중간 슬라이스에 걸치면 세로로 늘어난다. v1은 밀랍을 원본64×64 고정 모서리 안(x12,y12,44×43)에 넣었다. 가로세로 비율을 지켜야 하는 장식은 모서리 안에 넣는다.
- 주/보조 틀은 Border값만 다르게 두어도 실제 나무 폭이 같아질 수 있다. 실제 표시 크기에서 나무 폭을 따로 비교한다. v1 보조 틀은 생성된 테두리의 폭을 더 줄였으며 원본Border56과 종이색은 유지했다.
- 버튼 상태별 독립 생성은 위치가 다를 수 있다. v1 다섯 상태는 normal 틀·알파를 공통으로 사용하고, 이미지 생성 원본의 상태별 안쪽 면을 합성했다. 비활성 틀은 같은 위치에서 채도를 낮췄다.
