namespace Underwrite.Global
{
    /// <summary>
    /// 할 일 목록(QuestLog). 순서가 곧 이야기의 순서다. 웹툰 6화 뒤부터 첫 계절의 귀환까지.
    /// 어디로 가서 무엇을 누르라는 안내는 없앴다(2026-10-17 사장님 결정 — 받은 것과 진행도만).
    /// 맡기는 사람의 말(QUEST_ASKS)이 길잡이다. 문구는 과장 초안.
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
            "돌아온 사람들",
            "금 간 방패",
            "국경의 징조"
        };

        public static readonly string[] QUEST_ASKS =
        {
            "일자리? 마침 부엌 손이 모자라. 여관 문 앞에서 말만 해. 밥값은 쳐 줄게.",
            "창고 아래 그곳이 머릿속에서 떠나지 않는다. 장이 열릴 때 다시 내려가 봐야 한다.",
            "사람의 운명에 값이 매겨진다. …한 번은 직접 사 봐야 그게 뭔지 안다.",
            "저 구석 테이블 용사단, 우리 집 단골이야. 술 한 잔 따라 주면서 인사라도 해 둬.",
            "부엌 일 하면서 귀 좀 열어 둬. 저 사람들 요즘 표정이 안 좋아.",
            "도와주고 싶은 사람이 있으면 이름 안 밝히고 의뢰를 낼 수도 있지. 기록실 창구로 와.",
            "새 용병단이 들어왔어. 먼저 알아본 사람이 먼저 벌지. 한 단에 대해 두 가지만 캐 와 봐.",
            "사흘째엔 우리도 하루 쉬기로 했어. 그날 여관에서 보자.",
            "원정에서 돌아오는 날, 여관에서 기다려 줄래요? …그냥, 누가 기다려 주면 좋잖아요.",
            "고드릭 방패 봤어? 금이 갔는데 본인은 괜찮대. …다음 원정 전에 누가 좀 말려 줬으면 좋겠어.",
            "국경 쪽 하늘빛이 달라요. 누가 한 번 가서 봐 줬으면… 길드는 돈이 안 된다고 안 간대요."
        };

        public static string TEXT_QUEST_PROGRESS = "{0} / {1}";
        public static string TEXT_QUEST_ASK_TAG = "부탁";
        public static string TEXT_QUEST_SELF_TAG = "혼잣말";

        public static string TEXT_QUEST_LABEL = "할 일 · {0}";
        public static string TEXT_QUEST_DONE = "할 일을 마쳤다 — {0}";
        public static string TEXT_QUEST_DONE_REWARD = "할 일을 마쳤다 — {0} · {1:N0}을 받았다";
        public static string TEXT_QUEST_TAB = "할 일";
        public static string TEXT_QUEST_ALL_DONE = "지금은 할 일이 없다.";
    }
}
