namespace Underwrite.Global
{
    /// <summary>
    /// 문자열 상수. 태그·파라미터·경로처럼 외부 에셋과 묶인 계약과 표시용 문자열을 모두 모은다.
    /// 코드 규칙 §7 참고. 표시용은 TEXT_ 접두사를 쓴다.
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

        // 시장 화면 표시용
        public const string TEXT_MARKET_PHASE_CLOSED = "폐장";
        public const string TEXT_MARKET_PHASE_OPEN = "개장";
        public const string TEXT_MARKET_PHASE_HALTED = "휴장 — 영주 조사";

        public const string TEXT_SENTIMENT_CALM = "평온";
        public const string TEXT_SENTIMENT_FEVER = "투자 열풍";
        public const string TEXT_SENTIMENT_FEAR = "공포";
        public const string TEXT_SENTIMENT_DRAINED = "자금 부족";

        public const string TEXT_ORDER_FILLED = "체결되었습니다.";
        public const string TEXT_ORDER_MARKET_CLOSED = "지하시장이 열려 있지 않습니다.";
        public const string TEXT_ORDER_PAUSED = "시간이 멈춘 동안에는 주문할 수 없습니다.";
        public const string TEXT_ORDER_NOT_AT_MARKET = "지하시장에서만 주문할 수 있습니다.";
        public const string TEXT_ORDER_NO_CASH = "현금이 부족합니다.";
        public const string TEXT_ORDER_NO_SHARES = "보유 지분이 부족합니다.";
        public const string TEXT_ORDER_BAD_QUANTITY = "수량은 1 이상이어야 합니다.";
        public const string TEXT_ORDER_BAD_PRICE = "호가를 올바르게 지정하세요.";
        public const string TEXT_ORDER_TOO_MANY = "이 종목에 걸 수 있는 주문 수를 넘었습니다.";
        public const string TEXT_ORDER_QUEUED = "지정가 주문을 걸었습니다. 가격이 닿으면 체결됩니다.";

        public const string TEXT_SOURCE_TAVERN_TALK = "여관 소문";
        public const string TEXT_SOURCE_GUILD_RECORD = "길드 기록";
        public const string TEXT_SOURCE_PRIVATE_LETTER = "사적 서신";
        public const string TEXT_SOURCE_MARKET_BOARD = "시장 게시판";
        public const string TEXT_SOURCE_BROKER_LEAK = "중개인 누설";
    }

    /// <summary>
    /// 숫자·조정값 상수. 가격 모델의 계수는 전부 여기에 모아 한곳에서 조정한다.
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
        public const float MARKET_MINUTES_PER_REAL_SECOND = 0.3333f;

        /// <summary>오전장 개장 시각. 09시부터 15시까지다.</summary>
        /// <summary>
        /// 장중 정보 타임이 열리는 틱. 한 장에 두 번이다.
        /// 지하세계로 모험 진행 상황이 들어오는 순간이고, 그때 시장이 함께 반응한다.
        /// 72틱 장에서 24·48이면 장 시작 6분·12분 지점이다.
        /// </summary>
        public const int INFO_TIME_FIRST_TICK = 24;

        public const int INFO_TIME_SECOND_TICK = 48;

        public const int INFO_TIMES_PER_SESSION = 2;

        public const int MARKET_MORNING_START_HOUR = 9;
        /// <summary>오후장 개장 시각. 21시부터 다음 날 03시까지다.</summary>
        public const int MARKET_EVENING_START_HOUR = 21;

        // ── 가격 모델 (기획서 §7의 6요인) ──────────
        /// <summary>가격이 목표가를 향해 올라갈 때의 수렴 속도.</summary>
        public const float PRICE_REVERSION_PER_TICK = 0.02f;
        /// <summary>
        /// 가격이 목표가를 향해 내려갈 때의 수렴 속도. 올라갈 때보다 빠르다.
        /// 실제 급등주는 무너지는 속도가 오르는 속도보다 훨씬 빠르다. GameStop 2021은
        /// 오르는 데 20일, 정점에서 80퍼센트 빠지는 데 3거래일이 걸렸다.
        /// 상한(PRICE_MAX_DROP_PER_TICK)은 극단적 충격만 막고, 평소의 붕괴 속도는 이 값이 만든다.
        /// </summary>
        public const float PRICE_REVERSION_DOWN_PER_TICK = 0.035f;
        /// <summary>틱마다 더하는 무작위 변동의 기본 폭. 종목 성격과 분위기로 배율이 걸린다.</summary>
        public const float PRICE_NOISE_BASE = 0.0032f;

        /// <summary>
        /// 추세 지속. 직전 틱의 흐름이 다음 틱에 남는 비율.
        /// 0이면 매 틱이 서로 독립이라 가격이 한 수준에서 맴돌고, 그 수준만 알면 사고파는 게 공짜가 된다.
        /// 실제 주가가 예측하기 어려운 이유는 변동이 커서가 아니라 방향이 이어지기 때문이다.
        /// </summary>
        public const float PRICE_MOMENTUM_RETENTION = 0.62f;

        /// <summary>미세 호가를 다시 뽑는 간격(실시간 초). 틱과 무관하게 돈다.</summary>
        public const float MARKET_MICRO_SECONDS = 0.12f;

        /// <summary>미세 호가 흔들림 한 번의 크기. 틱 종가 대비 비율이다.</summary>
        public const float MICRO_AMPLITUDE = 0.0013f;

        /// <summary>미세 흔들림이 남는 비율. 1에 가까울수록 천천히 제자리로 돌아온다.</summary>
        public const float MICRO_RETENTION = 0.72f;

        /// <summary>미세 흔들림의 한계. 틱 종가에서 이보다 멀어지지 않는다.</summary>
        public const float MICRO_LIMIT = 0.007f;
        /// <summary>기대가 실제 가치로 식어가는 속도. 소문은 시간이 지나면 힘을 잃는다.</summary>
        public const float EXPECTATION_DECAY_PER_TICK = 0.02f;
        /// <summary>주문 한 주가 유동성 대비 가격을 밀어내는 정도.</summary>
        public const float ORDER_IMPACT_SCALE = 0.9f;
        /// <summary>한 틱에 허용하는 최대 상승률. 급등을 허용하되 폭주는 막는다.</summary>
        public const float PRICE_MAX_MOVE_PER_TICK = 0.18f;
        /// <summary>
        /// 한 틱에 허용하는 최대 하락률. 상승 상한보다 크다.
        /// 실제 급등주는 무너지는 속도가 오르는 속도보다 훨씬 빠르다.
        /// GameStop 2021은 오르는 데 20일, 정점에서 80퍼센트 빠지는 데 3거래일이 걸렸다.
        /// 조사 기록: Documents/Claude/Underwrite_시장모델_실제자료_샘플.md
        /// </summary>
        public const float PRICE_MAX_DROP_PER_TICK = 0.30f;
        /// <summary>가격 하한. 0 이하로 내려가지 않게 한다.</summary>
        public const float PRICE_FLOOR = 1f;

        // ── 종목 성격별 배율 ───────────────────────
        // 현실 대형주의 일 변동성은 1퍼센트대다. 모델은 짧은 장에서 체감이 필요해 더 크게 잡지만,
        // 베테랑만은 낮춰 종목 성격 사이의 대비를 키운다. 전부 현실에 맞추면 셋 다 밋밋해진다.
        public const float VOLATILITY_VETERAN = 0.45f;
        public const float VOLATILITY_ROOKIE = 1.8f;
        public const float VOLATILITY_HIGH_RISK = 2.4f;
        public const float VOLATILITY_FAMOUS = 1.0f;

        // ── 시장 분위기별 배율 ────────────────────
        // NASDAQ 10년(2016~2025)은 상승 8년 / 하락 2년이었다. 평온한 장에 약한 상승 편향을 둔다.
        public const float SENTIMENT_DRIFT_CALM = 0.0005f;
        public const float SENTIMENT_DRIFT_FEVER = 0.004f;
        public const float SENTIMENT_DRIFT_FEAR = -0.006f;
        public const float SENTIMENT_DRIFT_DRAINED = -0.002f;

        public const float SENTIMENT_VOLATILITY_CALM = 1.0f;
        public const float SENTIMENT_VOLATILITY_FEVER = 1.6f;
        public const float SENTIMENT_VOLATILITY_FEAR = 2.0f;
        public const float SENTIMENT_VOLATILITY_DRAINED = 1.2f;

        // ── 플레이어 자금 ─────────────────────────
        /// <summary>시제품 시작 현금. 종잣돈 퀘스트 전 기본값이며 검증에서 조정한다.</summary>
        public const float STARTING_CASH = 500f;

        // ── 호가와 지정가 ─────────────────────────
        /// <summary>호가창에 보여줄 매수·매도 각 단계 수.</summary>
        public const int BOOK_LEVELS = 5;
        /// <summary>호가 단위를 가격의 몇 배로 잡을지. 126.8이면 약 0.5가 된다.</summary>
        public const float BOOK_TICK_RATIO = 0.004f;
        /// <summary>호가 단위의 하한.</summary>
        public const float BOOK_TICK_MIN = 0.1f;
        /// <summary>1단계 호가에 쌓이는 수량을 유동성의 몇 배로 잡을지.</summary>
        public const float BOOK_DEPTH_BASE = 0.12f;
        /// <summary>단계가 멀어질수록 수량이 늘어나는 비율.</summary>
        public const float BOOK_DEPTH_GROWTH = 0.45f;
        /// <summary>한 종목에 동시에 걸 수 있는 지정가 주문 수.</summary>
        public const int PENDING_MAX_PER_STOCK = 5;

        // ── 차트 ──────────────────────────────────
        /// <summary>종목별로 보관하는 가격 이력 길이.</summary>
        public const int PRICE_HISTORY_CAPACITY = 240;
    }
}
