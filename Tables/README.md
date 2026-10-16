# Underwrite 데이터 테이블

코드에 박혀 있던 문자열과 수치를 뽑아 둔 것이다. 2026년 10월 16일 기준 스냅샷이다.

## 쓰임

1. 이 CSV를 구글 시트로 올려 지금 값이 무엇인지 한눈에 본다.
2. 같은 CSV를 ScriptableObject를 만들 때의 씨앗으로 쓴다.

**원본은 ScriptableObject다.** 시트는 내보내기 결과로 둔다. 시트를 원본으로 삼으면 Unity에서 값을 고칠 때마다 import/export를 왕복해야 하고, 둘 중 어느 쪽이 맞는지 알 수 없게 된다.

## 파일

| 파일 | 내용 | 행 |
|---|---|---|
| `UIText.csv` | 화면에 나가는 글자 | 143 |
| `MarketTuning.csv` | 시장 수치 상수 | 41 |
| `Stocks.csv` | 종목 정의 | 3 |
| `Events.csv` | 사건 일정 | 13 |
| `Personality.csv` | 성격별 변동성 배율 | 4 |
| `Sentiment.csv` | 분위기별 흐름·변동성 | 4 |

인코딩은 UTF-8 BOM이다. 구글 시트와 엑셀 둘 다 한글이 깨지지 않는다.

## 열 설명

### UIText.csv

- `Kind` — `UI`는 화면에 나가는 글자, `로그`는 개발용, `에디터`는 인스펙터 라벨이다. **테이블로 옮길 대상은 `UI`뿐이다.**
- `HasFormat` — `O`면 `{0}주 매수`처럼 값이 끼어드는 문구다. 그대로 옮길 수 없고 서식 문자열로 바꿔야 한다. 25건이다.
- `Where` — UXML은 요소 이름, C#은 줄 번호다. 줄 번호는 코드가 바뀌면 어긋난다. 찾아가는 실마리로만 쓴다.

### MarketTuning.csv

- `Owner` — **`튜닝`(29건)만 테이블로 뺀다.** `코드 고정`(12건)은 구조와 안전장치라 코드에 남긴다. `PRICE_FLOOR`가 0이 되거나 `BOOK_LEVELS`가 음수가 되면 시뮬레이션이 깨지는데, 시트에는 그걸 막을 방법이 없다.
- `Note` — 코드의 XML 주석을 그대로 옮긴 것이다. 왜 그 값인지가 적혀 있다.

### Events.csv

- `Slot` — 정보 타임 번호다. 0·1이 오전장 두 번, 2·3이 오후장 두 번이다. 기획서 §11 참고.
- `ExpectationDelta` — 시장 기대를 움직인다. 소문·보고 단계에서 쓴다.
- `ValueDelta` — 실제 내재가치를 움직인다. 결과 단계에서만 쓴다.
- `Visibility` — `Public`은 시장이 함께 알아 가격이 움직이고, `PlayerOnly`는 나만 알아 가격이 안 움직인다.

## 자산으로 옮기는 법

Unity 메뉴 **Underwrite → 테이블 가져오기**를 누르면 이 폴더의 CSV가 `Assets/Data/*.asset`으로 들어간다.

방향은 한쪽이다. **표가 자산을 덮어쓴다.** 되돌리는 내보내기는 만들지 않았다. 양방향을 미리 만들면 어느 쪽이 맞는지 알 수 없게 된다.

구글 시트에서 고쳤으면 `파일 → 다운로드 → CSV`로 받아 이 폴더에 같은 이름으로 덮어쓰고 메뉴를 누른다.

## 코드와 이어진 자리

| 자산 | 쓰이는 곳 |
|---|---|
| `MarketTuningData` | `Constants`의 튜닝 값 29개를 장 열기 전에 덮어쓴다 |
| `PersonalityTable` | `Constants.VOLATILITY_*` |
| `SentimentTable` | `Constants.SENTIMENT_*` |
| `StockTable` | `MarketScreenController.BuildStocks()` |
| `EventTable` | `MarketScreenController.BuildEvents()` |
| `UITextTable` | **아직 안 이어졌다.** 표만 채워 둔 상태다 |

표를 물리지 않으면 `PrototypeScenario`의 코드에 박힌 값으로 돌아간다. 화면이 비는 대신 예전처럼 돌아가므로, 표가 안 먹었을 때 바로 알아채기 어렵다는 점은 염두에 둔다.

## 다시 뽑는 법

코드 → 표 방향의 추출 스크립트는 일회성이라 저장하지 않았다. 자산이 원본이 된 뒤로는 쓸 일이 없다.
