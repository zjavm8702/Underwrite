namespace Underwrite.Global
{
    /// <summary>
    /// 첫 지하 방문 튜토리얼(2026-10-18 사장님 — 시간을 멈추고, 게시판 · 주식 사고팔기 · 지정가 · 정보상 · 정보를 차례로).
    /// 문구는 과장 초안 — 사장님 검토 대기.
    /// </summary>
    public static partial class StringConstants
    {
        // 지하 방 출구(계단 대신 벽의 문, 2026-10-18).
        public static string TEXT_ROOM_EXIT = "여관 위층으로";
        public static string TEXT_ROOM_EXIT_KEY = "E · 올라간다";
        public static string TEXT_ROOM_EXIT_LOCKED = "장이 끝나야 올라갈 수 있다";

        // 1. 도착 — 정보상의 첫인사와 시장 게시판 소개.
        public static string TEXT_MTUT_TAG = "처음 온 손님";
        public static readonly string[] TEXT_MTUT_ARRIVE =
        {
            "…술통 나르던 일꾼이군. 여기까지 내려온 걸 보니 들은 게 있나 보지.",
            "여긴 지하거래소. 사고파는 건 물건이 아니라 모험단이야 — 그들이 의뢰에 성공할지, 실패할지.",
            "벽의 칠판이 시장 게시판이다. 단마다 지금 값과 오늘 오르내린 폭이 적혀 있지.",
            "칠판 앞에 서 봐. 모르는 건 내가 봐줄 테니, 우선 조금만 사 보는 게 어때?"
        };

        public static string TEXT_MTUT_COACH_BOARD = "시장 게시판(칠판) 앞으로 가서 E — 거래판을 연다";
        public static string TEXT_MTUT_COACH_BUY = "첫 거래 — 종목을 하나 골라 1주 산다 · 「매수」 단추를 누른다";
        public static string TEXT_MTUT_COACH_SELL = "이제 판다 — 산 1주를 「매도」. 연습이라 오르내린 차이는 정보상이 메워 준다";
        public static string TEXT_MTUT_COACH_FLOW = "시간이 흐르기 시작했다. 정보 타임이 오면 정보상이 부른다";
        public static string TEXT_MTUT_COACH_AFTER = "소식을 봤으면 다시 판단한다 — 오를 것 같으면 더 사고, 내릴 것 같으면 판다";

        // 2. 거래판 — 처음 열었을 때 짚어 주는 순서.
        public static string TEXT_MTUT_1_TITLE = "거래 종목";
        public static string TEXT_MTUT_1_BODY = "지금 사고팔 수 있는 단들이다. 하나를 누르면 그 단의 시세와 호가가 가운데에 뜬다. 맨 아래 「새벽별 용사단」은 용사단 지수 — 천천히 꾸준히 오르지만 그들이 무너지면 같이 무너진다.";
        public static string TEXT_MTUT_2_TITLE = "시세";
        public static string TEXT_MTUT_2_BODY = "이번 장의 값 흐름이다. 소식이 들어오면 값이 그쪽으로 움직인다. 시작·고가·저가가 표시된다.";
        public static string TEXT_MTUT_3_TITLE = "호가";
        public static string TEXT_MTUT_3_BODY = "위 칸은 팔겠다는 사람, 아래 칸은 사겠다는 사람이다. 한꺼번에 많이 사면 위 칸을 차례로 먹어 올라가서 값이 비싸진다.";
        public static string TEXT_MTUT_4_TITLE = "매수 · 매도";
        public static string TEXT_MTUT_4_BODY = "매수는 산다, 매도는 판다. 오를 것 같으면 사 두었다가 오른 뒤에 판다 — 그 차이가 번 돈이다.";
        public static string TEXT_MTUT_5_TITLE = "시장가 · 지정가";
        public static string TEXT_MTUT_5_BODY = "시장가는 지금 바로 체결된다. 지정가는 「이 값이 되면」 걸어 두는 주문이다 — 지정가 매수는 값이 그만큼 내려오면 사고, 지정가 매도는 값이 그만큼 오르면 판다. 장이 끝나면 걸어 둔 주문은 사라진다.";
        public static string TEXT_MTUT_6_TITLE = "수량";
        public static string TEXT_MTUT_6_BODY = "몇 주를 사고팔지 정한다. 비율 단추는 가진 돈(팔 때는 가진 주식)의 몇 할인지로 채운다.";
        public static string TEXT_MTUT_7_TITLE = "주문";
        public static string TEXT_MTUT_7_BODY = "체결될 값과 총액을 확인하고 누른다. 많이 살수록 체결 값이 지금 값보다 높아질 수 있다.";
        public static string TEXT_MTUT_8_TITLE = "자산";
        public static string TEXT_MTUT_8_BODY = "현금과 가진 주식의 값을 합친 것. 영지에서 번 돈도 여기 들어온다. 용사단을 도울 돈도 여기서 나간다.";

        // 3. 첫 정보 타임 — 정보상 소개.
        public static readonly string[] TEXT_MTUT_INFORMANT =
        {
            "새 소식이 들어왔다. 다들 귀 기울여 봐.",
            "나는 이 거래소의 정보상. 모험단 소식을 모아 팔고, 판을 돌린다. 이름은 묻지 마.",
            "소식에는 무게가 다르다 — 원정 결과가 확정된 소식은 값을 크게 움직이고, 소문은 틀릴 수도 있다.",
            "다들 같은 걸 같이 듣는다. 남보다 앞서려면 장 사이에 영지에서 먼저 캐 와야 해. 자, 오늘 소식이다."
        };

        // 4. 소식을 본 뒤 — 다시 거래하는 법.
        public static string TEXT_MTUT_9_TITLE = "정보 기록";
        public static string TEXT_MTUT_9_BODY = "방금 들은 소식이 여기 쌓인다. 「내 종목」 탭은 가진 단의 소식만 모아 준다.";
        public static string TEXT_MTUT_10_TITLE = "다시 판단한다";
        public static string TEXT_MTUT_10_BODY = "좋은 소식이면 값이 오르기 전에 더 사고, 나쁜 소식이면 떨어지기 전에 판다. 「매도」를 누르고 수량을 정하면 가진 주식을 판다.";
        public static string TEXT_MTUT_11_TITLE = "지정가로 기다리기";
        public static string TEXT_MTUT_11_BODY = "지금 팔기 아까우면 지정가 매도로 원하는 값에 걸어 둔다. 호가 칸을 누르면 그 값이 지정가로 들어간다.";
        public static string TEXT_MTUT_12_TITLE = "장 끝내기";
        public static string TEXT_MTUT_12_BODY = "장이 끝나기 전에는 지하에서 나갈 수 없다. 「장 끝내기」로 남은 시간을 넘길 수 있다 — 끝나면 영지로 올라간다.";
    }
}
