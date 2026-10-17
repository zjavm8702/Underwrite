# 화면별 틀과 단추 v1
상태: **시안 — PNG 14종 제작·검증·Assets 배치 완료, 사용자 시각 승인 전**. 범위 PNG까지. 기본 제공 image_gen 사용, CLI/API fallback 미사용. 생성 일자 2026-10-17.

## 납품 규격
Border 순서 Left / Bottom / Right / Top, 단위 원본 px. 모두 RGBA8. PNG에는 피봇·Border·PPU가 저장되지 않으며 실제 Unity 설정은 하지 않았다. 권장 UI 피봇 Center(0.5,0.5).

| 파일 | 크기 | Border |
|---|---|---|
| dialogue_box.png | 1024×512 | 64/64/64/64 |
| dialogue_nameplate.png | 512×128 | 40/40/40/40 |
| dialogue_next.png | 256×128 | 32/32/32/32 |
| choice_normal.png | 512×128 | 40/40/40/40 |
| choice_hover.png | 512×128 | 40/40/40/40 |
| choice_pressed.png | 512×128 | 40/40/40/40 |
| choice_locked.png | 512×128 | 40/40/40/40 |
| popup_panel.png | 768×512 | 64/64/64/64 |
| popup_button_normal.png | 512×160 | 48/48/48/48 |
| popup_button_pressed.png | 512×160 | 48/48/48/48 |
| scene_box.png | 1024×512 | 64/64/64/64 |
| village_button_normal.png | 512×160 | 48/48/48/48 |
| village_button_pressed.png | 512×160 | 48/48/48/48 |
| village_prompt.png | 768×256 | 64/64/64/64 |

최종 경로는 모두 Assets/UI/Images/<파일명>. Outputs 루트에도 동일 PNG를 보관한다. 거래소 market_panel / ui_button_* / ui_tab_* / ui_field 및 기존 UI는 수정하지 않았다. 새 PNG만 추가했고 .meta·USS·씬·애니메이션은 편집하지 않았다.

## 표현
대화는 짙은 나무와 작은 금속 덩굴 장식·크림 종이, 이름표는 짙은 바탕, 다음 단추는 둥근 나무 표식이다. 선택지는 종이 띠이며 hover는 왼쪽의 작은 붉은 책갈피, locked는 바랜 종이와 가장자리 갈라짐이다. 팝업은 접힌 편지와 좌하단 빈 밀랍 봉인 자리, 팝업 단추는 붉은 밀랍이다. 정보 타임은 검은 나무와 녹슨 철·짙은 갈색 면, 영지는 판자와 걸쇠 간판이다. 글자·숫자·도형 아이콘·봉인 문양은 넣지 않았다. 책갈피·못·밀랍은 요청한 물건의 일부다. 빛은 왼쪽 위, 외부 그림자·후광 없음.

## 처리·검증 방법
각 14종을 별도 image_gen 호출로 생성했다. 생성 원본은 source/, 전문 프롬프트·원본 경로는 generation_prompts.json. 원본의 알파32 미만과 분리 잡성분을 정리하고 본체 경계를 기준으로 crop했다. 원화의 모서리/변 구역을 지정 Border 폭에 재배치하고 중앙을 bilinear로 늘렸다. 새 장식·표정·원화를 코드로 그리지 않았다. 투명 픽셀은 RGB0, 캔버스 밖 잘림을 피하도록 2px 여백을 유지했다. sourceBorderXY·crop·변환 기록은 verification.json.

normal/pressed 3쌍은 normal의 Border 전체와 알파 마스크를 공유한다. 생성된 pressed의 안쪽 면만 feather 합성하여 외곽·테두리·텍스트 영역 위치를 고정했다. Border의 RGBA 불일치 0, 알파 불일치 0, 위치 차이0px이다. 눌림 코드의 0.96배 동작은 구현하지 않았다. 검수는 정적 UI로 방향·프레임·루프·총 재생 시간·SpriteResolver 매칭은 해당 없음.

알파 연결 성분 각1개 / 분리 잡성분0개 / 숨은 RGB0픽셀 / 알파1~31 0픽셀 / 캔버스 접촉0픽셀. 밝은/검은 배경 테두리를 alpha_edges_preview.png에서 확인했다. Border 장식은 borders_overlay.png의 청록선 안쪽 중앙 영역을 침범하지 않도록 모서리·변 구역에 수용했다. 실제 UI 실행이 아닌 CPU bilinear 9-slice 검수다.

## 미리보기
- UIFrames_preview.png: 아래 납품 순서의 전체14종, 두 열로 왼쪽→오른쪽→다음 행. 이 이미지는 비교용으로 비율을 통일한 contact sheet다.
- borders_overlay.png: 표의 순서대로 원본 비율 유지, 청록색 Border 선. 최대 높이205px로 축소한 검수 시트.
- stretched_preview.png: 아래17개 실제 화면 크기 샘플을 세로로 배치. 각 개별 PNG는 previews/. 화면에 표시할 때 이미지 뷰어가 축소할 수 있으므로 100% 배율로 확인한다.
- state_comparison.png: 행은 choice / popup_button / village_button, 왼쪽 normal / 오른쪽 pressed.
- alpha_edges_preview.png: 위 검정 / 아래 밝은 바탕, 각 블록의 순서는 전체 미리보기와 동일.

17개 샘플 순서:
1. dialogue_box 1100×420
2. dialogue_box 1300×700
3. choice_normal 1000×64
4. choice_normal 1200×84
5. choice_hover 1100×74
6. choice_pressed 1100×74
7. choice_locked 1100×74
8. popup_panel 560×260
9. popup_panel 560×420
10. village_button_normal 180×48
11. village_button_pressed 180×48
12. village_prompt 400×120
13. scene_box 1200×500
14. popup_button_normal 200×56
15. popup_button_pressed 200×56
16. dialogue_nameplate 280×64
17. dialogue_next 96×48

**Border 값과 표시 테두리 배율은 별개다.** 이 미리보기는 일반 부품 slice scale0.5, popup/village_button은0.25를 사용했다. 예: village_button 원본 Border48은 표시 시12px이며 48px 높이에서도 중앙24px가 남는다. scale1에서 상하Border 합96px가 48px 단추보다 커지는 문제를 피하기 위한 전달값이다. 실제 UI Toolkit 설정 및 글자 패딩은 Claude Code가 적용·실행 확인한다. 원본 Border 숫자·캔버스는 요청대로 유지했다.

## 보존·원문·남은 사항
before.json은 기존 메타와 기존 UI 파일 해시, staging.json은 실제 배치 경로와 SHA256, verification.json은 최종 검사 결과다. 기존 메타566개 유지 여부와 추가 메타는 검사 결과를 따른다. 기존 파일 덮어쓰기 없음.

원문 확인: Documents/Codex/Underwrite_아트_작업규칙.md, Underwrite_8방향_애니메이션_자동생성툴.md, Documents/Shared/Underwrite_기획서_v0.6.md §17·§18 및 세계/화면 설명, AGENTS.md, 기존 MarketUI 두 납품분 README·원본. 캐릭터 애니메이션 Unity 자동 연결 범위는 이번 정적 UI에 해당하지 않으며 이번 명시 요청 PNG 범위를 적용했다. 기획서 §18의 거래화면 UI 미착수 문구는 현재 납품분보다 과거 상태다. 기획서는 수정하지 않았다.

남은 작업: 사용자 시각 승인, Claude Code의 USS 교체·9-slice 설정·글자 패딩·상태 이미지 전환·0.96배 눌림 연출·Unity 실행 확인. 새 PNG는 배치했으나 실제 화면은 아직 기존 UI를 사용한다. UIFrames PNG의 피봇과 PPU를 임의로 적용하지 않았다. 이미지 생성 결과의 미세한 재질·상태 차이는 시안 검토 대상이다.

추가 최종 검수: 내부 알파247~254를 실루엣 형상 기준으로255 복원(3×3 이웃이 모두 알파32 이상인 본체). 경계1px만 원본 AA 유지. 중앙 비불투명0픽셀. 기존 메타566개·기존 UI PNG27개 SHA256 유지, 변경/삭제0개. 영지 pressed는 중앙 얼룩을 제거하도록 image_gen 편집1회 추가, 이전 후보도 source에 보관했다. 원본 총15장, 최종14장. Documents/README.md 원문도 확인했다.
재사용 지식 문서: Documents/Codex/Underwrite_UI_9slice_제작기준.md 신규 작성. 상위 아트 규칙·기획서는 수정하지 않았다.
