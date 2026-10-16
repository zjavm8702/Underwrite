# 주인공 환호 모션 시안

- 정면(S) 전용, 8프레임. `Assets/Images/Characters/Protagonist/Cheer/S/cheer_s_00.png`~`07.png`.
- 기존 잭팟과 별도로 제작한 환호. 미소 → 두 주먹에 힘 모으기 → 양팔을 들어 기뻐하기 → 팔 내리고 정리. 점프 없이 접지 유지.
- 512×960 RGBA, 공통 바닥 y947, 권장 피봇 하단 좌표(256,13). 기존 주인공 Idle과 같은 PPU로 비교한다.
- 권장 시간 240/120/120/120/300/180/180/300ms, 총1.56초, 게임에서는 일회 재생. 검토용 preview.png는 반복한다.
- 이미지 생성은 기본 제공 image_gen 사용. generation_prompts.json에 전체 프롬프트와 참조를 기록한다. source.png는 생성 원본, sheet.png는 정렬된 프레임 모음이다.
- 코드 후처리는 알파32 미만·분리 잡점 제거, 동일 배율 리샘플링, 발 기준 정렬, 분할·미리보기 출력만 수행한다.
- 기존 .meta와 Animator·Sprite Library·Clip은 수정하지 않는다. 새 이미지의 Unity 임포트 및 모션 연결은 별도다.
- 프레임별 미세한 주름·윤곽 차이는 남은 시안이다. verification.json에 캔버스·알파·기존 메타 검사 결과를 보관한다.
