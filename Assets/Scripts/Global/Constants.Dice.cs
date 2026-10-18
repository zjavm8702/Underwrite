namespace Underwrite.Global
{
    /// <summary>여관 주사위 내기(라이어스 다이스, 2026-10-18). 문구는 과장 초안 — 사장님 검토 대기.</summary>
    public static partial class StringConstants
    {
        public static string TEXT_DICE_CHOICE = "모험단 테이블에서 주사위 내기를 건다";
        public static string TEXT_DICE_CHOICE_NOTE = "행동력 {0} · 이기면 취한 상대가 입을 연다 · 지면 술값 {1}";
        public static string TEXT_DICE_NONE = "주사위를 받아 줄 테이블이 없다";
        public static string TEXT_DICE_DONE_TODAY = "오늘은 이미 한 판 했다";
        public static string TEXT_DICE_NO_MONEY = "술값 {0}이 없다 · 가진 돈 {1:N0}";

        public static string TEXT_DICE_TITLE = "여관 · 주사위 내기";
        public static string TEXT_DICE_HINT = "둘의 주사위를 합쳐 「○ 눈이 N개 이상」을 부른다. 앞사람보다 높게 — 못 믿겠으면 「거짓말!」";
        public static string TEXT_DICE_ME = "나";
        public static string TEXT_DICE_SCORE = "나 {0} : {1} {2}";
        public static string TEXT_DICE_BID = "{0}: 「{1} 눈이 {2}개 이상」";
        public static string TEXT_DICE_CALL = "{0}: 「거짓말!」";
        public static string TEXT_DICE_NO_BID = "아직 아무도 부르지 않았다";
        public static string TEXT_DICE_YOUR_TURN = "내 차례 — 더 높게 부르거나 「거짓말!」";
        public static string TEXT_DICE_YOUR_OPEN = "내가 먼저 부른다";
        public static string TEXT_DICE_THINKING = "상대가 잔을 굴리며 생각한다…";
        public static string TEXT_DICE_REVEAL = "열어 보니 {0} 눈이 {1}개 — {2}";
        public static string TEXT_DICE_ROUND_WON = "내가 이겼다";
        public static string TEXT_DICE_ROUND_LOST = "내가 졌다";
        public static string TEXT_DICE_COUNT = "{0}개";
        public static string TEXT_DICE_HIDDEN = "?";
        public static string TEXT_DICE_BTN_BID = "부른다";
        public static string TEXT_DICE_BTN_CALL = "거짓말!";
        public static string TEXT_DICE_BTN_NEXT = "다음 판";
        public static string TEXT_DICE_BTN_END = "자리에서 일어난다";
        public static string TEXT_DICE_LEAVE = "그만둔다";
        public static string TEXT_DICE_MINUS = "−";
        public static string TEXT_DICE_PLUS = "+";
        public static string TEXT_DICE_MOOD_LINE = "{0} — {1}";

        // 상대의 대담함을 말로 흘린다 [조심 · 보통 · 대담]. 실마리라 숫자로 보이지 않는다.
        public static readonly string[] TEXT_DICE_MOODS =
        {
            "잔을 홀짝인다. 조심스러워 보인다.",
            "적당히 취했다.",
            "얼굴이 벌겋다. 큰소리를 친다."
        };

        public static string TEXT_DICE_HELP = "내 손에 {0} 눈이 {1}개 · 상대 주사위 {2}개 중엔 보통 0~1개";

        // 연습 판(프롤로그 여관). 상대는 단골 손님, 주사위와 수는 정해져 있다.
        public static string TEXT_DICE_TUT_OPPONENT = "단골 손님";
        public static string TEXT_DICE_TUT_MOOD = "주인 부탁으로 연습 상대를 해 준다.";
        public static string TEXT_DICE_TUT_1_OPEN = "네 손에 4가 둘이다. 둘의 주사위를 합치면 4는 적어도 둘 — 개수 2, 눈 4를 고르고 「부른다」.";
        public static string TEXT_DICE_TUT_1_RESPONSE = "「4 눈이 6개」? 여덟 개 중 여섯이라니 — 네 손엔 둘뿐이다. 「거짓말!」을 외치자.";
        public static string TEXT_DICE_TUT_1_REVEAL = "열어 보니 4는 셋. 상대가 허세를 부렸다 — 외친 내가 이겼다.";
        public static string TEXT_DICE_TUT_2_RESPONSE = "「3 눈이 2개」 — 네 손에만 3이 둘, 맞는 말이다. 의심 말고 하나 얹어 개수 3, 눈 3으로 「부른다」.";
        public static string TEXT_DICE_TUT_2_REVEAL = "열어 보니 3은 셋 — 내 말이 맞았다. 그럴듯한 말엔 올려 불러 넘기는 것도 수다.";

        public static string TEXT_DICE_TAG = "주사위 내기";
        public static string TEXT_DICE_WON = "「크으, 졌다 졌어.」 술기운에 {0} 사람이 털어놓는다.";
        public static string TEXT_DICE_WON_NOTHING = "「크으, 졌다.」 …털어놓을 얘기는 없는 모양이다. 술값은 굳었다.";
        public static string TEXT_DICE_LOST = "술값 {0}을 냈다. 「다음에 또 하자고.」";
    }
}
