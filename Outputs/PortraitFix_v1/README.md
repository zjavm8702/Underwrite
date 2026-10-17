# PortraitFix_v1 — PNG 수정 시안 배치

2026-10-17. 기본 제공 image_gen으로 학자 안도 1장 및 엘프 코 공통 수정 원화를 생성했다. 사용자 시각 승인 전 시안이며, 지정 Assets PNG8장 교체를 완료했다. Unity 에셋 연결은 기존 것을 유지한다.

## 변경
학자: 현재 Assets 평상의 몸·머리·얼굴 윤곽·안경을 고정하고 생성된 감은 눈과 숨 내쉬는 입만 합성했다. 학자 안도의 패치 밖 RGBA는 평상과 0픽셀 차이. 어깨 이동은 표정 밖 고정 조건에 따라 적용하지 않았다.
엘프: 평상에 작은 콧날·콧방울 아래 그늘 및 안경 코받침 접촉감을 보완하고, 동일한 코 패치의 RGBA를 7장에 사용했다. 코 패치 밖은 각 기존 표정과 0픽셀 차이. 눈·입·안경테·귀·머리·장신구를 유지했다.

820×1080 RGBA, 기존 파일명·전체 알파 유지. 메타14개 점검 모두 유지(실제 교체 대상8개 포함). spriteMode1의 유효 Rect는 전체 캔버스, 피봇 alignment0 중앙(410,540), PPU100 유지. SpriteSheet 아래 남은 rect는 Single 모드의 활성 Rect가 아니다.

## 검증
수정 대상8장 및 두 인물14장을 검사: 연결성분1, 숨은RGB0, 캔버스접촉0, alpha<32 픽셀0. 인물별 전체 알파 변경0, bbox/머리꼭대기 편차0. 눈선 편차: 학자4.5/3.5px, 엘프8.5/10.5px. 엘프 원거리 눈의 기존10.5px는 요청의10px 기준 초과이며 눈 변경 금지에 따라 그대로 유지했다. 기존 문서 눈 검출창은 학자 현재 Assets 위치와 달라 학자에 한해 현재 창을 기록했다.

기존 표정 마스크 밖 평상 대비 차이도 그대로 기록했다: 학자 neutral/happy/anxious/angry/shocked/sad/relieved=0/2/2/0/1/2/0px. 엘프=0/5/6/5/6/20/3249px. 엘프 안도는 기존 얼굴 합성 영역의 차이가 크다. 코 밖 원본 보존 요구 때문에 이번에 고치지 않았다. 이 기존 차이를 0이라고 보고하지 않는다.

## 파일
- before/original_*: 교체 전 원본14장(중복 검색 방지 접두사). before.json: PNG 및 메타 SHA256.
- final/revised_*: 배치된 수정본8장. staging.json: 적용 경로. verification.json: 배치 후 SHA256·알파·픽셀·정렬 검증.
- before_after.png: 학자 안도/엘프 평상/엘프 기쁨, 왼쪽 전·오른쪽 후, 2배 확대.
- *_seven_overlay.png: 두 인물 각각7장 평균 겹침. elf_nose_all_comparison.png: 위 전/아래 후, 표정 순서대로.
- *_generated*.png: 생성 원본, *_edit_target.png: 고정 참조, generation_prompts.json: 전문. patch_masks.json 및 *_patch_mask.png: 합성 영역과 보존 기준.
- review.html: 700ms/장,7장4.9초의 검수용 교체 미리보기. 게임의 재생 시간은 정하지 않음. 실제 Unity 재생은 범위 밖이라 미실행.

문서의 v2 과거 'Assets 제외'는 당시 제작 범위다. 이번 사용자의 PNG 교체 요청을 적용했다.
