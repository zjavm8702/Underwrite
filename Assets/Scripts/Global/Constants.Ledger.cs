namespace Underwrite.Global
{
    /// <summary>
    /// 장부 맞추기(길드 기록실). 첫 추리형 정보 모으기(2026-10-18). 문구는 과장 초안 — 사장님 검토 대기.
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_LEDGER_CHOICE = "기록실 장부를 맞춰 본다";
        public static string TEXT_LEDGER_CHOICE_NOTE = "{0}분 · 어긋난 줄을 찾으면 확실한 사실";
        public static string TEXT_LEDGER_NONE = "오늘은 맞춰 볼 장부가 없다";
        public static string TEXT_LEDGER_DONE_TODAY = "오늘은 이미 장부를 봤다";

        public static string TEXT_LEDGER_TITLE = "길드 기록실 · 의뢰와 지급";
        public static string TEXT_LEDGER_HINT = "약정한 보수와 실제로 나간 돈이 맞지 않는 단이 하나 있다. 그 단의 의뢰를 짚는다.";
        public static string TEXT_LEDGER_REQUESTS = "의뢰 기록";
        public static string TEXT_LEDGER_PAYMENTS = "지급 기록";
        public static string TEXT_LEDGER_REQUEST_HEAD = "단 · 의뢰 · 약정 보수";
        public static string TEXT_LEDGER_PAYMENT_HEAD = "날짜 · 받은 단 · 금액 · 비고";
        public static string TEXT_LEDGER_REQUEST_ROW = "{0}\n<size=16>{1}</size>";
        public static string TEXT_LEDGER_PAYMENT_DAY = "{0}일째";
        public static string TEXT_LEDGER_SUSPICION = "기록 담당의 눈길 {0} / {1}";
        public static string TEXT_LEDGER_WRONG = "더해 보니 맞아떨어진다. 기록 담당이 고개를 든다.";
        public static string TEXT_LEDGER_KICKED = "「거기서 뭘 그렇게 뒤지나?」 기록실에서 쫓겨났다.";
        public static string TEXT_LEDGER_LEAVE = "그만둔다";

        public static string TEXT_LEDGER_TAG = "장부 맞추기";
        public static string TEXT_LEDGER_FOUND = "{0}. 약정 {1:N0}, 실제로 나간 돈 {2:N0}.";
        public static string TEXT_LEDGER_CLUE_GAIN = "잔금 칸 옆에 작은 글씨가 있다. 「대장간 쪽으로」.";
        public static string TEXT_LEDGER_CLUE_LOSS = "잔금 칸 옆에 작은 글씨가 있다. 「일부 보류 — 사유 적지 않음」.";
        public static string TEXT_LEDGER_ASK = "무슨 뜻일까.";

        // 첫 칸이 맞는 답이다. 화면에는 섞어서 보인다.
        public static readonly string[] TEXT_LEDGER_GAIN_OPTIONS =
        {
            "누군가 이 단의 장비 값을 몰래 대고 있다 — 큰 원정을 준비한다",
            "길드가 보수를 두 번 내줬다 — 적는 사람의 실수다",
            "빚을 갚으려고 미리 당겨 받았다 — 쪼들린다"
        };

        public static readonly string[] TEXT_LEDGER_LOSS_OPTIONS =
        {
            "보수가 묶였다 — 지난 일에서 뭔가 틀어졌다. 준비가 어렵다",
            "아직 덜 적었을 뿐이다",
            "나눠 받기로 했다 — 여유가 있다"
        };

        public static string TEXT_LEDGER_RIGHT = "앞뒤가 맞는다. {0}에 대해 확실히 알았다 — {1}";
        public static string TEXT_LEDGER_WRONG_GUESS = "짐작은 섰지만 확신이 없다. 기록 담당이 장부를 덮었다.";

        // 장부를 채우는 이름 없는 무리와 의뢰 이름, 지급 비고(선금·중도금·잔금·완수 순).
        public static readonly string[] TEXT_LEDGER_FILLER_BANDS = { "떠돌이 사냥꾼 셋", "남쪽 상단 호위대", "늪지 약초꾼 무리" };

        public static readonly string[] TEXT_LEDGER_JOBS =
        {
            "북쪽 고개 마물 퇴치",
            "상단 호위",
            "늪지 정찰",
            "폐광 수색",
            "국경 순찰",
            "다리 보수 호위"
        };

        public static readonly string[] TEXT_LEDGER_NOTES = { "선금", "중도금", "잔금", "완수" };
    }
}
