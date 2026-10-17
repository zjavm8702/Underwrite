namespace Underwrite.Global
{
    /// <summary>
    /// 할 일 목록(QuestLog). 순서가 곧 이야기의 순서다. 웹툰 6화 뒤부터 첫 주기의 귀환까지.
    /// 힌트는 어디로 가서 무엇을 누르면 되는지까지 적는다(2026-10-17 사용자 요청).
    /// </summary>
    public static partial class StringConstants
    {
        public static readonly string[] QUEST_TITLES =
        {
            "먹고살 길",
            "창고 아래",
            "첫 거래",
            "단골 테이블",
            "엿들은 사정",
            "이름 없는 의뢰주",
            "새로 온 사람들",
            "쉬는 날",
            "돌아온 사람들"
        };

        public static readonly string[] QUEST_HINTS =
        {
            "여관 문 앞에서 E → 「주방 일을 거든다」",
            "장이 열릴 때(오후 9시) 여관 → 「창고로 내려간다」. 메뉴 → 시간 넘기기로 기다릴 수 있다",
            "지하거래소에서 아무 종목이나 산다 — 「매수」",
            "여관 → 「용사단 테이블로 간다」 → 한 사람에게 말을 건다",
            "여관 → 「주방 일을 거든다」. 술을 나르다 그들의 이야기를 듣는다",
            "길드 기록실 → 「익명 의뢰를 낸다」. 들은 사정에 맞는 의뢰를 고른다",
            "모험단 하나에 대해 두 가지를 알아낸다 — 직접 말 걸기, 길드·대장간·여관에서 묻기",
            "영주 시찰일(4일째), 여관 → 「용사단과 쉬는 날을 보낸다」",
            "7일째, 원정에서 돌아온 용사단을 여관에서 맞는다"
        };

        public static string TEXT_QUEST_LABEL = "할 일 · {0}";
        public static string TEXT_QUEST_DONE = "할 일을 마쳤다 — {0}";
        public static string TEXT_QUEST_DONE_REWARD = "할 일을 마쳤다 — {0} · {1:N0}을 받았다";
        public static string TEXT_QUEST_TAB = "할 일";
        public static string TEXT_QUEST_ALL_DONE = "지금은 할 일이 없다.";
    }
}
