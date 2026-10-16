# 주인공 수정 및 인간 여성 용사 Idle 시안

## 전달 위치
- SW 걷기: `Assets/Images/Characters/Protagonist/Walking/SW/walk_sw_04.png`, `walk_sw_05.png` 교체. 나머지 6프레임 유지. 640×724.
- 주인공 잭팟: `Assets/Images/Characters/Protagonist/Walking/Jackpot_S/jackpot_s_00.png`부터 `07.png`까지 교체. 512×960.
- 인간 여성 용사: `Assets/Images/Characters/Heroes/HumanFemale/Idle/{S,SE,E,NE,N,NW,W,SW}`. 방향당 6프레임, 총 48장. 512×724. 주인공보다 작은 키(실루엣 최대 620px, 주인공 약699px). 같은 PPU에서 비교한다.

## 재생
- 인간 여성 Idle: 900 / 350 / 80 / 110 / 120 / 700ms, 반복.
- 잭팟: 280 / 160 / 120 / 140 / 280 / 180 / 200 / 350ms, 합계1.71초. 정면 전용. 놀람 → 기대 → 웅크리기 → 두 팔 들어 환호 → 정리. 이번 판은 제자리 환호로 점프는 넣지 않았다. 게임에서는 일회 재생; 검토용 APNG는 반복한다.
- 바닥 기준은 Idle y711, 잭팟 y947. 권장 피봇은 하단 기준 (256,13). 기존 임포트 설정은 수정하지 않았다.

## 제작과 보존
그림은 기본 제공 image_gen 도구로 제작. 파이썬은 알파 잡점 정리, 완성 인물 추출, 동일 비율 축소, 캔버스 정렬, 분할과 미리보기 출력에만 사용했다. limbs를 변형하여 만든 리깅 애니메이션이 아니다.

기존 PNG는 Before 폴더(SW는 WalkingArmFix_SW_v3/before.json에 경로 기록)에 보관. 기존 .meta를 직접 작성하거나 변경하지 않았다. 새 여성 용사 PNG는 Unity가 새 에셋으로 임포트하며, Animator나 Sprite Library 연결은 추가하지 않았다.

프롬프트는 generation_prompts.json, 원본은 각 방향 source.png, 검사 결과는 validation.json에 기록한다. 프레임별 세부 주름·장식·윤곽에 생성 이미지 특유의 미세한 차이는 남아 있는 시안이다.
