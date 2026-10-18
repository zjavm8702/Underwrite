namespace Underwrite.Global
{
    /// <summary>
    /// 길드 기록 짜맞추기(보드게임 「미니 크라임」을 줄인 추리, 2026-10-18). 문구는 과장 초안 — 사장님 검토 대기.
    /// 단서 문장은 첫째 이름에만 조사를 붙인다({0}{은}). 둘째 이름({1}) 뒤에는 조사가 오지 않게 쓴다.
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_DEDUCE_CHOICE = "흩어진 의뢰 기록을 짜맞춘다";
        public static string TEXT_DEDUCE_CHOICE_NOTE = "행동력 {0} · 표를 다 맞히면 먼 원정을 가는 단의 사정";
        public static string TEXT_DEDUCE_NONE = "오늘은 짜맞출 기록이 없다";
        public static string TEXT_DEDUCE_DONE_TODAY = "오늘은 이미 기록을 봤다";

        public static string TEXT_DEDUCE_TITLE = "길드 기록실 · 기록 짜맞추기";
        public static string TEXT_DEDUCE_HINT = "기록 쪽지를 읽고 단마다 목적지와 의뢰인을 채운다. 다 채워야 맞춰 볼 수 있고, 어디가 틀렸는지는 알려 주지 않는다.";
        public static string TEXT_DEDUCE_CLUES = "기록 쪽지";
        public static string TEXT_DEDUCE_TABLE = "이번 계절 원정";
        public static string TEXT_DEDUCE_DEST_HEAD = "목적지";
        public static string TEXT_DEDUCE_CLIENT_HEAD = "의뢰인";
        public static string TEXT_DEDUCE_BLANK = "?";
        public static string TEXT_DEDUCE_OPTIONS = "목적지 — {0}\n의뢰인 — {1}";
        public static string TEXT_DEDUCE_OPTION_SEPARATOR = " · ";
        public static string TEXT_DEDUCE_TRIES = "남은 기회 {0} / {1}";
        public static string TEXT_DEDUCE_CHECK = "맞춰 본다";
        public static string TEXT_DEDUCE_NEED_ALL = "빈칸이 있다. 다 채워야 맞춰 볼 수 있다.";
        public static string TEXT_DEDUCE_WRONG = "어딘가 앞뒤가 안 맞는다. 기록 담당이 고개를 든다.";
        public static string TEXT_DEDUCE_OUT = "「거기서 뭘 그렇게 뒤지나?」 기록실에서 쫓겨났다.";
        public static string TEXT_DEDUCE_LEAVE = "그만둔다";
        public static string TEXT_DEDUCE_TAG = "기록 짜맞추기";
        public static string TEXT_DEDUCE_RIGHT = "앞뒤가 맞는다. {0} 쪽 먼 원정은 {1} — 그 단의 사정도 기록 틈에 보인다: {2}";

        public static string TEXT_DEDUCE_BAND_NOT_DEST = "{0}{은} {1} 쪽으로 가지 않는다.";
        public static string TEXT_DEDUCE_BAND_CLIENT = "{0}{은} {1}의 의뢰를 받았다.";
        public static string TEXT_DEDUCE_BAND_NOT_CLIENT = "{0}{은} {1}의 의뢰를 받지 않았다.";
        public static string TEXT_DEDUCE_DEST_CLIENT = "{0} 쪽으로 가는 단은 {1}의 의뢰를 받았다.";
        public static string TEXT_DEDUCE_DEST_NOT_CLIENT = "{0} 쪽으로 가는 단은 {1}의 의뢰가 아니다.";

        public static string TEXT_DEDUCE_TUT_HINT = "쪽지를 하나씩 읽고 칸을 눌러 채워 본다. 연습이라 틀려도 괜찮다.";
        public static string TEXT_DEDUCE_TUT_DONE = "…맞네. 흩어진 쪽지도 맞추면 한 장이 되지.";

        public static readonly string[] TEXT_DEDUCE_DESTINATIONS = { "북쪽 고개", "늪지", "폐광", "국경 초소", "동쪽 숲길" };
        public static readonly string[] TEXT_DEDUCE_CLIENTS = { "상단 조합", "영주관", "신전", "약재상", "광산 조합" };
    }
}
