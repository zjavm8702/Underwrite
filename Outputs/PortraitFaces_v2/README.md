# PortraitFaces v2 — scholar · elf · fairy

2026-10-17 / PNG 제작·수치 검증 완료, 사용자 시각 승인 전 시안. Assets·Unity 연결 제외.

## 납품

루트의 face_{scholar|elf|fairy}_{표정}.png 21장. 전부 820×1080 8bit RGBA. 피봇·PPU 미설정. 전신을 자르지 않고 상반신을 새로 그렸다.

열 순서: neutral → happy → anxious → angry → shocked → sad → relieved. 3명×7개의 정지 표정이며 8방향 애니메이션이 아니다. 표정별 게임 재생 시간은 정하지 않았다.

## 숫자 검수

bbox 원점은 왼쪽 위, 끝 좌표 포함. 알파 32 미만 제거 후 알파>0의 bbox다. 같은 인물 7장 모두 왼쪽 x·머리 꼭대기 y 편차 0px. 전체 알파 및 얼굴 합성 마스크 밖 RGBA 변경도 0픽셀.

흰색 후보: alpha≥200, RGB 모두≥244, max(R,G,B)-min(R,G,B)≤10. 분모는 alpha≥200 픽셀 수. 전부 1% 미만(0.004516~0.053841%).

| 파일 | bbox (x0,y0,x1,y1) | 머리 y | 왼쪽 x | 흰색 후보 % |
|---|---|---:|---:|---:|
| face_scholar_neutral.png | (36, 101, 783, 950) | 101 | 36 | 0.005645 |
| face_scholar_happy.png | (36, 101, 783, 950) | 101 | 36 | 0.004516 |
| face_scholar_anxious.png | (36, 101, 783, 950) | 101 | 36 | 0.004516 |
| face_scholar_angry.png | (36, 101, 783, 950) | 101 | 36 | 0.004516 |
| face_scholar_shocked.png | (36, 101, 783, 950) | 101 | 36 | 0.004516 |
| face_scholar_sad.png | (36, 101, 783, 950) | 101 | 36 | 0.005363 |
| face_scholar_relieved.png | (36, 101, 783, 950) | 101 | 36 | 0.004799 |
| face_elf_neutral.png | (33, 66, 789, 855) | 66 | 33 | 0.013492 |
| face_elf_happy.png | (33, 66, 789, 855) | 66 | 33 | 0.023783 |
| face_elf_anxious.png | (33, 66, 789, 855) | 66 | 33 | 0.037275 |
| face_elf_angry.png | (33, 66, 789, 855) | 66 | 33 | 0.019209 |
| face_elf_shocked.png | (33, 66, 789, 855) | 66 | 33 | 0.039105 |
| face_elf_sad.png | (33, 66, 789, 855) | 66 | 33 | 0.008004 |
| face_elf_relieved.png | (33, 66, 789, 855) | 66 | 33 | 0.006403 |
| face_fairy_neutral.png | (38, 108, 781, 964) | 108 | 38 | 0.034358 |
| face_fairy_happy.png | (38, 108, 781, 964) | 108 | 38 | 0.042319 |
| face_fairy_anxious.png | (38, 108, 781, 964) | 108 | 38 | 0.053841 |
| face_fairy_angry.png | (38, 108, 781, 964) | 108 | 38 | 0.030377 |
| face_fairy_shocked.png | (38, 108, 781, 964) | 108 | 38 | 0.035615 |
| face_fairy_sad.png | (38, 108, 781, 964) | 108 | 38 | 0.033101 |
| face_fairy_relieved.png | (38, 108, 781, 964) | 108 | 38 | 0.028911 |

각 장 연결 성분 1개(본체), 분리 잡성분 0개, 숨은 RGB 0픽셀, 캔버스 접촉 0픽셀, alpha1~31 0픽셀. 넓은 내부 불투명과 가장자리 안티앨리어싱을 보존했다.

눈선 수치는 verification.json과 alignment_table.csv에 기록했다. 검출창은 v1과 다른 인물별 창이며 R<150,G<110,B<110 조건을 사용한다. 안경·속눈썹·감은 눈꺼풀이 수치에 포함되어 실제 머리 이동을 뜻하지 않는다. 정렬 통과 수치는 요청한 머리 꼭대기·왼쪽 x 편차이며, 눈선 수치를 v1의 해부학적 눈 위치와 직접 비교하지 않는다.

## 미리보기

- PortraitFaces_preview.png: 세 인물×일곱 표정 전체 비교.
- previews/{인물}_dark_preview.png, {인물}_white_preview.png: #141014·흰색 배경, 7장 나란히.
- previews/{인물}_seven_overlay.png: 일곱 장 평균 겹침.
- previews/tavern_all_preview.png: 기준 여관 배경 위 21장.
- previews/fullbody_comparison.png: 요청한 원본 전신과 새 상반신 비교.
- review.html: 개별 표정 및 700ms 교체(검수용 4.9초 반복; 게임 시간 규칙 아님).

## 제작·보존 이력

기본 제공 image_gen을 사용했다. 모든 표정은 생성 도구로 새로 그렸고, 코드로 눈·입을 그리거나 표정을 변형하지 않았다. 생성 표정을 공통 캔버스에 정렬한 뒤 얼굴 영역 RGB만 7px 경계 혼합으로 합성했다. 전체 알파와 머리·몸·의상 바깥 영역은 인물별 평상 원화와 같다.

generation_prompts.json은 기존 공유 작업의 프롬프트와 이번 생성·교정 프롬프트 전문을 함께 보존한다. generation_current/에는 최종 채택 생성 원본, generation_sources.json에는 해시가 있다. locked_bases/는 최종 고정 원화다. face_masks.json과 rebuild-current.mjs로 최종 합성을 재현한다.

작업 중 같은 Outputs 폴더의 평상·기쁨 파일 등이 다른 구도로 변경되는 상황을 감지했다. 현재본을 preserved_existing/에 보존하고, 최종은 독립 verified_delivery/에서 구성·검증한 뒤 루트에 배치했다. 엘프 기준 원화는 실제 초반 네 표정이 참조했던 기존 생성본을 채택했다. 구도가 섞인 엘프 sad/relieved 및 페어리 angry/shocked는 고정 원화를 참조해 재생성했으며 잘못된 중간본은 *_rejected.png로 남겼다. 중간 세로 정렬 보정 시도는 최종에 적용하지 않았다.

검증·보존본은 face_ 접두사를 피하여 재귀 임포터가 중복 묶음을 만들지 않도록 했다. 루트의 정확한 face_*.png 21장만 납품 대상이다.

before.json은 기존 시작 기록, before_current.json은 이번 세션 시작 기록이다. 기존 Assets 메타 495개 해시 유지, 변경 0개. 검사 중 Materials/CharacterShadow.mat.meta, Materials.meta, UI/Images/character_shadow.png.meta 3개가 외부에서 추가된 것을 관측했다. 본 작업은 Assets에 쓰지 않았고 해당 추가 파일을 건드리지 않았다.

## 문서 갱신·주의

기존 Documents/Codex/Underwrite_대화용_상반신_표정_제작기준.md에 v2 좌표와 고정 참조본 보관 지침을 추가했다. 상시 지시서 요약의 Assets 배치 일반 규칙보다 이번 요청의 Outputs 전용·PNG까지 범위를 우선했다.

사용자 시각 승인이 남아 있다. 의상·장식의 새 상반신 해석과 표정 강도는 전신 비교 및 미리보기로 검토한다. Unity 임포트·피봇·PPU·게임 내 표시 검증은 요청 범위 밖이다.