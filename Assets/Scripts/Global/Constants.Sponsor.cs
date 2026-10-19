namespace Underwrite.Global
{
    /// <summary>
    /// 첫 후원 안내 — 용사단 테이블 → 여관 일에서 사정 듣기 → 길드 익명 의뢰(2026-10-19 사장님: 후원 방법 설명이 부족하다, 강제로).
    /// 문구는 과장 초안.
    /// </summary>
    public static partial class StringConstants
    {
        // 용사단 테이블을 처음 열었을 때, 한 사람과 이야기하기 전에는 떠나지 않는다.
        public static string TEXT_SPONSOR_TABLE_LOCKED = "먼저 한 사람에게 인사부터";

        // 여관 주인이 부엌 일을 맡긴다 — 술을 나르며 용사단 테이블의 말을 듣는다.
        public static string TEXT_SPONSOR_WORK_CALL = "왔어? 오늘은 구석 테이블 쪽 주문을 맡아 줘. 술 나르면서 귀도 좀 열어 두고.";

        // 사정을 듣고 난 뒤의 혼잣말. 왜 익명 의뢰인지를 주인공이 스스로 짚는다.
        public static readonly string[] TEXT_SPONSOR_THINK =
        {
            "치유사가 다쳤다. 약값이 없어 큰 의뢰는 전부 물렸다고 했다.",
            "이대로 원정을 떠나면… 원작처럼 하나씩 쓰러지는 첫 장면이 된다.",
            "그렇다고 여관 일꾼이 금화를 내밀면 누가 받겠나. 받아도 수상하게 볼 것이다.",
            "…이름을 비우고 길드에 의뢰를 내자. 돈은 길드를 거쳐 저들 앞으로 간다."
        };

        // 길드 서기가 익명 의뢰를 일러 준다. 무엇을 · 어떻게 고르고 · 무엇을 근거로 · 언제까지.
        public static readonly string[] TEXT_SPONSOR_CLERK_LINES =
        {
            "익명 의뢰는 처음이시군요. 받을 단을 적고, 무엇에 쓸 돈인지 고르고, 예치금을 먼저 맡기시면 됩니다. 의뢰주 칸은 비워 두셔도 저희가 봉인해서 전합니다.",
            "고르실 때 조심하세요. 그 단의 처지에 맞는 의뢰여야 실제로 씁니다. 처지에 안 맞으면 받기만 하고 소용이 없지요. 예치금은 어느 쪽이든 돌려드리지 않습니다.",
            "단의 처지는 저희 창구에도 적혀 오지 않습니다. 그 사람들 말이 제일 많이 오가는 데가 여관이라더군요."
        };
        public static string TEXT_SPONSOR_CLERK_DEADLINE = "그리고 원정을 떠나기 전에 닿아야 소용이 있습니다. 그 단이 떠나기까지 {0}일 남았습니다.";

        public static string TEXT_SPONSOR_FORM_NOTE = "여관에서 들은 사정에 맞는 의뢰를 고른다";
        public static string TEXT_SPONSOR_FORM_LOCKED = "첫 의뢰는 이 자리에서 낸다";

        // 처지에 맞지 않는 것을 골랐다 — 첫 의뢰는 내지 않고 들은 말을 다시 떠올린다.
        public static string TEXT_SPONSOR_MISS = "…아니다. 여관에서 들은 말을 다시 떠올려 보자.";

        public static string TEXT_SPONSOR_NO_MONEY = "예치금은 {0:N0}부터입니다. 지금 가지신 건 {1:N0}… 기한 안에 마련해 오시면 됩니다.";
        public static string TEXT_SPONSOR_NO_MONEY_SELF = "돈이 모자라다. 지하 장에서 벌어 와야 한다.";

        // 첫 의뢰를 맡긴 뒤 — 이 게임의 고리(지하에서 번다 → 들은 사정에 맞게 후원한다 → 여관에서 반응을 듣는다).
        public static string TEXT_SPONSOR_AFTER = "이름 없는 돈이 저들에게 간다. 맞게 골랐는지는 다음 날, 여관에서 일하며 들을 수 있다.";
        public static string TEXT_SPONSOR_AFTER_LOOP = "지하에서 벌고, 여관에서 듣고, 길드에서 건넨다. …이게 내가 결말을 바꾸는 방법이다.";
    }
}
