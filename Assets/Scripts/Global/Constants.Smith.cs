namespace Underwrite.Global
{
    /// <summary>
    /// 주문서 읽기(대장간). 셋째 정보 모으기(2026-10-18). 여관에서 들은 소문이 맞는지 주문서로 확인한다.
    /// 엿듣기(여관)로 듣고 여기서 가린다 — 두 정보 모으기가 짝이다. 문구는 과장 초안 — 사장님 검토 대기.
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_SMITH_CHOICE = "주문서 꽂이를 들여다본다";
        public static string TEXT_SMITH_CHOICE_NOTE = "{0}분 · 여관 소문이 맞는지 주문서로 가린다";
        public static string TEXT_SMITH_NONE = "이번 계절 소문은 더 가려 볼 것이 없다";
        public static string TEXT_SMITH_DONE_TODAY = "오늘은 이미 꽂이를 봤다";

        public static string TEXT_SMITH_TITLE = "대장간 · 주문서 꽂이";
        public static string TEXT_SMITH_RUMOR = "여관에서 도는 말 — {0}: 「{1}」";
        public static string TEXT_SMITH_HINT = "그 단의 주문 가운데 이 말과 이어지는 것을 짚는다. 지난 계절 주문과 취소된 주문은 이번 일을 말해 주지 않는다.";
        public static string TEXT_SMITH_DAY = "{0}일째";
        public static string TEXT_SMITH_LAST_SEASON = "지난 계절";
        public static string TEXT_SMITH_SUSPICION = "대장장이의 눈길 {0} / {1}";
        public static string TEXT_SMITH_WRONG_BAND = "다른 단의 주문이다. 대장장이가 망치질을 멈춘다.";
        public static string TEXT_SMITH_WRONG_OLD = "지난 계절 주문이다 — 이번 말과는 상관없다. 대장장이가 이쪽을 본다.";
        public static string TEXT_SMITH_WRONG_CANCELLED = "취소된 주문이다 — 아무것도 말해 주지 않는다. 대장장이가 이쪽을 본다.";
        public static string TEXT_SMITH_KICKED = "「남의 주문서를 왜 그렇게 뒤져?」 대장간에서 쫓겨났다.";
        public static string TEXT_SMITH_LEAVE = "그만둔다";

        public static string TEXT_SMITH_TAG = "주문서 읽기";
        public static string TEXT_SMITH_FOUND = "{0}의 이번 계절 주문 — 「{1}」";
        public static string TEXT_SMITH_ASK = "여관의 말은 「{0}」 — 맞는 말일까.";
        public static string TEXT_SMITH_VERDICT_TRUE = "주문서가 말을 받쳐 준다 — 맞는 말이다";
        public static string TEXT_SMITH_VERDICT_FALSE = "주문서가 말과 어긋난다 — 헛말이다";
        public static string TEXT_SMITH_RIGHT_TRUE = "앞뒤가 맞는다. {0}에 대한 여관의 말은 맞는 말이다.";
        public static string TEXT_SMITH_RIGHT_FALSE = "앞뒤가 맞는다. {0}에 대한 여관의 말은 헛말이다.";
        public static string TEXT_SMITH_WRONG_GUESS = "다시 읽어 보니 확신이 서지 않는다. 대장장이가 꽂이를 치웠다.";

        // 연습 꽂이(프롤로그 대장간). 한 상단의 주문 — 지난 계절 · 다른 무리 · 취소 · 이번 계절(정답) 순.
        public static string TEXT_SMITH_TUT_TARGET = "붉은 망치 상단";
        public static string TEXT_SMITH_TUT_TASK = "대장장이: 「붉은 망치 상단 주문 가운데 오늘 만들 걸 짚어 줘. 지난 계절 것, 취소된 건 빼고.」";
        public static string TEXT_SMITH_TUT_HINT = "주문서마다 날짜와 셈 상태가 적혀 있다. 지난 계절 것과 「취소」 도장이 찍힌 것은 오늘 일이 아니다.";
        public static string TEXT_SMITH_TUT_DONE = "그거야. 지난 계절 것도, 취소된 것도 오늘 일이 아니지.";
        public static readonly string[] TEXT_SMITH_TUT_ITEMS =
        {
            "장검 둘 벼리기",
            "편자 넷 갈기",
            "투구 셋 수선",
            "사슬 갑옷 고리 땜질"
        };

        // 주문서의 셈 상태. 마지막 칸이 취소다(SmithSlip.Status).
        public static readonly string[] TEXT_SMITH_STATUS = { "완납", "선금", "외상", "취소" };

        /// <summary>
        /// 여관 소문(<see cref="CODEX_TAVERN"/>)과 짝을 이루는 주문 [종목][주기 순번][0=말이 맞을 때, 1=헛말일 때].
        /// 말이 맞으면 이번 계절 주문서에 0번이, 헛말이면 1번이 꽂힌다. 반대쪽은 미끼(지난 계절·취소)로 쓴다.
        /// 공략처럼 읽히지 않게 물건과 대장장이가 적어 둔 한마디만 쓴다. 해석은 플레이어 몫.
        /// </summary>
        public static readonly string[][][] SMITH_EVIDENCE =
        {
            new[]
            {
                new[] { "기사용 마구 한 벌 — 동행할 분 몫이라며", "늘 네 사람 몫 그대로. 더 맞출 사람은 없다고" },
                new[] { "칼 수리비 외상 — 여관 빚부터 갚고 주겠다며", "숫돌 값을 은화로 바로 치렀다" },
                new[] { "낯선 금속 조각 감정 — 유적에서 나왔다며", "유적용 갈고리 반품 — 들어가 보지도 못했다며" }
            },
            new[]
            {
                new[] { "갑옷을 되사 줄 수 있느냐는 문의", "다음 계절 몫 화살촉 백 개를 미리 주문" },
                new[] { "작은 치수 경갑 한 벌 — 새로 든 아이 몫", "늘 쓰던 치수 그대로, 사람 수도 그대로" },
                new[] { "어깨 보호대를 덧대 달라 — 다친 쪽이라며", "무거운 대검을 새로 벼려 달라" }
            },
            new[]
            {
                new[] { "갑옷 두 벌을 녹여 달라 — 입을 사람이 없다며", "여섯 사람 몫 투구, 지난번과 같은 수" },
                new[] { "길드 선금으로 장비 값을 한 번에 치렀다", "값싼 땜질만 — 일감이 없다며" },
                new[] { "단장의 칼을 담보로 맡겼다", "단장이 금장 칼집을 새로 맞췄다. 현금으로" }
            },
            new[]
            {
                new[] { "수리비를 다음 원정 뒤로 미뤄 달라", "창 열 자루 값을 선금으로 다 냈다" },
                new[] { "방패의 길드 표식을 지워 달라", "길드 의뢰용 표식 방패 넷" },
                new[] { "공성용 쇠갈고리 여덟 — 급하다며", "숫돌 하나만. 당분간 쉰다며" }
            },
            new[]
            {
                new[] { "칼자루에 가문 문장을 새겨 달라", "문장 대신 기사단 표식만 — 집안 같은 건 없다며" },
                new[] { "먼 길용 편자 열두 벌", "이 영지 마물용 창 — 오래 쓸 거라며" },
                new[] { "같은 문장 방패를 두 색으로 따로 칠해 달라", "단원 모두 같은 망토 핀, 한 번에" }
            }
        };

        // 다른 단의 주문서. 소문과 상관없는 평범한 일감이다.
        public static readonly string[] TEXT_SMITH_NOISE_ITEMS =
        {
            "편자 넷 갈기",
            "칼날 갈기 셋",
            "가죽끈 수선",
            "화살촉 서른",
            "찌그러진 투구 펴기",
            "방패 테 갈기",
            "사슬 갑옷 고리 땜질",
            "단검 두 자루"
        };
    }
}
