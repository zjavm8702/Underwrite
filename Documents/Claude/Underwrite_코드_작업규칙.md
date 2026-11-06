# Underwrite 코드 작업 규칙

작성: 2026-10-16  
버전: 1.0 (확정)  
적용 범위: `Assets/Scripts`(런타임), `Assets/Editor`(에디터 도구)  
언어·엔진: C# / Unity 6.5 (6000.5.10f1)

이 문서는 Underwrite의 C# 코드 규칙을 정리한 것이다. 프로젝트 초기의 스크립트 5개는 시험용 코드이므로 기준이 아니라 참고 자료로만 다룬다.

사용자가 명시한 추가 규칙 네 가지는 §4(LINQ), §5(중괄호), §7(Constants), §7-3(상수 범위)에 반영했다.

아트·애니메이션은 `../Codex/Underwrite_아트_작업규칙.md`, Codex의 작업 범위는 루트 `AGENTS.md`, Claude Code의 작업 방식은 루트 `CLAUDE.md`가 상위 문서다.

### v1.0 — 전 항목 확정

남아 있던 결정을 모두 마쳤다. `.asmdef` 도입과 `using` 정렬은 위임받아 결정했고 근거를 §12에 적었다. 나머지는 초안 제안대로 확정했다.

### 확정된 항목 (v0.4)

- **`switch` 식과 분기용 식 본문 멤버(`=>`)를 쓰지 않는다.** 중괄호로 나눈 `switch` 문만 쓴다. §5 참고.

- **별도 문자열 테이블과 조회 함수를 쓰지 않는다.** 키를 포함한 모든 문자열 상수는 `Constants.cs`에서 관리한다. §7-2 참고.

### 확정된 항목 (v0.3)

- **언어 정책을 §9로 확정했다.** 주석만 한국어, 사용자에게 보여줄 문자열은 문자열 테이블 키, 예외 메시지는 영어, 로그는 채널에 따라 나뉜다. v0.2의 "주석·UI 문자열·예외 메시지는 한국어" 서술은 틀렸으므로 폐기한다.
- **`UnderwriteLog`를 도입했다.** `Assets/Scripts/Utility/UnderwriteLog.cs`로 추가했다. §10 참고.
- **`sealed` 적용 기준을 확정했다.** 상속될 가능성이 없는 단일 클래스에 붙인다. §8 참고.

---

## 1. 폴더와 네임스페이스

네임스페이스는 `Underwrite.<폴더명>`으로 폴더 구조와 1:1로 맞춘다.

폴더는 아래 분류를 기준으로 삼고, 필요한 영역만 만든다.

| 폴더 | 담는 것 | Underwrite 예상 |
|---|---|---|
| `Global/` | `Constants.cs`, 문자열 테이블, 오브젝트 풀, 상태 기계 | 공용 |
| `Enumeration/` | enum만 모음. `XxxEnum.cs` | `MarketEnum.cs`, `CharacterEnum.cs` |
| `Data/` | 순수 데이터 클래스 (로직 없음) | `AdventurerData.cs`, `OrderData.cs` |
| `Interface/` | `I` 접두사 인터페이스 | `IManager`, `IMarketTick` |
| `Manager/` | 생명주기를 가진 매니저 | `GameManager`, `MarketManager` |
| `Utility/` | 정적 헬퍼, 로그 래퍼 | `UnderwriteLog.cs` |
| `Extension/` | 확장 메서드 | `EnumExtension.cs` |
| `UI/` | 화면·팝업 | `UI_TradeScreen.cs` |
| `Character/` | 캐릭터·연출 | 기존 `Characters/` 정리 |

에디터 전용 코드는 반드시 `Assets/Editor` 아래에 둔다. 런타임 폴더에 `UnityEditor` 참조 코드를 두면 빌드가 깨진다.

```csharp
// 좋음 — 폴더와 네임스페이스가 일치한다
// Assets/Scripts/Manager/MarketManager.cs
namespace Underwrite.Manager { }
```

## 2. 타입 이름 규칙

접두·접미 규칙은 아래와 같다.

| 종류 | 규칙 | 예 |
|---|---|---|
| 인터페이스 | `I` + PascalCase | `IManager`, `IMarketTick` |
| 기반 클래스 | `Base` + 역할 | `BaseUI`, `BaseManager` |
| 매니저 | `<영역>Manager` | `GameManager`, `MarketManager` |
| 분할 처리기 | `<영역><역할>Handler` | `VillageMenuHandler`, `VillageQuestHandler` |
| UI 화면 | `UI_` + PascalCase | `UI_Dialogue`, `UI_Tutorial` |
| enum 파일 | `<영역>Enum.cs`, `Enumeration/`에 모음 | `MarketEnum.cs`, `StoryEnum.cs` |
| enum 값 | `<분류>_<이름>` | `Stage_Idle`, `Stage_Walking` |
| 에디터 창 | `<기능>Window` | `SpriteBatchTrimWindow` |

매니저나 화면이 커지면 `Handler`로 쪼갠다. 책임 단위로 나누고, 본체는 조립만 한다.

## 3. 멤버 이름 규칙

| 대상 | 규칙 | 예 |
|---|---|---|
| 클래스·메서드·프로퍼티 | PascalCase | `TryGetInstance`, `Instance` |
| **private·protected 필드** | **PascalCase, 밑줄 없음** | `Body`, `IsInitialized` |
| 프로퍼티의 백킹 필드 | camelCase (프로퍼티와 구별) | `instance` ↔ `Instance` |
| 이벤트 핸들러 필드 | `on` + PascalCase + `EventHandler` | `onOrderFilledEventHandler` |
| 상수 (`const`·`static readonly`) | **UPPER_SNAKE_CASE** | `MOVE_INPUT_DEAD_ZONE`, `TAG_PROTAGONIST` |
| 지역변수·매개변수 | camelCase | `orderedStocks`, `bestPrice` |
| 불리언 | `Is`·`Has`·`Can` 접두사 | `IsOpen`, `HasMarket` |

밑줄 접두사(`_name`)는 쓰지 않는다.

```csharp
// 좋음
private static MarketManager instance;            // Instance 프로퍼티의 백킹 필드
private readonly List<OrderBook> Books = new();
private Transform Root;
private bool IsInitialized;

// 나쁨 — 타입 약어를 이름에 붙였다 (현재 PlayerCharacterInput.cs)
private Rigidbody2D CharRigBody2D = null;

// 좋음 — 역할로 읽힌다
private Rigidbody2D Body;
```

참조형 필드에 `= null`을 직접 쓰지 않는다. 기본값이다.

## 4. LINQ 사용 제한

**LINQ는 호출 빈도가 낮은 지점에만 허용한다.**

허용:

- 초기화·로드 (`Awake`, `Initialize`, Addressable 로드)
- 라운드·장면 전환처럼 드물게 일어나는 전환 시점
- 데이터 테이블 검증·집계
- 에디터 도구 (프레임 예산이 없다)

금지:

- `Update`, `FixedUpdate`, `LateUpdate`, 코루틴의 매 프레임 구간
- 캐릭터·종목처럼 개체 수에 비례해 매 틱 도는 루프
- 시장 가격 갱신처럼 초당 여러 번 호출되는 경로

금지 구간에서는 `for`·`foreach`와 미리 할당한 리스트를 쓴다. LINQ는 반복마다 열거자와 람다 클로저를 할당해 GC를 유발한다.

```csharp
// 좋음 — 초기화 때 한 번만 돈다
var orderedStocks = Stocks
    .OrderBy(stock => stock.Price)
    .ToArray();

// 나쁨 — 매 프레임 LINQ. 열거자와 람다가 매번 할당된다
private void Update()
{
    var nearest = Adventurers.Where(a => a.IsAlive).OrderBy(a => a.Distance).FirstOrDefault();
}

// 좋음 — 같은 일을 할당 없이
private void Update()
{
    Adventurer nearest = null;
    float best = float.MaxValue;

    for (int i = 0; i < Adventurers.Count; i++)
    {
        if (Adventurers[i].IsAlive == false)
        {
            continue;
        }

        if (Adventurers[i].Distance < best)
        {
            best = Adventurers[i].Distance;
            nearest = Adventurers[i];
        }
    }
}
```

판단이 애매하면 LINQ를 쓰지 않는 쪽을 고른다. 쓸 때는 왜 저빈도인지 한 줄 주석을 남긴다.

## 5. 중괄호와 분기

**`if`·`else`·`switch`·`for`·`foreach`·`while`은 본문 길이와 무관하게 항상 중괄호로 감싼다.** 한 줄짜리 본문도 예외가 없다.

중괄호를 생략한 한 줄 분기는 쓰지 않는다. 조건과 본문이 한 줄에 붙으면 디버거 중단점을 분기별로 걸 수 없고, 본문을 한 줄 추가할 때 조건 밖으로 빠지는 사고가 난다.

```csharp
// 나쁨 — Underwrite에서는 금지
if(i==null)return;
if(Initialized)return;Result=Wire(Config);Initialized=true;

// 좋음
if (i == null)
{
    return;
}

if (Initialized == true)
{
    return;
}

Result = Wire(Config);
Initialized = true;
```

- 중괄호는 Allman(여는 괄호를 다음 줄에). 들여쓰기는 공백 4칸.
- 삼항 연산자를 중첩하지 않는다. 분기가 셋 이상이면 `switch` **문**으로 펼친다.
- **`switch` 식(`=> value switch { ... }`)과 식 본문 멤버(`=>`)를 분기에 쓰지 않는다.** 중괄호로 나눈 `switch` 문만 쓴다.
- `switch`의 각 `case`는 `break`·`return`·`throw`로 끝낸다. 의도한 fall-through에는 주석을 남긴다.
- **분기 대상은 enum으로 명시한다.** `int`·`string` 리터럴로 상태를 구분하지 않는다. 매개변수와 필드의 타입 자체가 어떤 값이 올 수 있는지 알려주어야 한다.

```csharp
// 나쁨 — int로 분기한다. 1·2·4가 무엇인지 타입이 알려주지 않는다
private static ProtagonistMotion MotionOf(int stage)
{
    switch (stage)
    {
        case 1:
        {
            return ProtagonistMotion.Walking;
        }
        ...
    }
}

// 나쁨 — switch 식. 쓰지 않는다
private static ProtagonistMotion MotionOf(PreviewStage stage) => stage switch
{
    PreviewStage.Stage_Walking => ProtagonistMotion.Walking,
    PreviewStage.Stage_CheerHappy or PreviewStage.Stage_CheerAngry => ProtagonistMotion.Cheer,
    _ => ProtagonistMotion.Idle,
};

// 좋음 — enum으로 분기하고 중괄호로 나눈 switch 문
private static ProtagonistMotion MotionOf(PreviewStage stage)
{
    switch (stage)
    {
        case PreviewStage.Stage_Walking:
        {
            return ProtagonistMotion.Walking;
        }
        case PreviewStage.Stage_CheerHappy:
        case PreviewStage.Stage_CheerAngry:
        {
            return ProtagonistMotion.Cheer;
        }
        default:
        {
            return ProtagonistMotion.Idle;
        }
    }
}
```

분기에 쓰는 enum은 §1·§2에 따라 `Enumeration/` 폴더의 `<영역>Enum.cs`에 두고, 값은 `<분류>_<이름>`으로 적는다.

```csharp
// Assets/Scripts/Enumeration/CharacterEnum.cs
namespace Underwrite.Enumeration
{
    public enum PreviewStage
    {
        Stage_Idle,
        Stage_Walking,
        Stage_CheerHappy,
        Stage_CheerAngry
    }
}
```

## 6. 불리언 비교

`== true`·`== false`를 명시한다. `!` 한 글자가 긴 조건식에서 눈에 안 띄는 문제를 피한다.

```csharp
// 좋음
if (IsQuitting == true)
if (Market.IsPanelOpen == false)
if (Books.TryGetValue(id, out var book) == true)

// 쓰지 않음
if (!Market.IsPanelOpen)
```

## 7. 상수와 문자열 관리

### 7-1. Constants.cs 구조

문자열·숫자 상수는 `Assets/Scripts/Global/Constants.cs`에서 관리한다. 문자열과 숫자를 두 클래스로 나눈다.

```csharp
namespace Underwrite.Global
{
    // 문자열 전용. partial이므로 자동 생성 키 파일이 같은 클래스를 확장할 수 있다
    public static partial class StringConstants
    {
        public const string TAG_PROTAGONIST = "Protagonist";
        public static readonly string LAYERMASK_VILLAGE_BLOCK = "VillageBlock";

        public const string ANIM_PARAM_DIRECTION_X = "DirectionX";
        public const string ANIM_PARAM_DIRECTION_Y = "DirectionY";
        public const string ANIM_PARAM_SPEED = "Speed";

        public const string ANIM_STATE_IDLE = "Idle";
        public const string ANIM_STATE_WALKING = "Walking";
        public const string ANIM_STATE_CHEER = "Cheer";
    }

    // 숫자·벡터 전용
    public static class Constants
    {
        public const float MOVE_INPUT_DEAD_ZONE = 0.001f;
        public static readonly float PROTAGONIST_MOVE_SPEED = 3f;
        public static readonly Vector2 DIRECTION_SOUTH = new Vector2(0f, -1f);
    }
}
```

- 이름은 **UPPER_SNAKE_CASE**.
- 도메인 접두사로 묶는다. `TAG_`, `LAYERMASK_`, `ANIM_PARAM_`, `ANIM_STATE_`, `MOVE_`, `MARKET_`.
- 컴파일 시점에 고정되는 값은 `const`, 그 외는 `static readonly`.
- `Vector2` 같은 참조·구조체 값은 `static readonly`만 가능하다.

### 7-2. 모든 문자열은 Constants.cs에서 가져온다

**별도의 문자열 테이블과 조회 함수를 두지 않는다.** 키를 포함해 문자열 상수로 다룰 모든 것은 `Constants.cs`의 `StringConstants`에 모은다.

따라서 화면에 보이는 문자열도 코드에 리터럴로 박지 않고 `StringConstants`의 상수를 쓴다. 조회 한 단계가 없으므로 상수가 곧 표시될 문자열이다.

```csharp
// 좋음 — 사용자에게 보이는 문자열도 Constants.cs의 상수를 쓴다
HeadlineLabel.text = StringConstants.TEXT_MARKET_HEADLINE_HALT;
NameLabel.text = StringConstants.TEXT_HERO_NAME_HUMAN_FEMALE;

// 나쁨 — 리터럴을 코드에 박는다
HeadlineLabel.text = "지하시장이 휴장했습니다.";
```

표시용 문자열은 `TEXT_` 접두사로 묶고 그 뒤에 영역을 붙인다. 양이 늘어나면 영역별 `partial` 파일로 나눈다. 클래스는 하나로 유지한다.

```csharp
// Assets/Scripts/Global/Constants.cs — 태그·파라미터 등 시스템 문자열
public static partial class StringConstants
{
    public const string TAG_PROTAGONIST = "Protagonist";
    public const string ANIM_PARAM_DIRECTION_X = "DirectionX";
}

// Assets/Scripts/Global/Constants.Text.cs — 표시용 문자열
public static partial class StringConstants
{
    public const string TEXT_MARKET_HEADLINE_HALT = "지하시장이 휴장했습니다.";
    public const string TEXT_MARKET_ORDER_FILLED = "주문이 체결되었습니다.";
}
```

표시용 문자열이 코드에 그대로 들어가므로, 나중에 다국어가 필요해지면 이 상수들을 조회 계층으로 한 번 더 감싸야 한다. 지금은 그 비용을 지불하지 않는 쪽을 선택한 것이다.

### 7-3. 상수를 어디에 둘지

- **`static` 전역 상수는 한 클래스에서만 쓰더라도 `Constants.cs`로 옮겨 쓴다.** 상수가 흩어지면 같은 값이 여러 이름으로 중복된다.
- **그 밖의 값은 해당 `.cs` 안에 둔다.** 지역변수, 인스턴스 필드, 인스펙터에서 조정하는 `[SerializeField]` 값은 옮기지 않는다.

```csharp
// 좋음 — 전역 상수는 Constants.cs에서 가져온다
if (MoveInput.sqrMagnitude < Constants.MOVE_INPUT_DEAD_ZONE)

// 좋음 — 인스펙터에서 조정할 값은 해당 클래스에 남긴다
[SerializeField] private float MoveSpeed = 3f;

// 나쁨 — 전역 성격의 상수를 클래스 안에 숨겨 두었다
private const string AnimStateCheer = "Cheer";
```

판단 기준: **다른 클래스가 이 값을 알아야 할 가능성이 있거나, 외부 에셋(Animator·태그·레이어·경로)과 묶인 계약이면 `Constants.cs`로 보낸다.**

### 7-4. Animator 파라미터와 상태 이름

`DirectionX`·`DirectionY`·`Speed`와 상태 `Idle`·`Walking`·`Cheer`는 아트 문서와 Animator Controller에 묶인 계약이다. 이름은 `StringConstants`에, 실행용 해시는 사용하는 클래스에 캐싱한다.

```csharp
// 좋음
private static readonly int DirectionXHash = Animator.StringToHash(StringConstants.ANIM_PARAM_DIRECTION_X);
private static readonly int CheerStateHash = Animator.StringToHash(StringConstants.ANIM_STATE_CHEER);

Animator.SetFloat(DirectionXHash, Mathf.Cos(snappedAngle));

// 나쁨 — 해시와 리터럴을 섞어 쓴다 (현재 PlayerCharacterInput.cs)
CharAnimator.SetFloat("Speed", CharRigBody2D.linearVelocity.magnitude);
```

상태 이름을 `enum.ToString()`으로 만들지 않는다. 이름이 어긋나면 `Animator.Play`가 조용히 실패하고, 매 프레임 문자열도 생성된다.

방향 코드 순서는 `S, SE, E, NE, N, NW, W, SW`로 고정하고 `DirectionalAnimationBuilder.Codes`를 단일 출처로 삼는다. 대각선 좌표는 정규화한 `±0.7071068`이다.

## 8. 클래스 선언과 접근 수정자

- `private`를 생략하지 않고 명시한다.
- **상속될 가능성이 없는 단일 클래스에 `sealed`를 붙인다.** `Base*`·매니저처럼 상속 지점으로 설계한 클래스는 열어 둔다. 지금 자식이 없어도 파생을 의도한 자리면 붙이지 않는다.
- `MonoBehaviour`·`EditorWindow`는 파일 이름과 클래스 이름을 같게 하고 한 파일에 하나만 둔다.
- 데이터 묶음은 `Data/` 아래 순수 클래스로 분리하고 로직을 넣지 않는다.
- `MonoBehaviour`의 public 필드는 지양한다. 인스펙터 노출은 `[SerializeField] private`, 외부 변경은 메서드로 받는다.
- 필드 하나에 선언 하나. 한 줄에 여러 필드를 묶지 않는다.

```csharp
// 나쁨 — 접근 수정자 생략, 한 줄에 네 개
public SpriteResolver eyes, mouth, rightHand, leftHand;

// 좋음
[SerializeField] private SpriteResolver Eyes;
[SerializeField] private SpriteResolver Mouth;
[SerializeField] private SpriteResolver RightHand;
[SerializeField] private SpriteResolver LeftHand;
```

## 9. 언어 정책과 문서화

문자열의 **독자가 누구인지**로 언어를 정한다.

| 대상 | 언어 | 비고 |
|---|---|---|
| 주석, `/// <summary>` | **한국어** | 읽는 사람이 개발자다 |
| 게임 화면·대사·뉴스 등 사용자에게 보이는 문자열 | **한국어** | 리터럴로 박지 않고 `Constants.cs`의 상수를 쓴다 (§7-2) |
| 예외 메시지 (`throw`) | **영어** | 스택과 함께 기록·수집되는 개발 정보다 |
| `UnderwriteLog` 로그 | **한국어** | 개발 과정 로그와 빌드에 노출해도 되는 로그 모두 |
| `Debug.Log` 직접 호출 | **영어** | |

새 로그는 §10에 따라 `UnderwriteLog`를 기본으로 쓰므로 대부분 한국어가 된다. `Debug.Log`를 직접 부르는 경우에만 영어다.

```csharp
// 좋음 — 주석은 한국어
// 입력이 없어도 마지막으로 바라본 방향을 유지한다.

// 좋음 — 사용자에게 보이는 문자열은 Constants.cs의 상수를 쓴다
HeadlineLabel.text = StringConstants.TEXT_MARKET_HEADLINE_HALT;

// 좋음 — 예외 메시지는 영어
throw new InvalidOperationException("Market tick interval must be greater than zero.");

// 좋음 — UnderwriteLog는 한국어
UnderwriteLog.Warning("SpriteResolver가 비어 있어 표정 교체를 건너뜁니다.", context: this);

// 좋음 — Debug.Log를 직접 쓸 때는 영어
UnityEngine.Debug.Log("Addressable catalog reload requested.");

// 나쁨 — 사용자에게 보이는 문자열을 코드에 박았다
HeadlineLabel.text = "지하시장이 휴장했습니다.";

// 나쁨 — 예외 메시지를 한국어로 썼다
throw new InvalidOperationException("시장 갱신 주기는 0보다 커야 합니다.");
```

### 문서화

- `public` 타입과 메서드에는 `/// <summary>`를 한국어로 단다.
- 주석은 **왜**를 쓴다. 코드를 다시 설명하지 않는다.
- 호출하는 쪽이 지켜야 할 제약은 반드시 `<summary>`에 적는다.

```csharp
/// <summary>
/// 새 객체를 생성하지 않고 현재 실행 중인 매니저가 있는지 확인합니다.
/// </summary>
public static bool TryGetInstance(out GameManager manager)

/// <summary>
/// 에디터에서는 모든 로그, 플레이어에서는 UNDERWRITE_LOGS 심볼과 includeInBuild가 모두 켜진 로그를 출력합니다.
/// 심볼 없는 플레이어에서는 호출과 인자 평가가 생략되므로 인자에 필수 동작을 넣지 마세요.
/// </summary>
```

### 에디터 도구의 검증 메시지 — 한국어 (확정)

8방향 생성기처럼 에디터 창이 개발자에게 직접 보여주는 검증 문구는 사용자 UI도, 수집되는 예외도 아닌 중간 성격이다. **창에 그대로 표시하는 검증 메시지는 UI 문자열로 취급해 한국어를 유지한다.** 영어로 쓰는 것은 런타임 예외뿐이다. 이 메시지는 `Constants.cs`로 옮기지 않고 해당 도구 파일에 둔다 — 그 도구 외에는 쓰이지 않고 외부 에셋과 묶인 계약도 아니다(§7-3).

```csharp
// 좋음 — 에디터 창에 그대로 보여주는 검증 문구
throw new InvalidOperationException("Resolver 경로는 Animator 기준 상대 경로입니다. 같은 오브젝트라면 비워두세요.");
```

## 10. 런타임 코드

- `Update`에서 `GetComponent`·`Find`·`new`·문자열 생성·LINQ를 하지 않는다. 참조는 `Awake`에서 캐싱한다.
- 입력 반영과 애니메이션 파라미터는 `Update`, 물리는 `FixedUpdate`.
- 필드와 지역변수의 이름을 겹치게 선언하지 않는다. 섀도잉은 컴파일되지만 값이 반영되지 않는다.
- 직렬화 참조는 `Awake`에서 한 번 검증하고 비었으면 경고를 남긴다.
- **로그는 `UnderwriteLog`로 남긴다.** `Assets/Scripts/Utility/UnderwriteLog.cs`에 도입했다. `Debug.Log`를 직접 부르지 않는다.
  - `UnderwriteLog.Info` / `Warning` / `Error`를 쓴다. 메시지는 한국어(§9).
  - 호출 지점의 파일·줄·메서드가 자동으로 붙는다. 직접 적지 않는다.
  - 빌드에 남겨야 하는 로그만 `includeInBuild: true`를 준다. 기본값은 에디터 전용이다.
  - 빌드에서 로그를 켜려면 `UNDERWRITE_LOGS` 심볼을 정의한다. 심볼이 없으면 `[Conditional]`로 **호출과 인자 평가가 모두 제거**되므로, 인자에 게임 로직을 넣지 않는다.
  - 문자열을 만드는 비용이 큰 로그는 `UnderwriteLog.IsEnabled()`로 먼저 확인한다.
- 싱글턴은 종료 중 재생성을 막고(`IsQuitting`), 생성 없이 조회하는 `TryGetInstance`를 함께 제공한다.

```csharp
// 나쁨 — 지역변수가 필드를 가려 입력이 버려진다 (현재 PlayerCharacterInput.cs)
public void OnMove(InputValue value)
{
    var MoveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
}

// 좋음
public void OnMove(InputValue value)
{
    MoveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
}
```

```csharp
// 나쁨 — 인자 안에서 상태를 바꾸면 빌드에서 사라진다
UnderwriteLog.Info($"주문 체결: {Orders.Dequeue()}");

// 좋음 — 비싼 문자열은 먼저 확인한다
if (UnderwriteLog.IsEnabled() == true)
{
    UnderwriteLog.Info($"주문 체결: {BuildOrderSummary()}");
}
```

## 11. 에디터 도구 코드

- `.meta`, `.anim`, `.asset`, `.prefab`, `.unity`, `.spriteLib`, `.controller`를 **텍스트로 편집하지 않는다.** 변경은 Unity Editor API로만 한다.
- **읽기와 쓰기를 분리한다.** 검사·미리보기는 `Analyze`(에셋을 바꾸지 않음), 적용은 `Build`. 기존 8방향 도구 구조를 유지한다.
- 덮어쓰기는 명시적 플래그가 켜졌을 때만 허용한다. 기본값은 차단이다(`allowUpdate` 패턴).
- 파괴적 작업 전에 원본을 복사하고, 실패하면 새 에셋을 정리하고 사본으로 복구한다.
- 작업 범위 밖의 카테고리·상태·전환 조건·GUID를 건드리지 않는다.
- 메뉴 경로는 `Tools/Underwrite/<분류>/<도구>`로 통일한다.
- 창에 그대로 표시하는 검증 메시지에는 원인과 **해결 방법**을 함께 넣는다. 언어는 §9의 에디터 예외 항목을 따른다.
- 에디터 코드는 프레임 예산이 없으므로 LINQ를 자유롭게 쓴다(§4).

```csharp
[MenuItem("Tools/Underwrite/Animation/8 Direction Builder")]
[MenuItem("Tools/Underwrite/Sprites/Batch Trim")]

throw new InvalidOperationException("Resolver 경로는 Animator 기준 상대 경로입니다. 같은 오브젝트라면 비워두세요.");
```

## 12. 결정 현황

### 확정

| 항목 | 결정 |
|---|---|
| private 필드 이름 | PascalCase, 밑줄 없음 (§3) |
| LINQ | 저빈도 호출에만 허용 (§4) |
| 중괄호 | `if`·`switch` 등 모든 분기에 필수. `switch` 식과 분기용 `=>` 금지 (§5) |
| 불리언 비교 | `== true` / `== false` 명시 (§6) |
| 상수 관리 | `Global/Constants.cs`의 `StringConstants` + `Constants`, UPPER_SNAKE_CASE (§7) |
| 문자열 테이블 | 쓰지 않는다. 모든 문자열 상수는 `Constants.cs` (§7-2) |
| 전역 상수 위치 | `static` 전역 상수는 한 클래스 전용이라도 `Constants.cs`로, 그 외는 해당 `.cs` (§7-3) |
| `sealed` | 상속될 가능성이 없는 단일 클래스에 붙인다 (§8) |
| 언어 정책 | 주석 한국어 / 사용자 문자열은 테이블 키 / 예외 영어 / `UnderwriteLog` 한국어 / `Debug.Log` 영어 (§9) |
| 로그 | `UnderwriteLog` 도입 — `Assets/Scripts/Utility/UnderwriteLog.cs` (§10) |
| 어셈블리 정의 | `Underwrite.Runtime` + `Underwrite.Editor` 두 개 도입 |
| `using` 정렬 | 그룹 분리 없이 단일 알파벳 순. 별칭 지시문 금지 |
| `Object` 등 충돌 이름 | `UnityEngine.Object`처럼 전체 이름으로 명시 |
| 에디터 검증 메시지 | 한국어 유지 (§9) |
| 테스트 | 도입. 에디터 도구의 판정 로직 우선 |
| 줄 길이 | 120자 권장, 강제 안 함 |
| 파일 머리 주석 | 쓰지 않는다 |

### 어셈블리 정의 (`.asmdef`) — 도입한다

`Assets/Scripts`와 `Assets/Editor`에 각각 어셈블리 정의를 둔다.

| 파일 | 어셈블리 이름 | 플랫폼 | 참조 |
|---|---|---|---|
| `Assets/Scripts/Underwrite.Runtime.asmdef` | `Underwrite.Runtime` | 전체 | `Unity.InputSystem`, `Unity.2D.Animation.Runtime` |
| `Assets/Editor/Underwrite.Editor.asmdef` | `Underwrite.Editor` | **Editor 전용** | `Underwrite.Runtime`, `Unity.2D.Animation.Runtime`, `Unity.2D.Animation.Editor`, `Unity.2D.Sprite.Editor` |

도입 이유는 세 가지다.

1. **테스트의 전제 조건이다.** `.asmdef`로 정의한 어셈블리는 Unity의 미리 정의된 어셈블리(`Assembly-CSharp`)를 참조할 수 없다. 의존 방향이 반대로만 열려 있다. 따라서 Test Assembly에서 본 코드를 참조하려면 본 코드가 먼저 `.asmdef` 안에 있어야 한다. 테스트를 도입하기로 했으므로 이 항목은 선택이 아니다. `com.unity.test-framework` 1.7.0은 이미 설치되어 있다.
2. **에디터 코드가 런타임에 섞이는 사고를 컴파일 단계에서 막는다.** `Underwrite.Runtime`은 `UnityEditor`를 참조하지 않으므로, 실수로 `Assets/Scripts`에 에디터 코드를 넣으면 그 자리에서 컴파일 에러가 난다. 폴더 규칙(§1)만으로는 에디터에서 멀쩡히 돌다가 빌드할 때만 깨진다.
3. **컴파일 시간이 줄어든다.** 바꾼 어셈블리만 다시 컴파일한다.

스크립트가 적은 초기에 나누는 것이 가장 싸다. 파일이 많아진 뒤에 쪼개려면 순환 참조를 풀어야 하므로 비용이 크다.

주의할 점:

- 참조는 **어셈블리 이름**으로 적는다. 위 표의 이름은 이 프로젝트의 `Library/PackageCache`에서 확인한 실제 값이다.
- 새 패키지 API를 쓰기 시작하면 해당 어셈블리를 참조에 추가해야 한다. 추가하지 않으면 `CS0246` 타입 미해결로 바로 드러난다.
- `Underwrite.Editor`는 `includePlatforms`를 `["Editor"]`로 둔다. 이것이 빌드에서 에디터 코드를 제외하는 장치다.
- 런타임이 에디터를 참조하는 방향은 만들지 않는다.

### `using` 정렬 — 단일 알파벳 순

**빈 줄로 그룹을 나누지 않고 전체를 알파벳 한 줄로 정렬한다.** 별칭 지시문(`using X = Y;`)은 쓰지 않는다.

```csharp
// 좋음 — 현재 DirectionalAnimationBuilder.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.U2D.Animation;

// 나쁨 — 나중에 추가한 using을 맨 위에 끼워 넣었다
using Underwrite.Global;
using System;
using System.Collections.Generic;
using Underwrite.Data;
```

v0.4 초안은 `System` → 서드파티 → `Underwrite.*` → `Unity*` 네 그룹으로 나누자고 제안했으나 **철회한다.** 이유는 두 가지다.

1. using 블록 다수가 이미 단순 알파벳 순이다. 네임스페이스가 `System`·`Underwrite`·`Unity*`로 시작해 알파벳 순이 자연히 `System` → 프로젝트 → `Unity` 순서를 만든다. 그룹 규칙을 따로 둘 이득이 없다.
2. 그룹 분리는 사람이 판단해야 하므로 어긋난다. 단일 정렬은 IDE 명령 하나로 복구된다 — Visual Studio의 `Remove and Sort Usings`, Rider의 `Optimize Imports`.

`.editorconfig`에 `dotnet_sort_system_directives_first = false`와 `dotnet_separate_import_directive_groups = false`로 강제한다.

#### 별칭 지시문을 쓰지 않는다 — 충돌하는 이름은 전체 이름으로

**`using X = Y;` 형태의 별칭 지시문은 쓰지 않는다.** 파일 맨 위의 한 줄이 그 아래 수백 줄에서 이름의 의미를 바꾸므로, 읽는 사람이 `Object`를 보고 어느 `Object`인지 using 블록까지 거슬러 올라가야 한다.

`using System;`과 `using UnityEngine;`을 함께 쓰면 `Object`가 `System.Object`와 `UnityEngine.Object`로 모호해져 `CS0104`가 난다. 이때 **전체 이름 `UnityEngine.Object`로 명시한다.**

`using System;`이 없다는 사실에 기대어 맨 `Object`를 쓰는 것도 금지한다. 나중에 누가 `using System;`을 추가하면 그 파일이 깨진다.

```csharp
// 좋음 — 전체 이름으로 명시한다
public static void Info(string message, bool includeInBuild = false, UnityEngine.Object context = null)

static void CheckAssetType<T>(string path) where T : UnityEngine.Object

UnityEngine.Object.DestroyImmediate(temp);

// 나쁨 — 별칭 지시문
using UnityEngine;
using Object = UnityEngine.Object;

public static void Info(string message, Object context = null)

// 나쁨 — using System;이 없는 것에 기대고 있다. 추가되는 순간 CS0104
using UnityEngine;

public static void Info(string message, Object context = null)
```

같은 규칙이 `Random`(`System.Random` ↔ `UnityEngine.Random`), `Debug`(`System.Diagnostics.Debug` ↔ `UnityEngine.Debug`)에도 적용된다. 로그는 §10에 따라 `UnderwriteLog`를 쓰므로 `Debug`를 직접 쓰는 일은 드물지만, 쓸 때는 `UnityEngine.Debug`로 명시한다.

### 초안 제안대로 확정한 항목

| 항목 | 확정 내용 |
|---|---|
| 에디터 도구 검증 메시지 언어 | 에디터 창에 그대로 표시하는 검증 메시지는 UI 문자열로 취급해 **한국어**를 유지한다. 런타임 예외만 영어다 (§9) |
| 테스트 도입 | 도입한다. `Assets/Editor/Tests`에 Test Assembly를 두고 `Underwrite.Editor`를 참조한다. 8방향 생성기의 `Analyze` 검증 로직처럼 **에셋을 바꾸지 않고 판정만 하는 코드**를 먼저 덮는다. `MonoBehaviour` 전체를 덮는 것은 목표로 하지 않는다 |
| 최대 줄 길이 | 120자 권장, 강제하지 않는다 |
| 파일 머리 주석 | 쓰지 않는다. 수정 시 갱신되지 않아 틀린 정보가 남는다 |

`.editorconfig`는 §3·§5·§6·`using` 정렬·줄 길이를 담아 프로젝트 루트에 추가한다.

## 13. 현재 Underwrite 스크립트에 대한 메모

현재 `Assets/Scripts`의 두 파일은 **시험용 코드**다. 이 문서의 기준으로 평가하거나 품질 부채로 집계하지 않는다. 아래는 본 코드를 작성할 때 되풀이하지 않기 위한 메모다.

| 파일 | 메모 |
|---|---|
| `PlayerCharacterInput.cs` | 네임스페이스 없음(§1), `Speed`만 문자열 리터럴(§7-4), `OnMove`의 필드 섀도잉(§10) |
| `ProtagonistPerformance.cs` | 한 줄 다문장·중첩 삼항(§5), 접근 수정자 생략(§8), public 필드 다중 선언(§8), `enum.ToString()` 상태 재생(§7-4), `int` 단계 값으로 분기(§5), 매직 넘버(§7-3) |

`PlayerCharacterInput.cs`의 `CharAnimator`·`MoveSpeed` 같은 PascalCase private 필드는 §3에 맞으므로 문제가 아니다. 타입 약어를 붙인 `CharRigBody2D`만 이름을 다듬으면 된다.

### 본 코드로 승격할 때 고쳐야 할 동작 문제 2건

1. `PlayerCharacterInput.OnMove`가 필드 대신 지역변수에 입력을 담는다. `MoveInput` 필드가 항상 0이므로 이동과 방향 전환이 동작하지 않는다. `var` 선언을 지우면 해결된다.
2. `ProtagonistMotion.Celebrate`를 `animator.Play(motion.ToString())`로 재생하는데 Controller의 상태 이름은 `Cheer`다(`Controller.controller`에서 `Idle`·`Walking`·`Cheer` 확인). 환호가 재생되지 않는다. enum 값을 `Cheer`로 바꾸고 §7-4의 해시 상수를 쓴다.
