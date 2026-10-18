namespace Underwrite.Global
{
    /// <summary>
    /// 미니게임 규칙서(2026-10-18 — 연습 판 전에 쪽을 넘기며 보는 설명). 문구는 과장 초안 — 사장님 검토 대기.
    /// 쪽마다 제목 · 두세 줄 설명, 무대 위 말풍선·도장 글씨.
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_RULE_BACK = "이전";
        public static string TEXT_RULE_NEXT = "다음";
        public static string TEXT_RULE_TRY = "해 본다";
        public static string TEXT_RULE_COUNT = "{0} / {1}";
        public static string TEXT_RULE_ME = "나";
        public static string TEXT_RULE_OPPONENT = "상대";

        // ── 주사위 내기 ──
        public static string TEXT_RULE_DICE_HEADING = "주사위 내기 — 규칙";
        public static string TEXT_RULE_DICE_1_TITLE = "가린 주사위";
        public static string TEXT_RULE_DICE_1_BODY = "둘이 주사위를 4개씩 굴린다. 내 주사위는 나만, 상대 주사위는 상대만 본다.";
        public static string TEXT_RULE_DICE_2_TITLE = "부르기 — 둘의 주사위를 합쳐서";
        public static string TEXT_RULE_DICE_2_BODY = "「○ 눈이 N개 이상」을 부른다. 셈은 내 것과 상대 것 8개를 모두 합쳐서 한다. 내 손에 많은 눈일수록 부르기 안전하다.";
        public static string TEXT_RULE_DICE_2_BUBBLE = "「4 눈이 3개 이상」";
        public static string TEXT_RULE_DICE_2_COUNT = "4 눈 {0}개";
        public static string TEXT_RULE_DICE_3_TITLE = "더 높게 부르기";
        public static string TEXT_RULE_DICE_3_BODY = "다음 사람은 앞 말보다 높게 불러야 한다. 개수를 늘리거나, 같은 개수면 더 높은 눈으로.";
        public static readonly string[] TEXT_RULE_DICE_3_LADDER =
        {
            "나: 「4 눈 2개」",
            "상대: 「5 눈 2개」 — 같은 개수, 더 높은 눈",
            "나: 「3 눈 3개」 — 개수를 늘렸다",
            "✗ 「2 눈 3개」 — 앞 말보다 낮아서 못 부른다"
        };
        public static string TEXT_RULE_DICE_4_TITLE = "거짓말!";
        public static string TEXT_RULE_DICE_4_BODY = "앞 말을 못 믿겠으면 「거짓말!」. 둘 다 주사위를 열고 센다. 말이 맞으면 부른 쪽이, 틀리면 외친 쪽이 그 판을 이긴다.";
        public static string TEXT_RULE_DICE_4_BUBBLE = "상대: 「6 눈이 5개 이상」";
        public static string TEXT_RULE_DICE_4_STAMP = "거짓말!";
        public static string TEXT_RULE_DICE_4_RESULT = "6 눈은 2개뿐 — 외친 내가 이긴다";
        public static string TEXT_RULE_DICE_5_TITLE = "승부";
        public static string TEXT_RULE_DICE_5_BODY = "먼저 두 판을 이기면 끝. 이기면 취한 상대가 이것저것 털어놓고, 지면 술값을 낸다.";
        public static readonly string[] TEXT_RULE_DICE_5_ROUNDS = { "1판", "2판", "3판" };
        public static string TEXT_RULE_WIN = "승";

        // ── 주문서 읽기 ──
        public static string TEXT_RULE_SMITH_HEADING = "주문서 꽂이 — 규칙";
        public static string TEXT_RULE_SMITH_1_TITLE = "주문서 한 장";
        public static string TEXT_RULE_SMITH_1_BODY = "주문서마다 날짜, 셈 상태, 주문한 단, 물건이 적혀 있다.";
        public static readonly string[] TEXT_RULE_SMITH_1_CALLOUTS = { "날짜", "셈 상태", "주문한 단", "물건" };
        public static string TEXT_RULE_SMITH_2_TITLE = "지난 계절 주문은 빼고";
        public static string TEXT_RULE_SMITH_2_BODY = "지난 계절에 들어온 주문은 이번 일과 상관없다.";
        public static string TEXT_RULE_SMITH_3_TITLE = "취소된 주문도 빼고";
        public static string TEXT_RULE_SMITH_3_BODY = "「취소」 도장이 찍힌 주문은 아무것도 말해 주지 않는다.";
        public static string TEXT_RULE_SMITH_4_TITLE = "남는 한 장";
        public static string TEXT_RULE_SMITH_4_BODY = "그 단의 이번 계절, 살아 있는 주문 한 장이 진짜다. 잘못 짚으면 대장장이 눈길이 쌓이고, 세 번이면 쫓겨난다.";

        // ── 기록 짜맞추기 ──
        public static string TEXT_RULE_DEDUCE_HEADING = "기록 짜맞추기 — 규칙";
        public static string TEXT_RULE_DEDUCE_1_TITLE = "기록 쪽지";
        public static string TEXT_RULE_DEDUCE_1_BODY = "흩어진 쪽지마다 단 셋의 목적지·의뢰인에 대한 사실이 하나씩 적혀 있다. 거짓 쪽지는 없다.";
        public static readonly string[] TEXT_RULE_DEDUCE_1_CLUES =
        {
            "떠돌이 사냥꾼 셋은 늪지 쪽으로 가지 않는다.",
            "폐광 쪽으로 가는 단은 신전의 의뢰를 받았다."
        };
        public static string TEXT_RULE_DEDUCE_2_TITLE = "표 채우기";
        public static string TEXT_RULE_DEDUCE_2_BODY = "표의 칸을 누를 때마다 보기가 돌아간다. 단마다 목적지 하나, 의뢰인 하나. 같은 보기는 두 단에 겹치지 않는다.";
        public static string TEXT_RULE_DEDUCE_3_TITLE = "지워 가며 좁히기";
        public static string TEXT_RULE_DEDUCE_3_BODY = "쪽지 하나로 안 되는 칸을 지운다. 하나만 남으면 그게 답이고, 그 보기는 다른 단에서 지운다.";
        public static readonly string[] TEXT_RULE_DEDUCE_3_STEPS =
        {
            "「사냥꾼은 늪지로 가지 않는다」",
            "「사냥꾼은 폐광으로 가지 않는다」",
            "사냥꾼 = 북쪽 고개 — 남은 하나",
            "북쪽 고개는 다른 단에서 지운다"
        };
        public static readonly string[] TEXT_RULE_DEDUCE_3_ROWS = { "사냥꾼", "호위대", "약초꾼" };
        public static readonly string[] TEXT_RULE_DEDUCE_3_COLS = { "북쪽 고개", "늪지", "폐광" };
        public static string TEXT_RULE_DEDUCE_4_TITLE = "맞춰 보기";
        public static string TEXT_RULE_DEDUCE_4_BODY = "표를 다 채워야 맞춰 볼 수 있고, 틀리면 어디가 틀렸는지는 알려 주지 않는다. 기회는 세 번 — 찍지 말고 따져서.";
        public static string TEXT_RULE_MARK_NO = "✗";
        public static string TEXT_RULE_MARK_YES = "○";
    }
}
