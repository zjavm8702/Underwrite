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
        public const string TEXT_ORDER_NOT_AT_MARKET = "지하시장에서만 주문할 수 있습니다.";
        public const string TEXT_ORDER_NO_CASH = "현금이 부족합니다.";
        public const string TEXT_ORDER_NO_SHARES = "보유 지분이 부족합니다.";
        public const string TEXT_ORDER_BAD_QUANTITY = "수량은 1 이상이어야 합니다.";

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
        /// <summary>한 틱이 나타내는 게임 내 분.</summary>
        public const int MARKET_MINUTES_PER_TICK = 5;
        /// <summary>장중 길이(분). 개장부터 폐장까지.</summary>
        public const int MARKET_SESSION_MINUTES = 480;
        /// <summary>실시간 1초가 흐르는 게임 내 분. 배속 1.0 기준.</summary>
        public const float MARKET_MINUTES_PER_REAL_SECOND = 2f;

        // ── 가격 모델 (기획서 §7의 6요인) ──────────
        /// <summary>가격이 내재가치와 기대로 만들어진 목표가를 향해 수렴하는 속도.</summary>
        public const float PRICE_REVERSION_PER_TICK = 0.12f;
        /// <summary>틱마다 더하는 무작위 변동의 기본 폭. 종목 성격과 분위기로 배율이 걸린다.</summary>
        public const float PRICE_NOISE_BASE = 0.006f;
        /// <summary>기대가 실제 가치로 식어가는 속도. 소문은 시간이 지나면 힘을 잃는다.</summary>
        public const float EXPECTATION_DECAY_PER_TICK = 0.02f;
        /// <summary>주문 한 주가 유동성 대비 가격을 밀어내는 정도.</summary>
        public const float ORDER_IMPACT_SCALE = 0.9f;
        /// <summary>한 틱에 허용하는 최대 변동률. 급등락을 허용하되 폭주는 막는다.</summary>
        public const float PRICE_MAX_MOVE_PER_TICK = 0.18f;
        /// <summary>가격 하한. 0 이하로 내려가지 않게 한다.</summary>
        public const float PRICE_FLOOR = 1f;

        // ── 종목 성격별 배율 ───────────────────────
        public const float VOLATILITY_VETERAN = 0.6f;
        public const float VOLATILITY_ROOKIE = 1.8f;
        public const float VOLATILITY_HIGH_RISK = 2.4f;
        public const float VOLATILITY_FAMOUS = 1.0f;

        // ── 시장 분위기별 배율 ────────────────────
        public const float SENTIMENT_DRIFT_CALM = 0f;
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

        // ── 차트 ──────────────────────────────────
        /// <summary>종목별로 보관하는 가격 이력 길이.</summary>
        public const int PRICE_HISTORY_CAPACITY = 240;
    }
}
