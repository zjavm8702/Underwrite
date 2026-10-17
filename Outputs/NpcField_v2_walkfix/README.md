# NpcField v2 walkfix — 미완료 시안

**게임 투입용 완료본이 아니다.** 4유형×5방향×8프레임의 Walking 본체160장과 대응 마스크160장을 새 폴더에 만들었다. PNG 기술 검사는 통과했지만 요청의 핵심인 정상 보행 교대 검수는 실패/미완료다.

## 진행표

|순서|대상|상태|
|---|---|---|
|①|leader|5방향40장+40마스크 시안. S05 장갑 누락을 image_gen으로 보정. 마스크에서 커프스 제외|
|②|archer|5방향40장+40마스크 시안|
|③|shield|5방향40장+40마스크 시안. SE에 생긴 불필요 검집 생성본은 제외하고 재생성|
|④|porter|5방향40장+40마스크 시안|
|⑤|검수|알파·잘림·개수·마스크 범위·발밑·보존 검사 통과. 발 교대 엄격 대조32행 확인/128행 미확정. 정상 걷기 합격 아님|

## 배치·규격

- {leader,archer,shield,porter}/Walking/{S,SE,E,NE,N}/{방향}_00.png~07.png 및 *_mask.png. 본체160+마스크160.
- 모든 납품 PNG: 640×724 RGBA. 기존 파일명·프레임 수·캔버스 유지. PNG 공통 기준점 Unity 아래(320,13), PNG 위(320,711). Unity 피봇을 실제 설정한 것은 아니다.
- 100ms×8=800ms 반복. timing.json은 기존 version:2 및 characters→Walking→directions 구조를 유지한 걷기 전용 파일이다. Idle은 새로 복사하지 않았으며 기존 Outputs/NpcField_v2의 것을 그대로 사용한다.
- 기존의 왼쪽 방향 반전 방식을 바꾸지 않았다. 이번 폴더에 SW/W/NW 원화를 만들지 않았다.
- Outputs/NpcField_v2의 기존773파일과 참조 주인공 .meta203개 SHA256 유지. Assets 쓰기·Unity 연결 없음.

## 제작·후처리

새 동작은 built-in image_gen 22회(20방향 시트+leader 장갑 보정+shield SE 재시도)로 제작했다. 원본은 sources, 프롬프트 전문은 generation_prompts.json, 원본 경로·해시는 generation_manifest.json이다. CLI/API fallback은 사용하지 않았다.

주인공의 실제 같은 방향·번호 PNG40장을 4열×2행 자세 참조 시트로 만들어 사용했다. 00~03은 윗줄, 04~07은 아랫줄이다. 생성 시트는 연결 성분으로 분리했으며 방향당 공통 배율, 몸통 밴드 중심, 발밑 세로 기준으로 배치했다. 발의 앞뒤 폭으로 X를 매 프레임 재정렬하지 않았다. 포즈마다 별도로 확대·축소하지 않았다. 이미지 변형 코드로 새 다리를 그리지 않았다.

알파32 미만 제거, 내부 불투명도 정리, 본체와 떨어진 생성 잡점 제거, 알파0의 RGB0 처리. 마스크는 흰색 불투명/나머지 RGBA0이며, 천 내부 회색 중앙값179를 기준으로 명암을 유지했다. largest-component 처리는 leader의 별개 커프스를 단 색 망토에서 제외할 때만 마스크에 적용했다.

## 검수 결과와 핵심 미해결

- 본체160장 모두 연결 성분1. 분리 잔점0, 숨은 RGB0, 캔버스 접촉0, alpha1~31 픽셀0.
- 마스크160장 모두 빈 마스크 아님, 본체 알파 밖0. 발 실루엣 최하단의 방향 내 Y 편차0px. 이는 좌우 발 교대·실제 접지 검증과 다른 지표다.
- 20개 APNG의 각8프레임·100ms 지연·무한반복 및 최종 PNG와 동일 IHDR/IDAT 스트림을 확인했다. 실제 화면 재생은 미확인이다. 브라우저 목록이 비었으며 iab는 Browser is not available 오류였다.
- 발 교대 표는 foot_alternation.csv/json/md. 총160행, S 범주 일치32행을 육안 확인했다. 나머지128행은 잠정 해석/미확정이며 정상 보행 합격으로 처리하지 않았다. 같은 문자열을 복사한 것을 검증 증거로 사용하지 않는다.
- 주인공 S06·07의 앞발은 S00과 같은 왼발 전방 계열로 읽힌다. S02에서 같은 왼발의 바닥이 노출되고 오른발이 접지하는 등 일반적인 반주기와 다른 진행도 보인다. **이 원본의 같은 번호에 똑같이 맞추라는 조건을 지키면서 정상 보행을 보장했다고 주장할 수 없다.** 이 판단은 확대 띠에 근거한 시각적 관찰이며 주인공 원본을 바꾸지 않았다. 정상 좌우 교대를 위해 04~07을 재설계할지는 사용자 판단이 필요하다.
- 일부 생성 프레임의 머리·의상·장비·명암은 기존과 미세한 차이가 남았다. porter 일부 허리의 작은 주머니 등 추가된 세부도 남아 있어 승인 완료 외형으로 취급하지 않는다.
- 상위 문서의 일반8방향·Unity 자동 연결 범위와 달리 이번 명시 요청의5방향·PNG만·Assets 금지를 적용했다. 상시 지시서·아트 최종 규칙·AGENTS.md 자체는 수정하지 않았다.

## 미리보기

- 유형별 Walking_5directions.png: 위에서 S/SE/E/NE/N. 각 방향은 주인공 한 줄 바로 아래 NPC 한 줄. 왼쪽부터00~07.
- 유형별 Walking_{방향}_comparison.png: 같은 방향 두 줄.
- 유형별 Walking_{방향}_legs.png: 허리 아래 y300~723을 원본 비율로 자른 두 줄, 주인공→NPC. Walking_legs_5directions.png는5방향 합본.
- 유형별 team_mask_red.png: 대응 마스크에만 빨강 곱셈 적용, 원본 PNG의 단 색을 빨강으로 바꾼 것이 아니다. edges_light_dark.png는 방향별00의 밝은/검은 배경 비교.
- 유형별 Walking_{방향}_animation.apng: 최종8장·800ms 반복. review.html: 방향·유형·번호 선택과 재생 비교. 제작 세션에서 브라우저 화면을 열지는 못했다.
- NpcField_v2_walkfix_preview.png: 유형 leader/archer/shield/porter 행, S/SE/E/NE/N 열의00 프레임.

## 기록

검증 verification.json, 배치·최종 PNG 해시 staging.json, 변경 전 해시 before.json, 발 교대 표 foot_alternation.*, 생성 원본 sources 및 generation_*.json. 재현 코드는 process.mjs·finish.mjs·audit_final.mjs·review_assets.mjs이며 이전 실행용 run_*.mjs도 남겼다.

재사용 문서 Documents/Codex/Underwrite_영지NPC_필드_제작기준.md에 발 bbox 검사와 실제 교대 검사를 구분하는 주의사항을 추가했다.
