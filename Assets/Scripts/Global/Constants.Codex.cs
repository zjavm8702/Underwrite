namespace Underwrite.Global
{
    /// <summary>
    /// 모험단 도감의 내용. 단마다 네 줄 — 직접 말을 걸어, 길드에서, 대장간에서, 여관에서 알 수 있다.
    /// 순서는 <see cref="Enumeration.CodexSource"/>와 같다. 플레이어가 조사한 만큼만 보인다(2026-10-17 사용자 요청).
    /// </summary>
    public static partial class StringConstants
    {
        public static readonly string[] CODEX_STOCK_IDS =
        {
            "STOCK_ROOKIE", "STOCK_VETERAN", "STOCK_HIGH_RISK", "STOCK_WARDEN", "STOCK_LANTERN"
        };

        // [종목][출처] — 말 / 길드 / 대장간 / 여관
        public static readonly string[][] CODEX_FACTS =
        {
            new[]
            {
                "혼자 다니는 검사. 고난도 유적 의뢰를 노린다.",
                "등록한 지 석 달, 완수 기록이 거의 없다 — 신입이다. 소문에 크게 흔들린다.",
                "검을 두 번이나 갈았다. 아끼는 장비가 없다.",
                "유명 기사와 엮였다는 소문을 즐기는 눈치다."
            },
            new[]
            {
                "말수 적은 노기사.",
                "완수 기록이 길다 — 베테랑이다. 크게 오르지도 내리지도 않는다.",
                "갑옷 손질을 한 번도 거른 적이 없다.",
                "곧 은퇴한다는 말이 돈다."
            },
            new[]
            {
                "단장은 위험한 일일수록 보수가 좋다고 믿는다.",
                "고위험 의뢰만 맡는다 — 성공하면 크게, 실패하면 전부를 잃는다.",
                "방패가 늘 모자라다. 수리 주문이 밀려 있다.",
                "지난 원정에서 둘을 잃었다는 소문."
            },
            new[]
            {
                "북쪽 국경 경비 출신. 이 영지는 처음이다.",
                "이 영지 실적이 없다 — 상장되면 신입으로 거래된다.",
                "장비는 낡았지만 손질은 잘 돼 있다.",
                "대장이 빚을 지고 왔다는 말이 있다."
            },
            new[]
            {
                "북쪽 영지에서 옮겨 왔다. 기사단다운 규율이 있다.",
                "다른 영지의 실적이 길다 — 베테랑으로 상장된다.",
                "좋은 장비를 맞추는 데 돈을 아끼지 않는다.",
                "단장이 귀족 출신이라는 소문."
            }
        };

        public static readonly string[] CODEX_HINTS =
        {
            "??? — 그 단에게 직접 말을 걸면 알 수 있다",
            "??? — 길드 기록실에서 물으면 알 수 있다",
            "??? — 대장간에서 물으면 알 수 있다",
            "??? — 여관에서 듣게 된다"
        };

        public static string TEXT_CODEX_TAB = "모험단";
        public static string TEXT_CODEX_KNOWN = "알아낸 것 {0} / {1}";
        public static string TEXT_CODEX_LEARNED = "수첩에 적었다 — {0}";
        public static string TEXT_CODEX_LEARNED_LINE = "{0}: {1}";
        public static string TEXT_CODEX_STAGE_NOT_ARRIVED = "아직 영지에 없다";
        public static string TEXT_CODEX_STAGE_LEFT = "떠났다";
        public static string TEXT_NOTEBOOK = "수첩";
        public static string TEXT_NOTEBOOK_NEWS = "들은 소식";
        public static string TEXT_NOTEBOOK_CLOSE = "닫기";
        public static string TEXT_NOTEBOOK_EMPTY = "이번 장 사이에 들은 소식이 아직 없다.";
    }
}
