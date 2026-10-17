# 용병단 종목 대화 초상 v1

상태: PNG 제작·검증·Assets 배치 완료. 사용자 시각 승인 전 시안.
범위: PNG까지. PortraitSet·종목 연결·Unity 임포트 설정은 Claude Code 담당.
제작 도구: 기본 제공 image_gen, 24회(평상5 + 표정15 + 블랙무어 재설계1 + 재설계 표정3). API/CLI 미사용.

## 납품

Assets/UI/Images/Faces/face_band_{sella,brann,blackmoor,warden,lantern}_{neutral,anxious,shocked,happy}.png
5명×4표정=20장, 820×1080 8bit RGBA. 손·무기 없는 상반신 신규 원화, 약한 화면 오른쪽 3/4. 피봇·PPU는 PNG 단계 미설정. 후속에는 같은 UI 기준점을 사용하고 개별 자동 크롭을 피한다.
고정 표정 이미지이며 애니메이션·8방향 대상이 아니다. 게임용 재생시간이나 전환조건을 정하지 않았다.

## 디자인과 검수

셀라: 짧은 밤색 머리, 가죽 갑옷, 야심 있는 눈.
브란: 60대 백발·면도한 얼굴, 오래된 뺨 흉터, 짙은 강철 갑옷·남색 겉옷.
블랙무어: 30대 검은 언더컷, 긴 얼굴·매부리코, 뾰족한 거친 검은 수염, 눈가 흉터, 검은 갑옷·위험한 웃음. 초기 원화가 은갑 용사와 유사해 폐기하고 재제작했다.
파수대: 40대 여성, 짧게 묶은 잿빛 머리, 회색 두건 망토, 바람에 튼 피부·매서운 눈.
등불 기사단장: 30대 금발 남성, 귀족적인 얼굴, 흰 겉옷·은빛 판금, 가슴 등불 문장.

vs_heroes.png: 왼쪽5명은 위 순서의 신규 용병. 오른쪽5명은 scholar / 기존 face_sella(흑발 검사) / elf / 기존 face_brann(은갑 워리어) / fairy. 열 라벨로 구분한다. 동일인물로 읽히지 않도록 얼굴형·머리·수염·복장 차이를 대조했다. 사용자 시각 승인은 남아 있다.

BandPortraits_preview.png, white_preview.png, tavern_preview.png: 행은 셀라/브란/블랙무어/파수대/등불, 열은 neutral/anxious/shocked/happy. 20장 전체를 밝은 배경·#141014·여관 위에서 검수했다. 이중 눈·입, 잘린 머리, 외곽 후광·흰 배경 잔여 여부를 확인했다.

## 고정과 검증

인물별 neutral을 동결한 뒤 각 표정은 image_gen으로 제작했다. 생성 이미지를 820×1080에 맞추고 고정 머리 영역으로 이동 정렬한 후, 눈썹·눈·입 주변 마스크에만 합성했다. 코·머리·목·몸·옷·전체 알파를 평상 원화에 고정한다. 코드로 표정을 그리거나 변형하지 않았다. 마스크 가장자리는 6px 이내 혼합이다.
알파32 미만 제거, 본체 밖 분리 잔점 제거, 숨은RGB0. 넓은 내부 알파를 불투명으로 정리하되 좁은 안티앨리어싱은 유지했다.

20장 모두 연결 성분1개(분리 성분0), 숨은RGB0, 캔버스 접촉0. 흰색 후보는 모두1% 미만이다.
인물별 전체 알파 차이0px, 얼굴 마스크 밖 RGBA 차이0px, bbox·머리꼭대기 편차0px.
고정 눈 검출창에서 어두운 눈선/홍채 bbox 중앙 y를 측정한 근/원 눈선 편차: 셀라2/0px, 브란0.5/0px, 블랙무어0.5/0px, 파수대0.5/0px, 등불0/0px. 모두10px 이하. 이는 해부학적 눈 중심 자체를 의미하지 않아 실제 눈·코 정렬도 함께 확인했다. 검출창·좌표는 verification.json, face_masks.json, alignment_table.csv, eye_windows.png 참조.
기존 PNG35장과 meta35개(총70파일) 해시 유지, 변경0. Assets 최종20장과 납품 검증본 해시 전부 일치.

## 기록 구조와 재현

raw/: 생성 원본24장(폐기본은 rejected 표기). aligned/: 동결 neutral과 합성본. delivery/: 검증된 최종본(파일명 중복 검색을 피하려 portrait_ 접두 사용).
preserved_existing/: 기존 파일70개 보존 사본(preserved_ 접두).
generation_prompts.json: 프롬프트 전문·참조·도구. generation_sources.json: 원본 경로·시각·해시·크기.
before.json / after.json / preservation.json: 전후 보존 검사.
staging.json: 신규 배치 계획·실제경로·전후 해시.
verification.json: 전체20장 알파·연결성분·흰색후보·고정성·눈선 검사.
rebuild_manifest.json / rebuild.mjs / png_tools.mjs / expression_tools.mjs / tools/: 재현 입력·정렬·얼굴합성 유틸.

## 참조 문서와 적용 범위

Documents/Codex/Underwrite_아트_작업규칙.md, Underwrite_8방향_애니메이션_자동생성툴.md, Underwrite_대화용_상반신_표정_제작기준.md, Documents/Shared/Underwrite_기획서_v0.6.md, Documents/Claude/Underwrite_상장흐름_설계안.md, AGENTS.md 원문을 읽었다. 상장흐름 §1-2의 지하시장을 모르는 외지 용병 설정을 적용했다.
초상 기준문서의 과거 v1·v2 작업 범위인 Assets 제외는 이번 요청에 적용하지 않고 명시된 Assets 신규 배치를 수행했다. 기존 용사 파일의 sella·brann 이름과 이번 용병의 동일 이름을 같은 인물로 취급하지 않는다.
문서 갱신: Documents/Codex/Underwrite_대화용_상반신_표정_제작기준.md에 face_band_ 이름 구분과 이번 검증 결과를 추가했다. 상위 아트 규칙·기획서는 변경하지 않았다.

## 남은 작업

사용자 시각 승인. Claude Code에서 PortraitSet과 종목 연결. 새 입력·전환 조건·Unity 에셋 연결은 이번 범위가 아니다.

재현 최종 확인: 동결 평상 원화와 생성 표정 원본·정렬·얼굴마스크로 재합성한20장의 SHA-256이 delivery 및 Assets 납품본과 모두 일치했다. 일괄 재현은 도구 시간 제한 때문에 인물 단위로 나눠 확인했다. 결과는 rebuild_verification.json. 재현 함수는 rebuild.mjs의 rebuildPerson(person)이며 Assets를 쓰지 않는다.
