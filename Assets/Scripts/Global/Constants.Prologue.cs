namespace Underwrite.Global
{
    /// <summary>
    /// 프롤로그 — 지하를 찾기 전의 마을 생활(2026-10-18 온보딩 설계안). 문구는 과장 초안 — 사장님 검토 대기.
    /// 마을 사람은 지하시장을 모른다(사장님 지적 8). 모두 동네 일거리로 말한다.
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_PROLOGUE_DAY7 = "일곱째 날";
        public static string TEXT_PROLOGUE_DAY8 = "여덟째 날";
        public static string TEXT_PROLOGUE_AFTERNOON = "오후";
        public static string TEXT_PROLOGUE_EVENING = "저녁";
        public static string TEXT_PROLOGUE_CLOCK = "{0} · {1}";

        // 화면 위 목표. 단계마다 바뀐다.
        public static string TEXT_PROLOGUE_GOAL = "살아남기 — 오늘 번 돈 {0} / {1}";
        public static string TEXT_PROLOGUE_GOAL_NEXT = "\n<size=15>다음: {0}</size>";
        public static string TEXT_PROLOGUE_GOAL_SLEEP = "\n<size=15>해가 졌다. 여관 다락방에서 잔다</size>";

        // 첫 혼잣말(일곱째 날). 웹툰 4장 「내가 아는 건 10년 뒤의 멸망뿐」을 게임 안에서 다시 짚는다.
        // 「일곱째 날」은 제목 카드가 먼저 보인다.
        public static readonly string[] TEXT_PROLOGUE_OPENING =
        {
            "내가 아는 건 10년 뒤의 멸망뿐이다. 그걸 아는 내가, 오늘 저녁 먹을 돈도 없다.",
            "…그 전에, 오늘 먹을 것부터."
        };

        public static string TEXT_PROLOGUE_CALL = "거기 젊은이! 일 찾는 거면 마을을 한 바퀴 돌아봐. 손 필요한 데가 있을 거야. 우리 집부터 들르든가.";
        public static string TEXT_PROLOGUE_GO_FIRST = "먼저 {0}에 들러 보자.";
        public static string TEXT_PROLOGUE_GO_INN = "해가 졌다. 여관으로 돌아가자.";
        public static string TEXT_PROLOGUE_KITCHEN_ONLY = "오늘은 여관 주방 일을 거들기로 했다.";
        public static string TEXT_PROLOGUE_WAGE = "품삯 {0:N0}을 받았다.";

        // 순찰 정거장마다: 주인의 소개 → (맛보기) → 마무리. 순서는 여관 · 대장간 · 길드 · 게시판 · 영주관.
        public static readonly string[] TEXT_PROLOGUE_STOP_INTRO =
        {
            "우리 집은 술 나르는 손이 늘 모자라. 손님들 주사위 한 판 받아 주는 것도 일이고.",
            "주문서 정리할 줄 알아? 취소된 거 골라내는 게 일의 반이야.",
            "기록실은 늘 종이가 흩어져 있지. 맞춰 주면 품삯은 주네.",
            "요즘 모험단이 부쩍 많아졌어. 저 단들 이름은 알아 둬서 손해 볼 거 없지.",
            "영주관에 무슨 볼일이지? 서신 심부름은 믿을 만한 사람에게만 맡긴다."
        };

        public static readonly string[] TEXT_PROLOGUE_STOP_OUTRO =
        {
            "옜다, 품삯. 저녁엔 다락방에서 자도 돼. 다른 데도 돌아봐.",
            "제법이네. 손 빌릴 일 있으면 또 와.",
            "…꼼꼼하군. 다음에도 부탁하지.",
            "…낯익은 이름이 섞여 있는 것 같기도 하다.",
            "언젠가는 저 문 안으로 들어갈 일이 있겠지."
        };

        // 정거장마다 마무리 말을 누가 하는가 — 게시판·영주관은 주인공의 혼잣말이다.
        public static readonly bool[] TEXT_PROLOGUE_OUTRO_IS_SELF = { false, false, false, true, true };

        public static string TEXT_PROLOGUE_SLEEP_OWNER = "오늘 고생했어. 다락방 써. 내일 아침엔 부엌 좀 거들고.";
        public static string TEXT_PROLOGUE_SLEEP_SELF = "번 돈 {0:N0}. …하루는 버텼다.";
        // 장면 전환 제목 카드.
        public static string TEXT_PROLOGUE_CARD_DAY7_SUB = "흘러든 영지, 아무것도 없는 오후";
        public static string TEXT_PROLOGUE_CARD_DAY8_SUB = "다락방에서 맞는 아침";
        public static string TEXT_PROLOGUE_CARD_DUSK = "해 질 녘";
        public static string TEXT_PROLOGUE_CARD_DUSK_SUB = "주방 일을 마칠 무렵";

        public static string TEXT_PROLOGUE_TUT_MOVE_TITLE = "영지";
        public static string TEXT_PROLOGUE_TUT_MOVE_BODY = "가진 것 하나 없이 떨어진 영지. WASD나 방향키로 걷고, 문 앞에서 E를 누른다. 마우스 휠로 가까이 볼 수 있다.";
        public static string TEXT_PROLOGUE_TUT_GOAL_TITLE = "목표";
        public static string TEXT_PROLOGUE_TUT_GOAL_BODY = "왼쪽 위에 지금 할 일이 늘 보인다. 반짝이는 이름표가 다음에 갈 곳이다. 오늘은 시간이 흐르지 않는다 — 천천히 둘러봐도 된다.";
    }
}
