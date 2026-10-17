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
            new[] { "젊은 검사가 이끄는 신생 단. 고난도 유적 의뢰를 노린다고 했다.", "등록한 지 석 달. 완수 기록 두 건, 둘 다 근교 의뢰." },
            new[] { "말수 적은 노기사가 이끄는 오래된 단. 이 영지에서 오래 지냈다.", "등록 십이 년. 완수 기록 백 건이 넘고, 실패는 손에 꼽는다." },
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

        // 대장간 사정이 주기 끝에 드러날 때의 소식 [종목][주기 순번]. 효과가 0이면 쓰지 않는다
        public static readonly string[][] CODEX_SMITH_OUTCOME =
        {
            new[] { "{0}{이} 이 빠진 검으로 의뢰를 반쯤 하다 돌아왔다.", "{0}{이} 새 경갑 덕에 다치지 않고 의뢰를 마쳤다.", "{0}의 장비가 낡아 의뢰 하나를 미뤘다." },
            new[] { "{0}{이} 평소처럼 의뢰를 마쳤다.", "{0}{이} 새 방패로 호위 의뢰를 무사히 마쳤다.", "{0}{이} 장비를 팔고 의뢰를 줄였다." },
            new[] { "{0}{이} 방패가 모자라 퇴각했다.", "{0}{이} 깊은 유적에서 큰 전리품을 가져왔다.", "{0}의 장비가 버티지 못해 의뢰를 접었다." },
            new[] { "{0}{이} 첫 의뢰를 탈 없이 마쳤다.", "{0}{이} 새 창으로 이 영지 몬스터를 처리했다.", "" },
            new[] { "{0}{이} 좋은 장비로 의뢰를 깔끔히 마쳤다.", "{0}{이} 먼 원정에서 성과를 들고 왔다.", "{0}{이} 평소처럼 의뢰를 마쳤다." }
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

        // 여관의 말이 맞았을 때의 소식 [종목][주기 순번]
        public static readonly string[][] CODEX_TAVERN_OUTCOME =
        {
            new[] { "{0}{이} 이름난 기사와 함께 큰 의뢰를 마쳤다.", "{0}{이} 술값 빚 때문에 싼 의뢰만 받았다.", "{0}{이} 유적에서 찾은 물건을 길드에 넘겼다." },
            new[] { "{0}{이} 은퇴를 길드에 알렸다.", "{0}에 젊은 단원이 들어 의뢰를 늘렸다.", "{0}{이} 다친 몸으로 의뢰 하나를 포기했다." },
            new[] { "{0}{이} 단원 둘을 잃었다는 것이 길드 기록으로 확인됐다.", "{0}{이} 큰 의뢰를 따내 마쳤다.", "{0} 단장이 빚 때문에 장비를 저당 잡혔다." },
            new[] { "{0} 대장의 빚이 길드에 알려져 의뢰가 줄었다.", "{0}{이} 길드와 다퉈 의뢰 배정이 밀렸다.", "{0}{이} 큰 일을 맡아 마쳤다." },
            new[] { "{0} 단장이 귀족 가문의 후원을 받았다.", "{0} 일부가 다른 영지로 떠났다.", "{0}{이} 둘로 갈라섰다." }
        };

        public static string TEXT_CODEX_NOTED = "(수첩에 적은 일) ";
        public static string TEXT_CODEX_RUMOR_FALSE = "헛말로 드러났다 — {0}: 「{1}」";

        public static readonly string[] CODEX_HINTS =
        {
            "??? — 그 단에게 직접 말을 걸면 알 수 있다",
            "??? — 길드 기록실에서 물으면 알 수 있다",
            "??? — 이번 주기 장비 사정. 대장간에서 물으면 알 수 있다",
            "??? — 이번 주기에 도는 말. 여관에서 듣게 된다"
        };

        // 새 용병단 이름 = 앞말 + " " + 뒷말 + 꼬리. 이름 조각이라 배열로 둔다(언어 설정 때 함께 옮긴다).
        public static readonly string[] BAND_NAME_FIRST =
        {
            "푸른", "녹슨", "서리", "잿빛", "황금", "외눈", "가시", "새벽", "그림자", "들불", "돌", "바람"
        };

        public static readonly string[] BAND_NAME_SECOND =
        {
            "늑대", "까마귀", "수리", "여우", "곰", "방패", "망치", "횃불", "사슴", "뿔", "발톱", "올빼미"
        };

        public static readonly string[] BAND_NAME_TAIL = { "단", "단", "단", "용병단" };

        // 영지를 걷는 파티 단원. {0}은 단 이름. 앞에서부터 단장·궁수·방패병·짐꾼 자리
        public static string TEXT_PARTY_MEMBER_1 = "{0} 단장";
        public static string TEXT_PARTY_MEMBER_2 = "{0} 궁수";
        public static string TEXT_PARTY_MEMBER_3 = "{0} 방패병";
        public static string TEXT_PARTY_MEMBER_4 = "{0} 짐꾼";
        public static string TEXT_PARTY_LINE_1 = "길 막지 마쇼.";
        public static string TEXT_PARTY_LINE_2 = "이번 의뢰만 끝나면 한몫 잡는다.";
        public static string TEXT_PARTY_LINE_3 = "…뭘 그렇게 봐?";
        public static string TEXT_PARTY_LINE_4 = "여관 스튜가 그립군.";

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
