namespace Underwrite.Global
{
    /// <summary>
    /// 용사단 대사. 이름은 임시다(2026-10-17 사용자 요청으로 임시 이름을 붙였다. 확정되면 여기만 바꾼다).
    /// 연기 방향은 기획서 §17을 따른다. 양이 많아 영역별 partial로 나눴다(코드 규칙 §7-2).
    ///
    /// 순서는 학자 · 검사 · 마도사 · 전사 · 요정이다. <c>HERO_*</c> 배열은 모두 이 순서를 지킨다.
    /// </summary>
    public static partial class StringConstants
    {
        // ── 사람 ─────────────────────────────────
        // 임시 이름. 학자 · 검사 · 마도사(엘프) · 전사 · 요정 순
        public static readonly string[] HERO_NAMES =
        {
            "에드윈", "카이라", "엘라리아", "고드릭", "피오"
        };

        public static readonly string[] HERO_ROLES =
        {
            "용사단 · 기록과 작전", "용사단 · 선봉", "용사단 · 마도사", "용사단 · 방패", "용사단 · 치유사"
        };

        /// <summary>
        /// 표정 묶음 자산 이름. Assets/Data/Portraits/{이름}.asset.
        /// 종목 이름과 겹치지 않게 역할로 짓는다.
        /// </summary>
        public static readonly string[] HERO_FACE_ASSETS = { "scholar", "sword", "elf", "warrior", "fairy" };

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
        public static string TEXT_INTRO_CONTINUE = "이어하기 · {0} {1}";
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
        public static string TEXT_HERO_STOCK_NAME = "새벽별 용사단";

        // ── 시나리오 붕괴 (상장흐름 설계안 §1-5 — 게임이 더 진행되지 않는다) ──
        public static string TEXT_HERO_STOCK_SHOCK = "용사단의 원정 소식에 {0}{이} 주저앉았다. {1:N1} → {2:N1}";
        public static string TEXT_HERO_GONE = "더는 이 자리에 없다";

        // 파산(2026-10-18 인터뷰 — 재기는 한 번). 문구는 과장 초안.
        public static string TEXT_BANKRUPT_TAG = "파산";
        public static string TEXT_BANKRUPT_RESCUE_TITLE = "정보상이 동전 주머니를 내밀었다";
        public static string TEXT_BANKRUPT_RESCUE_BODY = "「빈손으로 계단을 오르게 둘 수는 없지. {0:N0}이다. 이자는 묻지 마. 다음엔 없어.」\n\n한 번 더 일어설 수 있다. 다음 파산은 끝이다.";
        public static string TEXT_BANKRUPT_RESCUE_OK = "받는다";
        public static string TEXT_BANKRUPT_END_TITLE = "두 번째 파산";
        public static string TEXT_BANKRUPT_END_BODY = "정보상은 이번엔 눈도 마주치지 않았다. 장부를 덮었다. 10년 뒤를 아는 사람이, 아무것도 바꾸지 못한 채.\n\n…다시, 처음부터.";
        public static string TEXT_BANKRUPT_END_OK = "처음으로";

        // ── 원정과 귀환 ──────────────────────────
        public static string TEXT_EXPEDITION_TAG = "원정 귀환";
        public static string TEXT_RETURN_OPEN = "용사단이 돌아왔다. 문이 열리자 여관이 한꺼번에 들썩였다.";
        public static string TEXT_RETURN_GOOD = "(전사가 웃으며 방패를 내려놓는다) 이번엔 다들 멀쩡하네! 어디 사는 누군지 모를 의뢰주 덕이지.";
        public static string TEXT_RETURN_SUSPECT = "(학자가 의뢰서 두 장을 나란히 놓는다) 같은 사람이 낸 의뢰예요. 글씨가 같아요. 그리고 둘 다… 우리가 이 여관에서 한 이야기를 알고 있어요.";
        // 귀환에서 드러나는 놓친 사정의 대가. 순서는 HERO_NAMES와 같다. 문구는 과장 초안.
        public static readonly string[] HERO_RETURN_WOUNDED =
        {
            "(학자가 팔을 감싸 쥔 채 웃는다) 기록하다 다친 건 처음이에요. …괜찮아요, 정말로.",
            "(검사가 붕대 감은 손을 등 뒤로 숨긴다) 별거 아니야. 검은 아직 쥘 수 있어.",
            "(엘프가 지팡이에 몸을 기댄다) 마력이 바닥났을 때 맞았어요. 며칠이면 나아요.",
            "(전사가 금 간 갑옷을 내려놓는다) 막긴 막았다. 몸으로.",
            "(요정이 붕대를 감은 채 웃어 보인다) 괜찮아… 조금 다쳤을 뿐이야. 약이 모자랐거든."
        };
        public static string TEXT_RETURN_DEATH_OPEN = "문이 열렸다. 들어오는 발소리가 하나 모자랐다.";
        public static string TEXT_RETURN_DEATH_NAME = "{0}의 자리가 비어 있었다. 아무도 그 이야기를 먼저 꺼내지 않았다.";
        public static string TEXT_RETURN_DEATH_LINE = "…돌아오는 길에, 그가 끝까지 앞에 서 있었어요.";
    }
}
