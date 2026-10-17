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
