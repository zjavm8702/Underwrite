using UnityEngine.InputSystem;

namespace Underwrite.Global
{
    /// <summary>
    /// 문자열 상수. 태그·파라미터·경로처럼 외부 에셋과 묶인 계약과 표시용 문자열을 모두 모은다.
    /// 코드 규칙 §7 참고. 표시용은 TEXT_ 접두사를 쓴다.
    /// </summary>
    /// <summary>
    /// 문자열 상수.
    ///
    /// 화면에 나가는 문구(TEXT_*)는 <c>static</c>이다. 표(UITextTable)가 덮어쓰기 때문이다.
    /// <c>const</c>면 컴파일 시점에 호출부에 박혀 표가 아무 소용이 없다.
    /// 애니메이터 파라미터 이름처럼 코드 계약인 값은 <c>const</c>로 남긴다.
    /// 여기 적힌 한국어는 표가 없을 때 쓰는 기본값이다. 화면에 열쇠 이름이 뜨는 일은 없다.
    /// </summary>
    public static partial class StringConstants
    {
        // Animator 계약. 아트 문서와 Controller에 묶여 있다.
        public const string ANIM_PARAM_DIRECTION_X = "DirectionX";
        public const string ANIM_PARAM_DIRECTION_Y = "DirectionY";
        public const string ANIM_PARAM_SPEED = "Speed";

        public const string ANIM_STATE_IDLE = "Idle";
        public const string ANIM_STATE_WALKING = "Walking";
        public const string ANIM_STATE_CHEER = "Cheer";
        // 아직 그림이 없는 몸짓. Animator에 이 이름의 상태가 생기면 그것을 재생한다(CharacterActor).
        public const string ANIM_STATE_NOD = "Nod";
        public const string ANIM_STATE_SHAKE = "Shake";
        public const string ANIM_STATE_HOP = "Hop";

        // 머리 위 감정 표시
        public static string TEXT_EMOTE_EXCLAIM = "!";
        public static string TEXT_EMOTE_QUESTION = "?";
        public static string TEXT_EMOTE_DOTS = "…";
        public static string TEXT_EMOTE_MUSIC = "♪";
        public static string TEXT_EMOTE_HEART = "♥";
        public static string TEXT_EMOTE_COIN = "＄";

        // 시장 화면 표시용
        public static string TEXT_MARKET_PHASE_CLOSED = "폐장";
        public static string TEXT_MARKET_PHASE_OPEN = "개장";
        public static string TEXT_MARKET_PHASE_HALTED = "휴장 — 영주 조사";

        public static string TEXT_SENTIMENT_CALM = "평온";
        public static string TEXT_SENTIMENT_FEVER = "투자 열풍";
        public static string TEXT_SENTIMENT_FEAR = "공포";
        public static string TEXT_SENTIMENT_DRAINED = "자금 부족";

        public static string TEXT_ORDER_FILLED = "체결되었습니다.";
        public static string TEXT_ORDER_MARKET_CLOSED = "지하시장이 열려 있지 않습니다.";
        public static string TEXT_ORDER_PAUSED = "시간이 멈춘 동안에는 주문할 수 없습니다.";
        public static string TEXT_ORDER_NOT_AT_MARKET = "지하시장에서만 주문할 수 있습니다.";
        public static string TEXT_ORDER_NO_CASH = "현금이 부족합니다.";
        public static string TEXT_ORDER_NO_SHARES = "보유 지분이 부족합니다.";
        public static string TEXT_ORDER_BAD_QUANTITY = "수량은 1 이상이어야 합니다.";
        public static string TEXT_ORDER_BAD_PRICE = "호가를 올바르게 지정하세요.";
        public static string TEXT_ORDER_TOO_MANY = "이 종목에 걸 수 있는 주문 수를 넘었습니다.";
        public static string TEXT_ORDER_QUEUED = "지정가 주문을 걸었습니다. 가격이 닿으면 체결됩니다.";

        // 거래 화면 문구. 표(UITextTable)가 덮어쓴다.
        public static string TEXT_SESSION_MORNING = "오전장";
        public static string TEXT_SESSION_EVENING = "오후장";
        public static string TEXT_SESSION_NEXT_MORNING = "다음 날 오전장";
        public static string TEXT_SIDE_BUY = "매수";
        public static string TEXT_SIDE_SELL = "매도";
        public static string TEXT_SESSION_OPENED = "{0}이 열렸습니다.";
        public static string TEXT_SESSION_END = "{0} 끝내기";
        public static string TEXT_SESSION_OPEN_NEXT = "{0} 열기";
        public static string TEXT_LEAVE_MARKET = "지하시장 나가기";
        public static string TEXT_SPEED = "{0}배";
        public static string TEXT_SESSION_SKIPPED = "장을 넘겼습니다. 소식 {0}건.";
        public static string TEXT_SESSION_SKIPPED_FILLED = "장을 넘겼습니다. 소식 {0}건, 지정가 체결 {1}건.";
        public static string TEXT_AUTO_PAUSED = "결과가 확정되어 자동 정지했습니다.";
        public static string TEXT_ORDER_CANCELLED = "주문을 취소했습니다.";
        public static string TEXT_HINT_MARKET = "체결 시 가격이 달라질 수 있어요.";
        public static string TEXT_HINT_LIMIT = "지정가에 닿으면 체결, 폐장 때 소멸.";
        public static string TEXT_SUBMIT_HALTED = "휴장 중";
        public static string TEXT_SUBMIT_PAUSED = "정지 중";
        public static string TEXT_SUBMIT_NO_SHARES = "보유 주식 없음";
        public static string TEXT_CHART_HALTED = "휴장이라 가격이 갱신되지 않아요.";
        public static string TEXT_SCENE_PUBLIC = "지하세계가 같은 순간에 들었습니다. 가격은 이미 움직였습니다.";
        public static string TEXT_SCENE_PRIVATE = "아직 시장에 돌지 않은 이야기입니다.";
        public static string TEXT_INFO_EMPTY = "아직 들어온 소식이 없습니다.";
        public static string TEXT_HOLDINGS_EMPTY = "보유한 종목이 없습니다.";
        public static string TEXT_TAG_PUBLIC = "시장에 공개됨";
        public static string TEXT_TAG_PRIVATE = "나만 아는 정보";
        public static string TEXT_INFO_NOT_PUBLIC = "아직 시장에 공개되지 않음.";

        // 상장 흐름 (상장흐름 설계안 §3)
        public static string TEXT_LISTING_ARRIVED = "{0}이(가) 영지에 도착했다. 다음 주기 상장 예정 · 예상 공모가 {1:N1}";
        public static string TEXT_BAND_ARRIVED = "갓 도착 · 다음 주기 상장 예정";
        public static string TEXT_BAND_LISTED = "상장 중";
        public static string TEXT_BAND_HELD = "보유 {0}주";
        public static string TEXT_INFO_HELD = "보유 {0}주";
        public static string TEXT_INFO_WHEN = "{0} · {1}일 {2}";
        public static string TEXT_INFO_PRIVATE_SHORT = "나만 앎";
        public static string TEXT_INFO_EMPTY_MINE = "가진 종목에 대한 소식이 아직 없습니다.";
        public static string TEXT_HELD_NOTE = "★ 내 종목 {0}주";

        public static string TEXT_SOURCE_TAVERN_TALK = "여관 소문";
        public static string TEXT_SOURCE_GUILD_RECORD = "길드 기록";
        public static string TEXT_SOURCE_PRIVATE_LETTER = "사적 서신";
        public static string TEXT_SOURCE_MARKET_BOARD = "시장 게시판";
        public static string TEXT_SOURCE_BROKER_LEAK = "중개인 누설";

        // 영지에서 묻기 전에 고르는 자리
        public static string TEXT_GATHER_ASK = "들은 게 있긴 한데, 공짜로 줄 수는 없지요.";
        public static string TEXT_GATHER_PAY = "{0:N0}을 내고 묻는다";
        public static string TEXT_GATHER_PAY_NOTE = "{0}분이 걸린다 · 가진 돈 {1:N0}";
        public static string TEXT_GATHER_NO_MONEY = "돈이 모자란다 · 가진 돈 {0:N0}";
        public static string TEXT_GATHER_LEAVE = "그냥 간다";

        // 여관 창고 아래 지하거래소 (웹툰 6화)
        public static string TEXT_CELLAR_GO = "창고로 내려간다";
        public static string TEXT_CELLAR_NOTE = "술통 너머, 지하거래소";
        public static string TEXT_GATHER_NO_TIME = "다음 장이 코앞이다";

        // 영지에서 맡는 일 (기획서 §10 종잣돈 퀘스트)
        public static string TEXT_WORK_NOTE = "{0}분이 걸린다 · {1:N0}을 받는다";
        public static string TEXT_WORK_NO_TIME = "시간이 모자란다 · 다음 장까지 {0}분";
        public static string TEXT_WORK_RUMOR = "그러고 보니 일하다 들었는데, {0} 쪽에 무슨 일이 있답니다.";
        public static string TEXT_WORK_RESULT = "{0:N0}을 받았습니다. 다음 장까지 {1}시간 {2}분.";

        // 영지 메뉴와 원형 시계 (2026-10-17)
        public static string TEXT_MENU = "메뉴";
        public static string TEXT_MENU_MARKET = "지하거래소로";
        public static string TEXT_MENU_MARKET_LOCKED = "아직 모른다";
        public static string TEXT_MENU_NO_TIME = "다음 장이 코앞이다";
        public static string TEXT_CLOCK_TITLE = "몇 시까지 보낼까";
        public static string TEXT_CLOCK_PICK = "{0:00}:{1:00}까지 · {2}시간 {3}분";
        public static string TEXT_CLOCK_LIMIT = "다음 장은 {0:00}:{1:00}에 열린다. 그 직전까지 고를 수 있다.";
        public static string TEXT_CLOCK_CONFIRM = "그때까지 보낸다";
        public static string TEXT_CLOCK_CANCEL = "그만둔다";
        public static string TEXT_CLOCK_SKIP_SESSION = "{0} 건너뛰기 — 그다음 장 직전까지 돌릴 수 있다";

        // 하루 끝 (기획서 §11의 넷째 단계 — 정보 정리 및 취침)
        public static string TEXT_SKIP_BREAK = "시간 넘기기";
        public static string TEXT_SLEEP = "잠자리에 든다";
        public static string TEXT_DAY_END_TAG = "하루 마감";
        public static string TEXT_DAY_END_TITLE = "{0}일째가 저문다";
        public static string TEXT_DAY_END_MONEY = "오늘 영지에서 {0:N0}을 벌고 {1:N0}을 썼다.";
        public static string TEXT_DAY_END_NOTES = "캐낸 소식 {0}건은 정보 기록에 옮겨 적었다.";
        public static string TEXT_DAY_END_NO_NOTES = "오늘은 캐낸 소식이 없다.";
        public static string TEXT_DAY_END_NEXT = "내일 오전장은 {0:00}시에 열린다.";

        // 후원 — 익명의 의뢰주 (후원흐름 설계안). 용사단 이름은 아직 정하지 않아 임시로 "용사단"이라 부른다.
        public static string TEXT_HERO_PARTY = "용사단";
        public static string TEXT_REQUEST_POST = "익명 의뢰를 낸다";
        public static string TEXT_REQUEST_POST_NOTE = "봉인한 의뢰서와 보수 예치금";
        public static string TEXT_REQUEST_POST_DEADLINE = "봉인한 의뢰서와 보수 예치금 · 들은 사정의 기한까지 {0}일";
        public static string TEXT_REQUEST_CLERK = "길드 서기";
        public static string TEXT_REQUEST_ASK = "어떤 의뢰입니까? 의뢰주 이름은 비워 두셔도 됩니다. 보수만 먼저 맡기시면 됩니다.";
        public static string TEXT_REQUEST_PURPOSE_HEALING = "치료비를 선금으로 주는 의뢰";
        public static string TEXT_REQUEST_PURPOSE_GEAR = "장비 수리비를 선금으로 주는 의뢰";
        public static string TEXT_REQUEST_PURPOSE_SUPPLIES = "식량과 물자를 미리 대 주는 의뢰";
        public static string TEXT_REQUEST_PURPOSE_TRAINING = "교관을 붙여 주는 의뢰";
        public static string TEXT_REQUEST_COST_NOTE = "예치금 {0:N0} · 가진 돈 {1:N0}";
        public static string TEXT_REQUEST_CANCEL = "그만둔다";
        public static string TEXT_REQUEST_DONE = "봉인한 의뢰서를 창구에 맡겼다. 수신인은 {0}. 의뢰주 칸은 비워 두었다.";
        public static string TEXT_REQUEST_DONE_NOTE = "{0:N0}을 맡겼습니다. 반응은 여관에서 들을 수 있다.";

        public static string TEXT_NEED_HEALER_RUMOR = "(술을 나르다 들었다) \"치유사가 지난 원정에서 크게 다쳤어. 약값이 없으니 큰 의뢰는 전부 물렸지.\"";
        public static string TEXT_NEED_HEALER_MET = "(술을 나르다 들었다) \"우리 앞으로 의뢰가 들어왔어. 치료비를 선금으로 준다고… 누가 우리 사정을 이렇게 잘 알지?\"";
        public static string TEXT_NEED_SHIELD_RUMOR = "(술을 나르다 들었다) \"방패가 쪼개졌는데 대장간에 맡길 돈이 없어. 이대로 나가면 앞줄이 버티질 못해.\"";
        public static string TEXT_NEED_SHIELD_MET = "(술을 나르다 들었다) \"수리비를 먼저 주는 의뢰라니. 방패 얘기는 여기서밖에 안 했는데.\"";
        public static string TEXT_HERO_REACTION_MISSED = "(술을 나르다 들었다) \"이상한 의뢰가 하나 들어왔더라. 고마운 일이긴 한데, 지금 우리한테 필요한 건 그게 아니야.\"";

        public static string TEXT_DAY_END_DEADLINE = "들은 용사단 사정의 기한까지 {0}일.";
        public static string TEXT_DAY_END_INSPECTION = "내일은 영주 시찰일이다. 지하시장은 하루 문을 닫는다.";
        public static string TEXT_VILLAGE_INSPECTION_TODAY = "오늘은 영주 시찰일 · 지하시장이 닫혔다";
        public static string TEXT_VILLAGE_INSPECTION_SOON = "영주 시찰까지 {0}일";
        public static string TEXT_DAY_END_WARNING = "여관 단골들 사이가 뒤숭숭하다. 무슨 일이 있는 모양이다.";
        public static string TEXT_DAY_END_COLLAPSE = "용사단이 의뢰를 접고 흩어졌다는 소식이 돌았다. 10년 뒤 그 자리에 설 사람이 없다.";

        // 영지 사람에게 말 걸기
        public static string TEXT_NPC_TALK_KEY = "E — 말을 건다";
        public static string TEXT_NPC_ROLE_RESIDENT = "주민";
        public static string TEXT_NPC_ROLE_VENDOR = "노점상";
        public static string TEXT_NPC_ROLE_ADVENTURER = "모험단";
    }

    /// <summary>
    /// 숫자·조정값 상수. 가격 모델의 계수는 전부 여기에 모아 한곳에서 조정한다.
    /// </summary>
    /// <summary>
    /// 공용 상수.
    ///
    /// 밸런스를 보면서 고치는 값은 <c>static</c>이다. 표(MarketTuningData)가 장을 열기 전에
    /// 덮어쓰기 때문이다. <c>const</c>면 컴파일 시점에 박혀 표가 아무 소용이 없다.
    /// 구조와 안전장치(PRICE_FLOOR, BOOK_LEVELS 등)는 <c>const</c>로 남긴다.
    /// 표에서 0이나 음수로 바뀌면 시뮬레이션이 깨지는데 인스펙터에는 그걸 막을 방법이 없다.
    /// </summary>
    public static class Constants
    {
        // ── 시장 시간 ──────────────────────────────
        /// <summary>한 틱이 나타내는 게임 내 분. 가격은 이 간격으로 갱신된다.</summary>
        public const int MARKET_MINUTES_PER_TICK = 5;
        /// <summary>한 장의 길이(분). 6시간이다. 72틱이 된다.</summary>
        public const int MARKET_SESSION_MINUTES = 360;
        /// <summary>
        /// 실시간 1초가 흐르는 게임 내 분. 배속 1.0 기준.
        /// 0.3333이면 한 틱이 실시간 15초, 한 장이 약 18분이다.
        /// 이보다 빠르면 가격이 눈으로 따라가기 어렵고, 판단할 틈이 없다.
        /// </summary>
        public static float MARKET_MINUTES_PER_REAL_SECOND = 0.3333f;

        /// <summary>오전장 개장 시각. 09시부터 15시까지다.</summary>
        public const int MARKET_MORNING_START_HOUR = 9;

        /// <summary>오후장 개장 시각. 21시부터 다음 날 03시까지다.</summary>
        public const int MARKET_EVENING_START_HOUR = 21;

        /// <summary>
        /// 장중 정보 타임이 열리는 틱. 한 장에 두 번이다.
        /// 지하세계로 모험 진행 상황이 들어오는 순간이고, 그때 시장이 함께 반응한다.
        /// 72틱 장에서 24·48이면 장 시작 6분·12분 지점이다.
        /// </summary>
        public const int INFO_TIME_FIRST_TICK = 24;

        /// <summary>두 번째 정보 타임. 장의 2/3 지점이다.</summary>
        public const int INFO_TIME_SECOND_TICK = 48;

        /// <summary>한 장에 열리는 정보 타임 횟수.</summary>
        public const int INFO_TIMES_PER_SESSION = 2;

        /// <summary>
        /// 한 주기에 플레이하는 장의 수. 설계안 §2는 3~5개라 했고 샘플로 4를 쓴다.
        /// 오전·오후를 합쳐 하루에 두 장이므로 한 주기는 이틀이다.
        /// 주기는 원정 한 번에 해당하며 40주기가 10년이다.
        /// </summary>
        public static int SESSIONS_PER_CYCLE = 4;

        /// <summary>
        /// 영지에서 흐르는 시간의 속도. 기획서 §11에서 실시간 1:1로 확정했다.
        /// 장중과 달리 압축하지 않는다. 그 시간이 플레이어의 것이기 때문이다.
        /// </summary>
        public static float BREAK_MINUTES_PER_REAL_SECOND = 1f / 60f;

        /// <summary>
        /// 「시간 넘기기」로 건너뛸 때 다음 장 몇 분 전에 멈추는가.
        /// 바로 개장에 떨어뜨리면 숨 돌릴 틈 없이 장이 시작된다.
        /// </summary>
        public static int BREAK_SKIP_MARGIN_MINUTES = 10;

        /// <summary>
        /// 영지에서 한 가지 일을 하는 데 드는 게임 시간(분).
        /// 영지의 자원은 횟수가 아니라 시간이다. 시간을 다 쓰면 장이 열린다.
        /// </summary>
        public static int GATHER_MINUTES_PER_VISIT = 60;

        /// <summary>상장 자리의 기본 수. 설계안 §6에 따라 분위기와 연차로 늘거나 준다.</summary>
        public static int LISTING_SLOTS = 3;

        /// <summary>전멸 청산율. 잔여 자산만 돌려받는다.</summary>
        public static float LIQUIDATION_WIPED = 0.15f;

        /// <summary>은퇴 청산율. 예고가 있으므로 미리 팔 수 있다.</summary>
        public static float LIQUIDATION_RETIRED = 0.85f;

        /// <summary>영지 이동 청산율.</summary>
        public static float LIQUIDATION_MOVED = 0.75f;

        // ── 가격 모델 (기획서 §7의 6요인) ──────────
        /// <summary>가격이 목표가를 향해 올라갈 때의 수렴 속도.</summary>
        public static float PRICE_REVERSION_PER_TICK = 0.02f;
        /// <summary>
        /// 가격이 목표가를 향해 내려갈 때의 수렴 속도. 올라갈 때보다 빠르다.
        /// 실제 급등주는 무너지는 속도가 오르는 속도보다 훨씬 빠르다. GameStop 2021은
        /// 오르는 데 20일, 정점에서 80퍼센트 빠지는 데 3거래일이 걸렸다.
        /// 상한(PRICE_MAX_DROP_PER_TICK)은 극단적 충격만 막고, 평소의 붕괴 속도는 이 값이 만든다.
        /// </summary>
        public static float PRICE_REVERSION_DOWN_PER_TICK = 0.035f;
        /// <summary>틱마다 더하는 무작위 변동의 기본 폭. 종목 성격과 분위기로 배율이 걸린다.</summary>
        public static float PRICE_NOISE_BASE = 0.0032f;

        /// <summary>
        /// 추세 지속. 직전 틱의 흐름이 다음 틱에 남는 비율.
        /// 0이면 매 틱이 서로 독립이라 가격이 한 수준에서 맴돌고, 그 수준만 알면 사고파는 게 공짜가 된다.
        /// 실제 주가가 예측하기 어려운 이유는 변동이 커서가 아니라 방향이 이어지기 때문이다.
        /// </summary>
        public static float PRICE_MOMENTUM_RETENTION = 0.62f;

        /// <summary>미세 호가를 다시 뽑는 간격(실시간 초). 틱과 무관하게 돈다.</summary>
        public static float MARKET_MICRO_SECONDS = 0.12f;

        /// <summary>미세 호가 흔들림 한 번의 크기. 틱 종가 대비 비율이다.</summary>
        public static float MICRO_AMPLITUDE = 0.0013f;

        /// <summary>미세 흔들림이 남는 비율. 1에 가까울수록 천천히 제자리로 돌아온다.</summary>
        public static float MICRO_RETENTION = 0.72f;

        /// <summary>미세 흔들림의 한계. 틱 종가에서 이보다 멀어지지 않는다.</summary>
        public static float MICRO_LIMIT = 0.007f;
        /// <summary>기대가 실제 가치로 식어가는 속도. 소문은 시간이 지나면 힘을 잃는다.</summary>
        public static float EXPECTATION_DECAY_PER_TICK = 0.02f;
        /// <summary>주문 한 주가 유동성 대비 가격을 밀어내는 정도.</summary>
        public static float ORDER_IMPACT_SCALE = 0.9f;
        /// <summary>한 틱에 허용하는 최대 상승률. 급등을 허용하되 폭주는 막는다.</summary>
        public static float PRICE_MAX_MOVE_PER_TICK = 0.18f;
        /// <summary>
        /// 한 틱에 허용하는 최대 하락률. 상승 상한보다 크다.
        /// 실제 급등주는 무너지는 속도가 오르는 속도보다 훨씬 빠르다.
        /// GameStop 2021은 오르는 데 20일, 정점에서 80퍼센트 빠지는 데 3거래일이 걸렸다.
        /// 조사 기록: Documents/Claude/Underwrite_시장모델_실제자료_샘플.md
        /// </summary>
        public static float PRICE_MAX_DROP_PER_TICK = 0.30f;
        /// <summary>가격 하한. 0 이하로 내려가지 않게 한다.</summary>
        public const float PRICE_FLOOR = 1f;

        // ── 종목 성격별 배율 ───────────────────────
        // 현실 대형주의 일 변동성은 1퍼센트대다. 모델은 짧은 장에서 체감이 필요해 더 크게 잡지만,
        // 베테랑만은 낮춰 종목 성격 사이의 대비를 키운다. 전부 현실에 맞추면 셋 다 밋밋해진다.
        public static float VOLATILITY_VETERAN = 0.45f;
        public static float VOLATILITY_ROOKIE = 1.8f;
        public static float VOLATILITY_HIGH_RISK = 2.4f;
        public static float VOLATILITY_FAMOUS = 1.0f;
        /// <summary>용사단 지수. 거의 흔들리지 않는다.</summary>
        public static float VOLATILITY_INDEX = 0.15f;

        // ── 용사단 지수 (상장흐름 설계안 §1-4·§1-5) ──
        public const string HERO_STOCK_ID = "STOCK_HEROES";
        /// <summary>처음 가치.</summary>
        public static float HERO_INDEX_VALUE = 100f;
        /// <summary>두꺼운 호가. 지수는 큰돈이 들어가도 잘 움직이지 않는다.</summary>
        public static float HERO_INDEX_LIQUIDITY = 2000f;
        /// <summary>
        /// 장마다 오르는 내재가치. 한 주기(4장) 약 2%, 40주기 약 2.2배 — 설계안의 "분기 +2%".
        /// 안전하게만 가면 반드시 모자라야 한다. 그래서 작다.
        /// </summary>
        public static float HERO_INDEX_GROWTH_PER_SESSION = 0.005f;
        /// <summary>시나리오가 깨지면 남는 가치의 비율. 실적이 끊기면 지수가 성립하지 않는다.</summary>
        public static float HERO_INDEX_COLLAPSE_RATIO = 0.12f;

        // ── 시장 분위기별 배율 ────────────────────
        // NASDAQ 10년(2016~2025)은 상승 8년 / 하락 2년이었다. 평온한 장에 약한 상승 편향을 둔다.
        public static float SENTIMENT_DRIFT_CALM = 0.0005f;
        public static float SENTIMENT_DRIFT_FEVER = 0.004f;
        public static float SENTIMENT_DRIFT_FEAR = -0.006f;
        public static float SENTIMENT_DRIFT_DRAINED = -0.002f;

        public static float SENTIMENT_VOLATILITY_CALM = 1.0f;
        public static float SENTIMENT_VOLATILITY_FEVER = 1.6f;
        public static float SENTIMENT_VOLATILITY_FEAR = 2.0f;
        public static float SENTIMENT_VOLATILITY_DRAINED = 1.2f;

        // ── 플레이어 자금 ─────────────────────────
        /// <summary>시제품 시작 현금. 종잣돈 퀘스트 전 기본값이며 검증에서 조정한다.</summary>
        public static float STARTING_CASH = 500f;

        // ── 호가와 지정가 ─────────────────────────
        /// <summary>호가창에 보여줄 매수·매도 각 단계 수.</summary>
        public const int BOOK_LEVELS = 5;
        /// <summary>
        /// 시장가 주문이 먹어 들어갈 수 있는 호가 단계의 한도. 화면에는 5단계만 보이지만 그 밖에도 호가는 있다.
        /// 이것을 넘는 주문은 마지막 가격에 남은 수량을 채운다.
        /// </summary>
        public const int BOOK_SWEEP_LIMIT = 40;
        /// <summary>호가 단위를 가격의 몇 배로 잡을지. 126.8이면 약 0.5가 된다.</summary>
        public static float BOOK_TICK_RATIO = 0.004f;
        /// <summary>호가 단위의 하한.</summary>
        public const float BOOK_TICK_MIN = 0.1f;
        /// <summary>1단계 호가에 쌓이는 수량을 유동성의 몇 배로 잡을지.</summary>
        public static float BOOK_DEPTH_BASE = 0.35f;
        /// <summary>단계가 멀어질수록 수량이 늘어나는 비율.</summary>
        public static float BOOK_DEPTH_GROWTH = 0.45f;
        /// <summary>한 종목에 동시에 걸 수 있는 지정가 주문 수.</summary>
        public const int PENDING_MAX_PER_STOCK = 5;

        // ── 차트 ──────────────────────────────────
        /// <summary>종목별로 보관하는 가격 이력 길이.</summary>
        public const int PRICE_HISTORY_CAPACITY = 240;

        // ── 영지 앞뒤 정렬 ────────────────────────
        /// <summary>
        /// 발밑 높이 1유닛을 정렬 순서 몇 칸으로 나눌지. 100이면 0.01유닛 차이까지 앞뒤를 가린다.
        /// 영지가 위아래로 ±300유닛을 넘으면 sortingOrder 범위(±32767)를 벗어난다.
        /// </summary>
        public const int VILLAGE_SORT_PER_UNIT = 100;
        /// <summary>바닥판의 정렬 순서. 어떤 사물·사람보다도 뒤에 깔린다.</summary>
        public const int VILLAGE_SORT_GROUND = -32000;
        /// <summary>
        /// 주인공 루트에서 발바닥까지의 거리(월드 유닛). 스프라이트 피벗이 Center라서 루트는 몸 한가운데다.
        /// 캔버스 724px의 아래 13px이 발바닥이고, PPU 100 × 축척 0.27이면 (362 − 13) × 0.0027 ≈ 0.94다.
        /// </summary>
        public const float PROTAGONIST_FOOT_OFFSET = -0.94f;

        // ── 영지 NPC ──────────────────────────────
        /// <summary>길 찾기 격자 한 칸(월드 유닛). 사람 발 폭(0.55)보다 조금 작게 잡는다.</summary>
        public const float VILLAGE_NAV_CELL = 0.4f;
        /// <summary>격자 칸을 막혔다고 볼 때 더하는 여유. 벽에 몸을 비비며 걷지 않게 한다.</summary>
        public const float VILLAGE_NAV_CLEARANCE = 0.25f;
        /// <summary>NPC 걷는 속도. 주인공(3.0)보다 느려야 영지를 산책하는 사람으로 보인다.</summary>
        public static float NPC_WALK_SPEED = 1.5f;
        /// <summary>모험단 안에서 앞사람과의 간격(월드 유닛).</summary>
        public static float NPC_PARTY_SPACING = 0.8f;
        /// <summary>목적지에 닿은 뒤 머무는 시간 범위(초).</summary>
        public static float NPC_PARTY_REST_MIN = 4f;
        public static float NPC_PARTY_REST_MAX = 10f;
        /// <summary>모험단이 장소에 닿았을 때 문 앞에서 아래로 비켜 서는 거리(월드 유닛).</summary>
        public static float NPC_PARTY_DOOR_GAP = 2.2f;
        /// <summary>그 둘레에서 흩어지는 반지름. 단마다 같은 점에 겹쳐 서지 않게 한다.</summary>
        public static float NPC_PARTY_SPREAD = 1.5f;
        /// <summary>모험단이 닿았을 때 한 사람이 들떠 몸짓하는 확률.</summary>
        public static float NPC_PARTY_CHEER_CHANCE = 0.45f;
        /// <summary>말을 걸 수 있는 거리(월드 유닛). 사람 키(1.9)보다 조금 작다.</summary>
        public static float NPC_TALK_RANGE = 1.4f;

        // ── 영주 시찰일 (기획서 §11) ──────────────
        /// <summary>첫 시찰일. 시제품에서는 4일째다. 그 전 사흘 동안 장과 영지에 익숙해진다.</summary>
        public static int INSPECTION_FIRST_DAY = 4;
        /// <summary>시찰 간격(일). 매번 달력에 미리 보인다. 시제품 검증에서 조정한다.</summary>
        public static int INSPECTION_INTERVAL_DAYS = 6;

        // ── 용사단 일정 (루틴 설계안) ─────────────
        /// <summary>원정을 떠나는 날. 시찰일(4일) 다음 날이다.</summary>
        public static int HERO_EXPEDITION_DAY = 5;
        /// <summary>원정 기간(일). 이 날들에는 여관에서 만날 수 없다.</summary>
        public static int HERO_EXPEDITION_DAYS = 2;
        /// <summary>여관 테이블에서 한 사람과 이야기하는 데 드는 영지 시간(분).</summary>
        public static int HERO_TALK_MINUTES = 60;
        /// <summary>이만큼 친해지면 현장 이야기를 덤으로 흘린다(관계와 투자의 다리).</summary>
        public static int HERO_FIELD_TALK_AFFINITY = 3;
        /// <summary>그 정도로 친할 때 현장 이야기가 나올 확률.</summary>
        public static float HERO_FIELD_TALK_CHANCE = 0.5f;

        // ── 후원 (익명의 의뢰주) ──────────────────
        /// <summary>용사단 사정 하나의 기한(일). 사용자 결정 2026-10-17.</summary>
        public static int PATRONAGE_DEADLINE_DAYS = 5;
        /// <summary>못 들은 사정의 기한이 이만큼 남으면 하루 결산에 경고를 띄운다.</summary>
        public static int PATRONAGE_WARNING_DAYS = 2;
        /// <summary>익명 의뢰를 꾸며 맡기는 데 드는 영지 시간(분).</summary>
        public static int REQUEST_MINUTES = 30;
        /// <summary>목적별 예치금. 시작 자금 500에 견주었다. 시제품 검증에서 조정한다.</summary>
        public static float REQUEST_COST_HEALING = 120f;
        public static float REQUEST_COST_GEAR = 150f;
        public static float REQUEST_COST_SUPPLIES = 80f;
        public static float REQUEST_COST_TRAINING = 100f;

        // ── 영지 카메라 확대 (2026-10-17) ─────────
        /// <summary>가장 가까이 당겼을 때의 Orthographic Size. 기본 7.5에서 약 1.7배 크게 보인다.</summary>
        public static float VILLAGE_ZOOM_NEAREST = 4.5f;
        /// <summary>휠 한 칸에 바뀌는 크기.</summary>
        public static float VILLAGE_ZOOM_STEP = 0.5f;
        /// <summary>화면이 목표 크기를 따라가는 빠르기. 클수록 빨리 붙는다.</summary>
        public static float VILLAGE_ZOOM_SMOOTH = 10f;
        /// <summary>이보다 작은 휠 값은 무시한다. 터치패드의 잔 움직임이다.</summary>
        public const float VILLAGE_ZOOM_SCROLL_THRESHOLD = 0.01f;

        // ── 필드 연기 (2026-10-17) ────────────────
        /// <summary>감정 표시가 떠 있는 시간(초).</summary>
        public static float ACTOR_EMOTE_SECONDS = 1.6f;
        /// <summary>말풍선이 떠 있는 시간(초).</summary>
        public static float ACTOR_SAY_SECONDS = 3.2f;
        /// <summary>그림이 없는 몸짓을 코드로 대신할 때의 길이(초).</summary>
        public static float ACTOR_FALLBACK_SECONDS = 0.6f;
        /// <summary>다가가 E를 누른 뒤 대화창이 열리기까지. 서로 돌아보는 모습이 보일 만큼.</summary>
        public static float ACTOR_APPROACH_SECONDS = 0.45f;
        /// <summary>이 거리 안에 들어오면 서 있는 사람이 주인공을 돌아본다.</summary>
        public static float ACTOR_NOTICE_RANGE = 2.6f;
        /// <summary>같은 사람이 다시 알아보는 표시를 띄우기까지(초).</summary>
        public static float ACTOR_NOTICE_COOLDOWN = 25f;
        /// <summary>노점상이 손님을 부르는 거리와 간격(초).</summary>
        public static float VENDOR_CALL_RANGE = 7f;
        public static float VENDOR_CALL_MIN = 7f;
        public static float VENDOR_CALL_MAX = 13f;

        // ── 대화 ──────────────────────────────────
        /// <summary>선택지를 고르는 숫자 키. 화면에 붙는 번호 「1.」「2.」와 같은 순서다.</summary>
        public static readonly Key[] DIALOGUE_CHOICE_KEYS =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };
    }
}
