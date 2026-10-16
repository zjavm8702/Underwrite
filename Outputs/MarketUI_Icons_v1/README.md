# 거래 화면 아이콘 v1 — 시안

ImageGen 기본 도구로 개별 생성. 승인 전 시안이며 최종 PNG 10종을 이 폴더에 납품한다. Assets에는 쓰지 않는다.

미리보기 열 순서: 1 rise, 2 fall, 3 flat, 4 expand, 5 collapse, 6 cancel, 7 star_on, 8 star_off, 9 locked, 10 warning. 행 순서 64, 24, 20, 16px. 정적 아이콘으로 방향·프레임·재생시간은 해당 없음. PNG 자체에는 피봇·PPU 메타데이터가 없고 UI 중심 (32,32)을 권장한다.

원본은 sources/, 프롬프트는 generation_prompts.json, 알파 검사와 면적은 verification.json, 기존 메타 SHA256은 before.json에 기록한다.

## 제작·검수

2026-10-16 제작. ImageGen 기본 제공 도구로 원본 10종을 각각 생성했다. 새 형상은 코드로 그리지 않았다. 원본 알파와 손으로 그은 외곽을 유지해 잔점·투명 RGB를 정리하고, 거의 불투명한 내부 알파(250 이상)를 255로 통일했다. 균등 비율 축소와 면적 평균 샘플링으로 64×64에 배치했다. 최종 알파 32 미만 제거 후 재검사했다. 보합은 시각적 무게를 맞추기 위해 최대 폭 56px, 나머지는 최대 변 50px 안에서 알파 면적 680px²를 상한 목표로 균등 축소했다.

상하좌우 여백 최소 4px 이상. 가시 픽셀 RGB는 모두 #FFFFFF이며 투명 RGB는 0이다. 각 아이콘 본체는 1개 성분, 경고만 삼각 테두리·세로 획·점의 의도된 3개 성분이다. 의도되지 않은 분리 성분은 0개다.

비교판 preview_ink.png / preview_paper.png는 원본64 및 실제24·20·16px이며 확대하지 않았다. MarketUI_Icons_preview.png는 두 판을 합친 파일이다. alpha_edges_preview.png는 검정/흰 바탕에서 64px 테두리를 확인한다. 종이 바탕에만 잉크색 틴트를 적용했으며 납품 PNG의 RGB는 변경하지 않았다. preview.html은 아이콘 이름과 실제 크기를 함께 볼 수 있다. 16px에서도 10종을 구분하고 별의 채움/테두리와 잠금 고리를 확인했다.

## 알파 면적

분모 4096px, 알파 가중 면적 = sum(alpha/255).

| 파일 | 알파 면적 비율 | 비영 알파 점유율 |
|---|---:|---:|
| icon_rise.png | 16.520% | 17.480% |
| icon_fall.png | 16.634% | 17.676% |
| icon_flat.png | 10.899% | 11.719% |
| icon_expand.png | 12.325% | 13.916% |
| icon_collapse.png | 14.536% | 15.918% |
| icon_cancel.png | 16.328% | 18.359% |
| icon_star_on.png | 16.752% | 18.091% |
| icon_star_off.png | 14.548% | 17.358% |
| icon_locked.png | 16.455% | 17.554% |
| icon_warning.png | 15.741% | 18.384% |

최대/최소 알파 가중 면적 비는 1.537배다. 보합·테두리 기호의 빈 공간 때문에 수치를 완전히 같게 하지 않고 실제 축소 가독성을 함께 확인했다.

## 보존 및 주의

기존 Assets 메타 404개 모두 SHA256 유지, 기존 파일 변경·삭제 0개. 작업 중 신규 메타 5개가 감지됐다. 본 작업은 Assets 파일을 쓰지 않았다. 신규 경로는 verification.json에 기록했다.

- Assets/Temp/ui_scene.png.meta
- Assets/Temp/ui_scene2.png.meta
- Assets/Temp.meta
- Assets/UI/Images/underworld_room.png.meta
- Assets/UI/Images/underworld_room_dim.png.meta

승인 전 시안 10종. Unity 연결·임포트·게임 UI 교체는 요청 범위 밖이다. 캐릭터 애니메이션 관련 기본 규칙과 달리 이번 요청은 정적 아이콘 PNG만 Outputs에 납품하도록 명시했으므로 그 범위를 적용했다. 기존 프로젝트 문서는 변경하지 않았다.
