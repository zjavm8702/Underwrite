namespace Underwrite.Global
{
    /// <summary>
    /// 거래 화면의 문구. MarketScreenController에 박혀 있던 것을 옮겼다(2026-10-18, 코드 규칙 §7-2).
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_HALT_BANNER = "오늘은 장이 열리지 않는다";
        public static string TEXT_STOCK_HOLD_EMPTY = "보유 0주 · 평균 매입가 0.0";
        public static string TEXT_STOCK_HOLD_PREFIX = "보유 ";
        public static string TEXT_STOCK_HOLD_MIDDLE = "주 · 평균 매입가 ";

        public static string TEXT_TOAST_BROUGHT_TITLE = "영지에서 들은 것";
        public static string TEXT_TOAST_BROUGHT_BODY = "캐낸 소식 {0}건이 정보 기록에 들어왔습니다.";
        public static string TEXT_TOAST_AWAY_TITLE = "쉰 동안의 장";
        public static string TEXT_TOAST_AWAY_BODY = "장 {0}회가 지나갔습니다. 자산 {1}{2:N0}, 오간 소식 {3}건.";

        // 청산 알림을 가려내는 표지. TEXT_NOTICE_DELIST_SOLD에 반드시 들어 있어야 한다.
        public static string TEXT_NOTICE_SOLD_MARK = "청산했습니다";
        public static string TEXT_POPUP_DELIST_TITLE = "상장 폐지";
        public static string TEXT_POPUP_OK = "확인";

        public static string TEXT_ORDER_QUEUED_MESSAGE = "{0:N1}에 {1}주 {2} 주문을 걸었습니다.";
        public static string TEXT_ORDER_QUEUED_TITLE = "{0} 주문을 걸었습니다";
        public static string TEXT_ORDER_QUEUED_BODY = "{0} · {1}주 · 지정가 {2:N1}";
        public static string TEXT_ORDER_BLOCKED_TITLE = "주문이 막혔습니다";
        public static string TEXT_ORDER_FILLED_MESSAGE = "{0} {1}주 @ {2:N1} = {3:N0}";
        public static string TEXT_ORDER_FILLED_TITLE = "{0} 체결";
        public static string TEXT_ORDER_FILLED_BODY = "{0} · {1}주 @ {2:N1} = {3:N0}";

        public static string TEXT_CLOCK_SESSION = " · {0} {1}";
        public static string TEXT_CLOCK_TICKS = " · {0} / {1}틱";
        public static string TEXT_SENTIMENT = "분위기 {0}";
        public static string TEXT_PAUSE = "일시정지";
        public static string TEXT_RESUME = "재개";
        public static string TEXT_PLACE_MARKET = "지하시장에 있음";
        public static string TEXT_PLACE_MOVING = "영지 이동 중";
        public static string TEXT_CHART_FOOTER = "한 장 · {0}틱";
        public static string TEXT_CHART_OPEN = "시작 {0:N1}";
        public static string TEXT_CHART_LOW = "저 {0:N1}";
        public static string TEXT_CHART_HIGH = "고 {0:N1}";

        public static string TEXT_PENDING_ROW = "{0} · {1:N1} {2}주 {3}";
        public static string TEXT_PENDING_CANCEL = "취소";
        public static string TEXT_PRICE_LAST = "마지막 거래가";
        public static string TEXT_PRICE_NOW = "현재가";
        public static string TEXT_RATIO_MAX_BUY = "최대";
        public static string TEXT_RATIO_MAX_SELL = "전량";
        public static string TEXT_HOLD_QTY = "{0}주";
        public static string TEXT_HOLD_QTY_LOCKED = "{0}주 (가용 {1})";
        public static string TEXT_IMPACT_LABEL = "체결 충격 ({0}주)";
        public static string TEXT_SUBMIT_LIMIT = "{0:N1}에 {1}주 {2}";
        public static string TEXT_SUBMIT_MARKET = "{0}주 {1}";
        public static string TEXT_HOLDINGS_COUNT = "{0}종목";
        public static string TEXT_HOLDING_SUB = "평균 {0:N1} · 평가 {1:N1}";
        public static string TEXT_INFO_COUNT = "{0}건";
        public static string TEXT_MARKET_PHASE_HALTED_SHORT = "휴장";
    }
}
