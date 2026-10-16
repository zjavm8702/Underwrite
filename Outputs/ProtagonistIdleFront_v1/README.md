# 주인공 정면 Idle 시안 v1

정면 1방향, 6프레임의 대기 동작 시안입니다. 8방향 전체가 완성된 상태가 아닙니다.

- `Idle_front_preview.png`: 2.26초 반복 APNG 미리보기.
- `protagonist_idle_front_sheet_v1.png`: 2172×724, 가로 6칸. 각 프레임 362×724.
- `idle_front_00.png`부터 `idle_front_05.png`: 동일 캔버스 개별 프레임.
- 프레임 유지 시간: 900, 350, 80, 110, 120, 700ms.
- 생성: 내장 image_gen. 자세·색·얼굴은 생성 이미지에서 가져옴.
- 허용받은 후처리: 알파 32 미만 제거, 각 프레임에서 캐릭터 본체와 떨어진 성분 제거, 완전 투명 영역의 숨은 RGB 제거.
- 발밑 기준으로 프레임 전체를 평행 이동해 등록. 크기 변경이나 관절 변형은 하지 않음. 개별 Trim 대신 고정 칸으로 사용 권장.

작은 호흡 변화와 눈 깜빡임을 검토하기 위한 시안입니다. 생성 프레임 사이 의상 주름·윤곽의 미세한 차이는 남아 있습니다.

Unity용 원본 위치: `Assets/Images/Characters/Protagonist/IdleFront_v1`.
Sprite Editor 슬라이스와 AnimationClip 연결은 아직 수행하지 않았습니다.
