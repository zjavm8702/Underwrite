namespace Underwrite.Global
{
    /// <summary>
    /// 모험단 도감의 내용(2026-10-17 사용자 결정으로 다시 짰다).
    ///
    /// - 공략을 적지 않는다. "신입이다, 크게 흔들린다" 대신 "등록 석 달, 완수 두 건"처럼 사실만. 해석은 플레이어 몫
    /// - 변하지 않는 것(출신·성향·기록)은 한 번 알면 남는다 — 말 걸기, 길드
    /// - 변하는 것(장비 사정·돌아다니는 말)은 주기마다 바뀌어 새로 캐야 한다 — 대장간, 여관
    /// - 여관에서 들은 말은 소문이라 틀릴 수 있다(「(소문)」을 붙인다). 길드·대장간이 더 확실하지만 비싸다.
    ///   어떤 소문이 맞았는지를 시장과 잇는 것은 다음 단계다
    ///
    /// 순서는 <see cref="Enumeration.CodexSource"/>와 같다. 주기 문장은 주기 번호로 돌아가며 고른다.
    /// </summary>
    public static partial class StringConstants
    {
        public static readonly string[] CODEX_STOCK_IDS =
        {
            "STOCK_ROOKIE", "STOCK_VETERAN", "STOCK_HIGH_RISK", "STOCK_WARDEN", "STOCK_LANTERN"
        };

        // 변하지 않는 것 [종목][0=말 걸기, 1=길드]
        public static readonly string[][] CODEX_FIXED =
        {
            new[] { "혼자 다니는 검사. 고난도 유적 의뢰를 노린다고 했다.", "등록한 지 석 달. 완수 기록 두 건, 둘 다 근교 의뢰." },
            new[] { "말수 적은 노기사. 이 영지에서 오래 지냈다.", "등록 십이 년. 완수 기록 백 건이 넘고, 실패는 손에 꼽는다." },
            new[] { "단장은 위험한 일일수록 보수가 좋다고 믿는다.", "등록 이 년. 맡은 의뢰가 전부 위험 등급 상(上). 완수 반, 실패 반." },
            new[] { "북쪽 국경 경비 출신. 이 영지는 처음이라고 했다.", "이 영지 기록 없음. 북쪽 영지 길드에서 보낸 소개장이 있다." },
            new[] { "북쪽 영지에서 옮겨 왔다. 기사단다운 규율이 보인다.", "다른 영지에서 넘어온 기록: 등록 칠 년, 완수 여든 건." }
        };

        // 대장간에서 아는 이번 주기의 장비 사정 [종목][주기 순번]
        public static readonly string[][] CODEX_SMITH =
        {
            new[] { "검을 두 번이나 갈았다. 여벌 장비가 없다.", "새 경갑을 맞췄다. 값은 외상.", "주문이 없다. 장비를 손보지 않는다." },
            new[] { "갑옷 손질을 거르지 않는다. 이번에도 평소대로.", "방패 하나를 새로 맞췄다.", "은퇴를 앞두고 장비를 판다는 문의가 왔다." },
            new[] { "방패가 모자라 수리 주문이 밀려 있다.", "갈고리와 밧줄을 잔뜩 사 갔다. 깊이 들어갈 셈이다.", "수리비가 석 달째 밀렸다." },
            new[] { "장비는 낡았지만 손질은 잘 돼 있다.", "이 영지 몬스터에 맞는 창을 새로 주문했다.", "주문이 없다." },
            new[] { "좋은 장비를 맞추는 데 돈을 아끼지 않는다.", "말 편자를 갈았다. 먼 원정이다.", "평소대로 손질만." }
        };

        // 여관에서 도는 이번 주기의 말 [종목][주기 순번]
        public static readonly string[][] CODEX_TAVERN =
        {
            new[] { "유명 기사와 동행한다는 말이 돈다.", "외상 술값이 쌓였다는 말.", "유적에서 무언가를 찾았다는 말." },
            new[] { "곧 은퇴한다는 말이 돈다.", "젊은 단원을 들인다는 말.", "다친 데가 있다는 말." },
            new[] { "지난 원정에서 둘을 잃었다는 말.", "큰 의뢰를 따냈다는 말.", "단장이 빚쟁이에게 쫓긴다는 말." },
            new[] { "대장이 빚을 지고 왔다는 말이 있다.", "길드와 다퉜다는 말.", "곧 큰 일을 맡는다는 말." },
            new[] { "단장이 귀족 출신이라는 말.", "다른 영지로 다시 떠난다는 말.", "기사단 둘이 갈라섰다는 말." }
        };


        public static readonly string[] CODEX_HINTS =
        {
            "??? — 그 단에게 직접 말을 걸면 알 수 있다",
            "??? — 길드 기록실에서 물으면 알 수 있다",
            "??? — 이번 주기 장비 사정. 대장간에서 물으면 알 수 있다",
            "??? — 이번 주기에 도는 말. 여관에서 듣게 된다"
        };

        public static string TEXT_CODEX_RUMOR = "(소문) ";
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
