namespace Underwrite.Global
{
    /// <summary>
    /// 용사단 대사. 이름이 정해지지 않아 역할 이름을 임시로 쓴다(2026-10-17).
    /// 연기 방향은 기획서 §17을 따른다. 양이 많아 영역별 partial로 나눴다(코드 규칙 §7-2).
    ///
    /// 순서는 학자 · 검사 · 마도사 · 전사 · 요정이다. <c>HERO_*</c> 배열은 모두 이 순서를 지킨다.
    /// </summary>
    public static partial class StringConstants
    {
        // ── 사람 ─────────────────────────────────
        public static readonly string[] HERO_NAMES =
        {
            "안경 쓴 학자", "흑발의 검사", "보라 머리 마도사", "은갑 전사", "나방 날개 요정"
        };

        public static readonly string[] HERO_ROLES =
        {
            "용사단 · 기록과 작전", "용사단 · 선봉", "용사단 · 마도사", "용사단 · 방패", "용사단 · 치유사"
        };

        /// <summary>표정 묶음 자산 이름. Assets/Data/Portraits/{이름}.asset.</summary>
        public static readonly string[] HERO_FACE_ASSETS = { "scholar", "sella", "elf", "brann", "fairy" };

        // ── 여관 테이블에서 말을 걸 때 ───────────
        public static readonly string[] HERO_GREET =
        {
            "(안경을 고쳐 쓰며) 아, 일꾼 씨. 주문은 아까 했는데… 무슨 일이죠?",
            "(팔짱을 낀 채) 술은 거기 두고 가. 할 말이라도 있어?",
            "(턱을 괴고 웃으며) 어머, 우리 테이블에 오래 머무네? 궁금한 게 있구나.",
            "(껄껄 웃으며) 오, 자네! 오늘도 손이 바쁘군. 잠깐 앉았다 가게.",
            "(테이블 위로 날아올라 얼굴을 들여다보며) 너, 어제보다 피곤해 보여!"
        };

        // 대답 셋: 따뜻하게 / 묻는다 / 일꾼답게. 순서는 모든 용사가 같다.
        public static string TEXT_HERO_REPLY_WARM = "요즘 다들 무사하신지 궁금해서요.";
        public static string TEXT_HERO_REPLY_CURIOUS = "어떤 의뢰를 하고 계신지 여쭤봐도 될까요?";
        public static string TEXT_HERO_REPLY_PLAIN = "술 더 필요하시면 부르세요.";

        public static readonly string[] HERO_REACT_WARM =
        {
            "…고마워요. 그런 걸 물어봐 주는 사람은 드물거든요.",
            "(잠깐 멈췄다가 시선을 돌리며) …별일 없어. 신경 쓰지 마.",
            "무사하냐고? 후후, 다정하네. 그런 사람은 오래 못 버티는데.",
            "하하! 걱정해 주는 건가? 우린 질기다네. 고맙군.",
            "응! 다들 살아 있어! 너도 밥 잘 먹어야 해!"
        };

        public static readonly string[] HERO_REACT_CURIOUS =
        {
            "(수첩을 덮으며) 의뢰 내용은 말할 수 없어요. 미안해요.",
            "(눈을 가늘게 뜨며) 일꾼이 그건 왜 궁금한데?",
            "비밀이야. …그래도 묻는 눈빛은 마음에 드네.",
            "북쪽 숲이지. 별건 아니야. 자네가 갈 곳은 아니고.",
            "음… 그건 단장이 말하지 말랬어. 근데 숲이야! 앗."
        };

        public static readonly string[] HERO_REACT_PLAIN =
        {
            "네, 그렇게요.",
            "그래. 가 봐.",
            "응, 수고해.",
            "그래, 고맙네!",
            "응! 이따 또 와!"
        };

        // 친해졌을 때 덤으로 흘리는 현장 이야기(루틴 설계안 — 관계와 투자의 다리)
        public static string TEXT_HERO_FIELD_TALK = "그러고 보니 현장에서 봤는데, {0} 쪽 사정이 이렇더라. \"{1}\"";

        // ── 여관 선택지 ──────────────────────────
        public static string TEXT_HERO_TABLE = "용사단 테이블로 간다";
        public static string TEXT_HERO_TABLE_NOTE = "{0}분 · 일꾼으로서 말을 건다";
        public static string TEXT_HERO_TABLE_AWAY = "용사단은 원정을 떠났다 · {0}일째 귀환 예정";
        public static string TEXT_HERO_TABLE_ASK = "누구에게 말을 걸까.";
        public static string TEXT_HERO_TALKED_TODAY = "오늘은 이미 이야기했다";
        public static string TEXT_HERO_DAY_OFF = "용사단과 쉬는 날을 보낸다";
        public static string TEXT_HERO_DAY_OFF_NOTE = "영주 시찰일 · 지하시장도 그들도 쉰다";

        // ── 시찰일 관계 이벤트 ────────────────────
        public static string TEXT_DAY_OFF_TAG = "영주 시찰일";
        public static string TEXT_DAY_OFF_OPEN = "시찰일이라 지하가 닫혔다. 여관은 오랜만에 한가했고, 용사단은 창가 자리를 차지했다.";
        public static string TEXT_DAY_OFF_INVITE = "(검사가 턱짓으로 빈자리를 가리킨다) 오늘은 손님도 없잖아. 앉아.";
        public static string TEXT_DAY_OFF_CLOSE = "해가 질 때까지 이야기가 이어졌다. 다음 원정은 내일 떠난다고 했다.";
        public static readonly string[] HERO_DAY_OFF_FAVORITE =
        {
            "(학자가 조용히 다가와) …오늘 얘기, 즐거웠어요. 일꾼 씨는 우리를 사람으로 대해 주네요.",
            "(검사가 나가다 멈춰 서서, 뒤를 보지 않고) …다음에도 자리 비워 둘게.",
            "(마도사가 귓가에 속삭인다) 너, 뭔가 숨기고 있지? 괜찮아. 나도 그래.",
            "(전사가 어깨를 두드리며) 자네 같은 친구가 있어서 이 영지가 좋다네.",
            "(요정이 손바닥만 한 꽃을 내민다) 이거 너 줄게! 오늘 고마웠어!"
        };

        // ── 인트로 (웹툰 1~6화) ───────────────────
        public static string TEXT_INTRO_SKIP = "건너뛰기";
        public static string TEXT_INTRO_COUNT = "{0} / {1} · 클릭하거나 스페이스로 넘긴다";

        // ── 지하거래소 발견 (웹툰 5~6화를 직접 겪는다) ──
        public static string TEXT_DISCOVER_TAG = "여덟째 날";
        public static string TEXT_DISCOVER_OWNER = "(여관 주인이 턱짓한다) 손 비었으면 창고에서 술통 좀 올려 와.";
        public static string TEXT_DISCOVER_OVERHEAR = "(구석 자리, 두건 쓴 손님이 낮게 말한다) \"귀환 소식 떴어. 지금 팔아.\"";
        public static string TEXT_DISCOVER_THINK = "…팔아? 뭘?";
        public static string TEXT_DISCOVER_CELLAR = "술통을 옮기러 내려간 창고 안쪽에서 사람들 소리가 났다. 매수, 전량 매도, 칠판 위의 숫자들.";
        public static string TEXT_DISCOVER_END = "사고파는 건 물건이 아니었다. 모험가의 성공과 실패에 돈이 몰리고 있었다. …이건, 주식판이잖아.";
        public static string TEXT_DISCOVER_NOTE = "여관 창고 안쪽 — 이제 「창고로 내려간다」로 지하거래소에 갈 수 있다.";

        // ── 거래소의 용사단 종목 ──────────────────
        public static string TEXT_HERO_STOCK_NAME = "용사단";

        // ── 시나리오 붕괴 (상장흐름 설계안 §1-5 — 게임이 더 진행되지 않는다) ──
        public static string TEXT_COLLAPSE_TAG = "시나리오 붕괴";
        public static string TEXT_COLLAPSE_1 = "그 뒤로 용사단은 여관에 오지 않았다.";
        public static string TEXT_COLLAPSE_2 = "10년 뒤 그 자리에 설 사람이 없다. 화면 속에서 본 멸망이, 이번에는 끝까지 갈 것이다.";
        public static string TEXT_COLLAPSE_3 = "…다시, 처음부터.";
        public static string TEXT_HERO_STOCK_COLLAPSE = "용사단 종목이 무너졌다. {0}주를 주당 {1:N1}에 청산했습니다.";
        public static string TEXT_HERO_STOCK_COLLAPSE_NONE = "용사단 종목이 무너졌다. 지하세계가 술렁인다.";

        // ── 원정과 귀환 ──────────────────────────
        public static string TEXT_EXPEDITION_TAG = "원정 귀환";
        public static string TEXT_RETURN_OPEN = "용사단이 돌아왔다. 문이 열리자 여관이 한꺼번에 들썩였다.";
        public static string TEXT_RETURN_GOOD = "(전사가 웃으며 방패를 내려놓는다) 이번엔 다들 멀쩡하네! 어디 사는 누군지 모를 의뢰주 덕이지.";
        public static string TEXT_RETURN_HURT = "(요정이 붕대를 감은 채 웃어 보인다) 괜찮아… 조금 다쳤을 뿐이야. 약이 모자랐거든.";
        public static string TEXT_RETURN_SUSPECT = "(학자가 의뢰서 두 장을 나란히 놓는다) 같은 사람이 낸 의뢰예요. 글씨가 같아요. 그리고 둘 다… 우리가 이 여관에서 한 이야기를 알고 있어요.";
        public static string TEXT_RETURN_COLLAPSE = "아무도 돌아오지 않았다. 원정에서 단이 흩어졌다는 소식만 늦게 도착했다.";
    }
}
