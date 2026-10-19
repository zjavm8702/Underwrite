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
        // 장부 맞추기 · 엿듣기 · 주문 나르기 규칙서(2026-10-19 데모 점검 — 규칙서가 없었다). 문구는 과장 초안.
        public static string TEXT_RULE_LEDGER_HEADING = "장부 맞추기 — 규칙";
        public static string TEXT_RULE_LEDGER_1_TITLE = "두 장의 기록";
        public static string TEXT_RULE_LEDGER_1_BODY = "왼쪽은 의뢰 기록(단 · 의뢰 · 약정 보수), 오른쪽은 지급 기록(날짜 · 받은 단 · 금액). 한 단만 받은 돈의 합이 약정과 맞지 않는다.";
        public static string TEXT_RULE_LEDGER_2_TITLE = "더하고 짚는다";
        public static string TEXT_RULE_LEDGER_2_BODY = "단마다 지급을 더해 약정과 견준다. 어긋난 단의 의뢰 줄을 누른다. 틀리게 짚으면 기록 담당의 눈길이 쌓이고, 셋이 차면 쫓겨난다.";
        public static string TEXT_RULE_LEDGER_3_TITLE = "무슨 뜻인가";
        public static string TEXT_RULE_LEDGER_3_BODY = "찾으면 비고의 작은 글씨를 보고 그 돈이 어디로 갔는지 고른다. 맞히면 그 단의 대장간 사정 — 재개장 때 반드시 맞는 정보를 안다.";
        public static readonly string[] TEXT_RULE_LEDGER_SAMPLE = { "붉은 칼날 · 약정 300", "지급 120 + 100 = 220", "→ 80이 모자란다" };

        public static string TEXT_RULE_EAVES_HEADING = "엿듣기 — 규칙";
        public static string TEXT_RULE_EAVES_1_TITLE = "테이블 사이를 돈다";
        public static string TEXT_RULE_EAVES_1_BODY = "술을 나르며 귀 기울일 기회가 네 번 있다. 테이블을 골라 「곁에 머문다」를 누르면 그 쪽 이야기가 한 토막 들린다.";
        public static string TEXT_RULE_EAVES_2_TITLE = "두 번 머물면 끝까지";
        public static string TEXT_RULE_EAVES_2_BODY = "한 테이블에 두 번 머물면 이야기를 끝까지 듣는다 — 도감에 그 단의 사정이 적힌다. 여러 곳을 한 번씩 돌면 토막만 남는다.";
        public static string TEXT_RULE_EAVES_3_TITLE = "경계하는 테이블";
        public static string TEXT_RULE_EAVES_3_BODY = "분위기가 날카로운 테이블에 오래 서 있으면 들킨다. 들키면 그날은 홀에 나갈 수 없다. 용사단 테이블에서는 그들의 사정이 들린다.";
        public static readonly string[] TEXT_RULE_EAVES_SAMPLE = { "오래된 참나무단 — 「…늪 쪽 길이…」", "한 번 더 머문다 → 끝까지 들었다" };

        public static string TEXT_RULE_WORK_HEADING = "주문 나르기 — 규칙";
        public static string TEXT_RULE_WORK_1_TITLE = "외운다";
        public static string TEXT_RULE_WORK_1_BODY = "주문표 셋이 잠깐 보였다가 가려진다. 어디에 무엇을 몇 개 — 가려지기 전에 외운다. 다 외웠으면 「다 외웠다」.";
        public static string TEXT_RULE_WORK_2_TITLE = "쟁반을 채워 내놓는다";
        public static string TEXT_RULE_WORK_2_BODY = "부르는 자리마다 물건을 눌러 쟁반에 담고 「내놓는다」. 맞힌 만큼 품삯을 받는다. 「대충 끝낸다」면 품삯은 절반이다.";
        public static readonly string[] TEXT_RULE_WORK_SAMPLE = { "창가 테이블 — 맥주 ×1, 포도주 ×2", "쟁반: 맥주, 포도주, 포도주 → 내놓는다" };

        // 놀이 화면의 「?」 — 언제든 규칙서를 다시 본다(온보딩 설계안 §3).
        public static string TEXT_RULE_HELP = "?";
        public static string TEXT_RULE_HELP_TIP = "규칙 다시 보기";

        // 처음 한 번 규칙서를 보인 놀이(StoryProgress.RulesSeen, 저장된다).
        public const string RULE_KEY_LEDGER = "rule_ledger";
        public const string RULE_KEY_EAVES = "rule_eaves";
        public const string RULE_KEY_WORK = "rule_work";
        public static string TEXT_RULE_MARK_NO = "✗";
        public static string TEXT_RULE_MARK_YES = "○";
    }
}
