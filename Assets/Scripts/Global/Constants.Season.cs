namespace Underwrite.Global
{
    /// <summary>
    /// 출발 밤과 계절 계획(시간구조 설계안 §2·§3). 용사단이 원정을 떠나고, 주인공은 휴장하는 석 달을 어떻게 보낼지 고른다.
    /// 고른 대로 짧은 장면이 몽타주로 흐르고, 친한 용사가 있으면 원정지에서 편지가 온다.
    /// 문구는 과장 초안 — 사장님 검토 대기(2026-10-18 인터뷰).
    /// </summary>
    public static partial class StringConstants
    {
        public const string NEED_SHIELD_ID = "need_shield";

        public static string TEXT_DEPART_TAG = "출발";
        public static string TEXT_DEPART_OPEN = "새벽별 용사단이 국경으로 떠났다. 여관 앞 등불이 그들의 등을 오래 비췄다.";
        public static string TEXT_DEPART_FAREWELL = "다녀올게요. …장부만 보지 말고, 밥도 좀 챙겨 먹어요.";
        public static string TEXT_DEPART_PLAN = "원정은 한 계절. 그동안 지하시장은 닫힌다. 그 석 달을 어떻게 보낼지 정해야 한다.";

        public static string TEXT_PLAN_TAG = "계절 계획";
        public static readonly string[] TEXT_PLAN_MONTH_ASK =
        {
            "첫째 달에는 무엇을 할까.",
            "둘째 달에는.",
            "셋째 달. 그들이 돌아오기 전에."
        };

        // 순서는 SeasonTask와 같다.
        public static readonly string[] TEXT_PLAN_TASK =
        {
            "여관 일을 더 맡는다",
            "길드 기록실 심부름을 한다",
            "대장간에서 견습을 한다",
            "지하 사람들과 어울린다",
            "원정 짐수레를 따라간다",
            "국경의 징조를 찾는다"
        };

        public static readonly string[] TEXT_PLAN_TASK_NOTE =
        {
            "다음 계절 투자 자금 +{0:N0}",
            "다음 계절에 오는 단 하나의 내력을 미리 안다",
            "다음 계절 단 하나의 대장간 사정을 미리 안다",
            "재개장 장에서 정보상이 귀띔을 하나 더 준다",
            "용사단 모두와 조금 가까워진다",
            "멸망의 단서를 하나 찾는다"
        };

        public static readonly string[] TEXT_PLAN_MONTAGE =
        {
            "여관 부엌의 불은 새벽까지 꺼지지 않았다. 손에 남은 건 굳은살과 동전 몇 닢.",
            "기록실 먼지를 털다가, 다음 계절에 올 용병단의 이름을 먼저 보았다.",
            "망치질을 배우며 누가 무엇을 주문하는지 귀에 익혔다. 쇠는 거짓말을 하지 않는다.",
            "창고 아래 탁자에서 술잔이 몇 번 오갔다. 정보상이 처음으로 내 이름을 불렀다.",
            "{0} — 짐수레 바퀴를 같이 고치다가, 처음으로 그 사람이 크게 웃는 걸 봤다.",
            "{0}"
        };

        // 국경의 징조. 찾을 때마다 다음 줄로 넘어가고, 다 보면 마지막 줄을 되풀이한다.
        public static readonly string[] TEXT_PLAN_OMENS =
        {
            "국경 마을의 우물이 말라 있었다. 아무도 길드에 알리지 않았다.",
            "경비 초소가 비어 있었다. 지키는 의뢰에는 돈이 붙지 않으니까.",
            "숲 가장자리에 처음 보는 발자국. 화면 속에서 본 그 모양이다.",
            "징조는 이제 숨지도 않는다. 보려는 사람이 없을 뿐이다."
        };

        public static string TEXT_LETTER_TAG = "원정지에서 온 편지";
        public static string TEXT_LETTER_BODY = "{0}의 글씨였다. 국경은 춥고, 밥은 맛없고, 다들 잘 있다고. 「당신도 잘 지내요.」";
        public static string TEXT_LETTER_SHIELD = "편지 끝에 한 줄이 덧붙어 있었다. 「고드릭 방패에 금이 갔어요. 돌아가면 새로 맞춰야 하는데, 다들 말을 안 해요.」";
        public static string TEXT_LETTER_REMOTE = "길드를 거쳐 익명으로 방패 값을 보낸다";
        public static string TEXT_LETTER_REMOTE_NOTE = "원격 의뢰 {0:N0} · 가진 돈 {1:N0}";
        public static string TEXT_LETTER_KEEP = "지금은 보내지 않는다";
        public static string TEXT_LETTER_SENT = "누가 보냈는지는 적지 않았다. 그래도 손이 떨렸다.";

        public static string TEXT_DEPART_FADE = "석 달이 흘렀다";

        // 데모 마무리. 문구는 과장 초안.
        public static string TEXT_DEMO_TAG = "첫 해 여름의 끝";
        public static string TEXT_DEMO_END_1 = "두 번의 원정이 지나갔다. 화면 속에서 본 끝까지는 아직 아홉 해하고도 반이 남았다.";
        public static string TEXT_DEMO_END_SUMMARY = "가진 돈과 종목 {0:N0} · 용사단 무사 {1} · 다침 {2} · 잃음 {3} · 국경의 징조 {4}";
        public static string TEXT_DEMO_END_2 = "이 이야기는 아직 만들어지는 중입니다. 여기까지 함께해 주셔서 고맙습니다.";
        public static string TEXT_DEMO_TO_TITLE = "처음 화면으로";
        public static string TEXT_DEMO_STAY = "조금 더 둘러본다";

        // 재개장 때 계절 계획의 결과를 알린다.
        public static string TEXT_PLAN_RESULT_TITLE = "석 달 동안 준비한 것";
        public static string TEXT_PLAN_RESULT_GUILD = "길드 기록에서 본 내력 — {0}: {1}";
        public static string TEXT_PLAN_RESULT_SMITH = "대장간에서 들은 사정 — {0}: {1}";
        public static string TEXT_PLAN_RESULT_TIPS = "정보상이 오늘 장에서 귀띔을 {0}번 더 해 주기로 했다.";

        // 정보상의 귀띔. 아직 다 반영되지 않은 원정 결과를 짚는다.
        public static string TEXT_INFORMANT_TIP_UP = "정보상의 귀띔: {0} 값은 아직 결과를 다 따라가지 못했다. 더 오를 쪽이다.";
        public static string TEXT_INFORMANT_TIP_DOWN = "정보상의 귀띔: {0} 값은 아직 덜 빠졌다. 더 내릴 쪽이다.";
    }
}
