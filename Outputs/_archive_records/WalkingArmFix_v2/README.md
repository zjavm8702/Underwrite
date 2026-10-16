# 걷기 팔 모션 수정 v2

대상: `Assets/Images/Characters/Protagonist/Walking/SE`, `NE`, `NW`.

각 방향의 04·05번, 즉 팔의 선행 방향이 바뀌는 두 프레임을 수정한다. SE는 배 높이로 접힌 팔꿈치를 낮춰 다른 방향과 비슷한 느슨한 보행 자세로 바꾸고, NE·NW는 팔이 계속 같은 쪽에 머무르던 구간에 반대 방향의 어깨·팔 움직임을 넣는다.

원화는 내장 image_gen으로 수정했다. 전체 시트 수정, 개별 포즈 수정 결과를 검토했고, 실제 팔 실루엣 변화가 확인된 개별 프레임을 선택한다. 검토에서 제외한 생성 결과를 프로젝트에 적용하지 않는다. 최종 프롬프트는 `selected_prompts.json`에 보관한다.

원래 PNG의 크기 **640×724**, 발밑 위치, 머리 중심을 기준으로 프레임을 등록한다. 이미지 크기·이름·프레임 수는 바꾸지 않는다. 알파 잔점과 투명 영역의 숨은 RGB를 정리한다. 생성 결과에는 팔 이외 선·명암의 미세한 차이가 남을 수 있다.

## 적용

- 기존 위치의 해당 PNG만 덮어쓴다.
- `.meta`, Sprite Library, Animator, AnimationClip, 임포터·피봇·슬라이스 설정은 변경하지 않는다. Unity Editor API로 재설정하지도 않는다.
- 작업 전 PNG 원본을 `before.json`에 적힌 작업 폴더에 백업한다.
- 작업 전후 Walking 아래 메타 파일과 Walking.meta의 SHA-256을 비교한다.
- 실제 교체 파일과 보존 검사 결과는 `applied.json`에서 확인한다.

## 미리보기

`Walking_arm_corrections_preview.png`: 왼쪽부터 SE, NE, NW. 수정한 프레임과 기존 나머지 프레임을 조합한 반복 APNG다. 재생 속도는 기존 납품과 같은 프레임당 100ms다.

`Walk_{방향}_keyframe_preview.png`: 방향별 반복 미리보기.

`before.json`: 원본 PNG와 메타 파일의 해시. `keyframe_staging.json`: 선택 원화와 출력 좌표 기록.
