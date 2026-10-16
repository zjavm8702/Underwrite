# 거래 화면 조작 부품 양피지화 v1

상태: **시안 — PNG 6종 제작·검증 완료, 사용자 시각 승인 전**. 범위: PNG까지, Outputs 안에만 배치. Assets·USS·Unity 연결 수정 없음.

## 납품

루트의 ui_button_normal / ui_button_pressed / ui_button_disabled / ui_tab_on / ui_tab_off / ui_field.png가 최종 PNG 6장이다. 전부 512×192 RGBA8, Border Left/Bottom/Right/Top **48/48/48/48**, slice scale **0.5**. PNG에는 Border·피봇·PPU가 저장되지 않는다. UI 피봇 권장 Center(0.5,0.5), 실제 설정 미수행. 글자·숫자·기호·화살표·글로우·외부 드롭섀도 없음. 정상은 살짝 돋은 종이, 선택은 짙은 잉크 면, 비활성은 바랜 회색 종이, 선택 탭은 열린 하단, 비선택은 뒤로 물러난 종이, 입력은 얇은 안쪽 홈이다. 승인된 캐릭터 외형이나 애니메이션과 관련 없는 정적 UI 작업이다.

## 검수 이미지

- **preview.html**: 이름과 실제 크기 라벨이 있는 검수 갤러리. 모든 이미지가 해당 크기로 표시된다.
- **nine_slice_preview.png**: 왼쪽부터 normal, pressed, disabled, tab_on, tab_off, field. 최상단은 256×96 기준 보기. 아래 행은 순서대로 **90×40, 120×40, 150×40, 80×34, 180×46, 86×46, 390×48**. 요청한 5개 용도와 상단 버튼 최소/중간/최대 폭을 포함한다. 6종×7크기=42샘플. 개별 PNG는 previews/.
- **state_comparison.png**: 위 순서대로 여섯 원본을 한 줄에 놓은 상태 비교(3072×192).
- **alpha_edges_preview.png**: 흰색/검은색 배경. 위·아래 각 블록에서 첫 행 normal/pressed/disabled, 다음 행 tab_on/tab_off/field.

미리보기는 CPU bilinear 9-slice 재현이며 Unity 실행 캡처가 아니다. Border48×scale0.5=24px이므로 높이48px 미만에서는 맞은편 Border를 비례 축소하고 중앙 높이를0으로 처리했다. 34px에서도 정상·선택·비활성을 구분하고 얇은 홈을 읽을 수 있다. 시각 검사에서 이음새·장식 단절 없음. 상하 테두리/입력 홈 구조는 원본의48px 범위 안에 있다. Unity UI Toolkit의 실제 경계 축소 처리 및 글자 패딩은 적용할 때 대조해야 한다. 기본 UI가 코드에서 글자색/배경 tint를 지정하므로 붉은·푸른 버튼은 만들지 않았다.

## 중앙 대비

원본 중앙 사각형 x48..463, y48..143의 **모든 픽셀 중 최솟값**, sRGB 선형 상대휘도 기준. normal/disabled/tab_on/tab_off/field는 #2A2118, pressed는 #F7F0E2 글자 기준. 전부4.5:1 통과.

| 파일 | 최소 대비 | 중앙 휘도 표준편차 |
|---|---:|---:|
| ui_button_normal.png | 12.02:1 | 0.00563 |
| ui_button_pressed.png | 13.85:1 | 0.00039 |
| ui_button_disabled.png | 9.70:1 | 0.00652 |
| ui_tab_on.png | 13.38:1 | 0.00361 |
| ui_tab_off.png | 10.84:1 | 0.00704 |
| ui_field.png | 12.48:1 | 0.00361 |

알파32 이상 연결 성분은 각1개, 분리 잡성분 각0개, 숨은 RGB 각0픽셀. 내부는 알파255, 좁은 모서리만 AA. full-bleed UI이므로 캔버스 외곽 접촉은 의도된 구조이며 장식/실루엣의 우발적 잘림은 없다. 정적6종으로 방향·프레임·재생 시간·루프는 해당 없음.

## 보존 및 주의

기존 Assets/**/*.meta **398개 해시 유지, 변경/삭제0개**. 작업 중 사전 목록에 없던 **Assets/Temp/ui_toast.png.meta, Assets/Temp.meta** 2개가 추가 관찰됐다. 이 작업은 해당 경로에 쓰지 않았으며 생성 주체는 확인하지 않았다. 최종 검사에서는 이2개 임시 항목이 사라져 최종 추가0개다. 중간 관찰 이력은 기존 해시 변동과 분리해 verification.json에 기록했다. before.json은 작업 시작 시점의 전체 사전 해시다.

아트 규칙/자동생성툴/AGENTS의 Unity 연결 완료 범위는 캐릭터 애니메이션에 관한 규정이며 이번 정적 UI PNG 요청은 명시된 PNG 전용 범위를 적용했다. 기획서 §18의 거래화면 UI 미착수 문구는 기존 양피지 납품분이 있는 현재 상태보다 뒤처져 있다. 원문 문서는 수정하지 않았다.

## 원본·처리 기록

기본 제공 image_gen 사용, CLI/API fallback 미사용. 각 부품을 별도 생성했다. 원본·폐기 후보는 source/, 프롬프트 전문은 generation_prompts.json. pressed와 tab_on의 최초 생성은 도구가 중앙을 투명하게 지웠으므로 불투명 재생성을 사용했다. field는 작은 화면에서 테두리가 너무 두꺼워 얇은 홈으로 재생성했다. 생성시각은 파일과 before/verification의 작업 시각, 원본 경로는 generation_prompts/staging에 기록했다.

재생성 원본 포함2048×768 이미지를512×192로 bilinear 변환했다. normal의 위아래 빈 여백(y96..671)을 제거하고, tab_on의 아래20 source px를 잘라 열린 하단을 유지했다. 내부 알파255 복원, 반경12px 모서리 알파 정리(탭 하단은 직각), 알파32 미만 제거, 투명 RGB0 처리만 코드로 했다. 화풍·질감·상태 원화는 모두 이미지 생성 도구 결과다. processing.mjs는 최종6장을 재현하는 코드이며 source/를 기준으로 실행한다. staging.json에 적용 계획과 최종 SHA256을 기록했다. 프로젝트 아트 문서 변경 없음.
