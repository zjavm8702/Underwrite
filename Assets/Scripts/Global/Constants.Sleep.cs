namespace Underwrite.Global
{
    /// <summary>
    /// 잠자리(2026-10-17 다시 넣음). 오후장이 끝난 밤, 여관 다락방에서 잔다.
    /// 그날을 정리하고 주인공이 혼잣말을 한다 — 이야기가 진행될수록 달라진다(사장님: 주기가 흐를수록 감정을 보이는 자리).
    /// 혼잣말은 한 번 나온 것은 다시 나오지 않고, 해당하는 것이 없으면 평소의 밤 혼잣말 셋을 돌려 쓴다.
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_SLEEP_CHOICE = "다락방에서 잔다";
        public static string TEXT_SLEEP_CHOICE_NOTE = "오후장이 끝난 밤 · 아침 {0}까지";
        public static string TEXT_SLEEP_LOCKED = "오후장이 끝난 밤에만 잘 수 있다";
        public static string TEXT_SLEEP_SPEAKER = "다락방";
        public static string TEXT_SLEEP_TAG = "{0}일째 밤";
        public static string TEXT_SLEEP_RECAP = "가진 돈 {0:N0} · 가진 종목의 값 {1:N0}. 오늘 밤 영지에서 들은 소식 {2}개.";
        public static string TEXT_SLEEP_DEADLINE = "들은 용사단 사정의 기한까지 {0}일 남았다.";
        public static string TEXT_SLEEP_INSPECTION = "내일은 영주 시찰일이다. 지하시장도 쉰다.";
        public static string TEXT_SLEEP_FADE = "다락방에서 잠들었다";

        // 이야기의 고비를 지난 뒤 한 번씩. 위에서부터 먼저 고른다.
        public static string TEXT_SLEEP_RETURN_SAFE = "다들 돌아왔다. 다친 사람 없이. …처음으로, 내가 무언가를 바꾼 것 같다.";
        public static string TEXT_SLEEP_RETURN_HURT = "피오가 붕대를 감고 웃었다. 약값만 먼저 닿았어도. 다음엔 놓치지 않는다.";
        public static string TEXT_SLEEP_SUSPECT = "에드윈이 의뢰서 두 장을 나란히 놓고 있었다. 들키면 어떻게 되지. …아니, 들켜도 멈출 수는 없다.";
        public static string TEXT_SLEEP_DAY_OFF = "오늘은 그냥 웃었다. 십 년 뒤를 아는 사람이 이렇게 웃어도 되는 걸까.";
        public static string TEXT_SLEEP_FIRST_REQUEST = "이름 없는 의뢰를 냈다. 그들은 누가 보냈는지 모른다. 그게 맞다.";
        public static string TEXT_SLEEP_HEARD_NEED = "술잔을 나르다 들은 이야기가 자꾸 떠오른다. 그들에게 지금 필요한 건 돈이다.";
        public static string TEXT_SLEEP_FIRST_TRADE = "처음으로 사람의 운명에 돈을 걸었다. 손이 아직 떨린다.";
        public static string TEXT_SLEEP_CELLAR = "창고 아래 그곳이 머릿속에서 떠나지 않는다. 모험가의 목숨값이 칠판 위에서 오르내렸다.";

        // 평소의 밤
        public static string TEXT_SLEEP_PLAIN_1 = "다락방 창으로 영지의 불빛이 하나둘 꺼진다. 내일도 장은 열린다.";
        public static string TEXT_SLEEP_PLAIN_2 = "화면 속에서 본 끝은 아직 멀다. 그래도 하루가 줄었다.";
        public static string TEXT_SLEEP_PLAIN_3 = "아래층에서 누군가 노래를 부른다. 그 목소리가 십 년 뒤에도 있었으면.";
    }
}
