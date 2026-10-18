using Underwrite.Enumeration;
using UnityEngine.InputSystem;

namespace Underwrite.Global
{
    /// <summary>
    /// 문자열 상수. 태그·파라미터·경로처럼 외부 에셋과 묶인 계약과 표시용 문자열을 모두 모은다.
    /// 코드 규칙 §7 참고. 표시용은 TEXT_ 접두사를 쓴다.
    /// </summary>
    /// <summary>
    /// 문자열 상수.
    ///
    /// 화면에 나가는 문구(TEXT_*)는 <c>static</c>이다. 표(UITextTable)가 덮어쓰기 때문이다.
    /// <c>const</c>면 컴파일 시점에 호출부에 박혀 표가 아무 소용이 없다.
    /// 애니메이터 파라미터 이름처럼 코드 계약인 값은 <c>const</c>로 남긴다.
    /// 여기 적힌 한국어는 표가 없을 때 쓰는 기본값이다. 화면에 열쇠 이름이 뜨는 일은 없다.
    /// </summary>
    public static partial class StringConstants
    {
        // Animator 계약. 아트 문서와 Controller에 묶여 있다.
        public const string ANIM_PARAM_DIRECTION_X = "DirectionX";

        // 용사단 사정의 열쇠. 귀환 장면과 이야기 주제가 이 사정이 풀렸는지를 본다.
        public const string NEED_HEALER_ID = "need_healer";

        // 저장 파일. Application.persistentDataPath 아래에 둔다(PC·안드로이드 공통).
        public const string SAVE_FILE_NAME = "underwrite_save.json";
        public const string SAVE_TEMP_SUFFIX = ".tmp";
        public const string ANIM_PARAM_DIRECTION_Y = "DirectionY";
        public const string ANIM_PARAM_SPEED = "Speed";

        public const string ANIM_STATE_IDLE = "Idle";
        public const string ANIM_STATE_WALKING = "Walking";
        public const string ANIM_STATE_CHEER = "Cheer";
        // 아직 그림이 없는 몸짓. Animator에 이 이름의 상태가 생기면 그것을 재생한다(CharacterActor).
        public const string ANIM_STATE_NOD = "Nod";
        public const string ANIM_STATE_SHAKE = "Shake";
        public const string ANIM_STATE_HOP = "Hop";

        // 머리 위 감정 표시
        public static string TEXT_EMOTE_EXCLAIM = "!";
        public static string TEXT_EMOTE_QUESTION = "?";
        public static string TEXT_EMOTE_DOTS = "…";
        public static string TEXT_EMOTE_MUSIC = "♪";
        public static string TEXT_EMOTE_HEART = "♥";
        public static string TEXT_EMOTE_COIN = "＄";

        // 시장 화면 표시용
        public static string TEXT_MARKET_PHASE_CLOSED = "폐장";
        public static string TEXT_MARKET_PHASE_OPEN = "개장";
        public static string TEXT_MARKET_PHASE_HALTED = "휴장";

        public static string TEXT_SENTIMENT_CALM = "평온";
        public static string TEXT_SENTIMENT_FEVER = "투자 열풍";
        public static string TEXT_SENTIMENT_FEAR = "공포";
        public static string TEXT_SENTIMENT_DRAINED = "자금 부족";

        public static string TEXT_ORDER_FILLED = "체결되었습니다.";
        public static string TEXT_ORDER_MARKET_CLOSED = "지하시장이 열려 있지 않습니다.";
        public static string TEXT_ORDER_PAUSED = "시간이 멈춘 동안에는 주문할 수 없습니다.";
        public static string TEXT_ORDER_NOT_AT_MARKET = "지하시장에서만 주문할 수 있습니다.";
        public static string TEXT_ORDER_NO_CASH = "현금이 부족합니다.";
        public static string TEXT_ORDER_NO_SHARES = "보유 지분이 부족합니다.";
        public static string TEXT_ORDER_BAD_QUANTITY = "수량은 1 이상이어야 합니다.";
        public static string TEXT_ORDER_BAD_PRICE = "호가를 올바르게 지정하세요.";
        public static string TEXT_ORDER_TOO_MANY = "이 종목에 걸 수 있는 주문 수를 넘었습니다.";
        public static string TEXT_ORDER_QUEUED = "지정가 주문을 걸었습니다. 가격이 닿으면 체결됩니다.";

        // 거래 화면 문구. 표(UITextTable)가 덮어쓴다.
        public static string TEXT_SESSION_MORNING = "오전장";
        public static string TEXT_SESSION_EVENING = "오후장";
        public static string TEXT_SESSION_NEXT_MORNING = "다음 날 오전장";
        public static string TEXT_SIDE_BUY = "매수";
        public static string TEXT_SIDE_SELL = "매도";
        public static string TEXT_SESSION_OPENED = "{0}이 열렸습니다.";
        public static string TEXT_SESSION_END = "{0} 끝내기";
        public static string TEXT_SESSION_OPEN_NEXT = "{0} 열기";
        public static string TEXT_LEAVE_MARKET = "지하시장 나가기";
        public static string TEXT_SPEED = "{0}배";
        public static string TEXT_SESSION_SKIPPED = "장을 넘겼습니다. 소식 {0}건.";
        public static string TEXT_SESSION_SKIPPED_FILLED = "장을 넘겼습니다. 소식 {0}건, 지정가 체결 {1}건.";
        public static string TEXT_AUTO_PAUSED = "결과가 확정되어 자동 정지했습니다.";
        public static string TEXT_ORDER_CANCELLED = "주문을 취소했습니다.";
        public static string TEXT_HINT_MARKET = "체결 시 가격이 달라질 수 있어요.";
        public static string TEXT_HINT_LIMIT = "지정가에 닿으면 체결, 폐장 때 소멸.";
        public static string TEXT_SUBMIT_HALTED = "휴장 중";
        public static string TEXT_SUBMIT_PAUSED = "정지 중";
        public static string TEXT_SUBMIT_NO_SHARES = "보유 주식 없음";
        public static string TEXT_CHART_HALTED = "휴장이라 가격이 갱신되지 않아요.";
        public static string TEXT_SCENE_PUBLIC = "지하세계가 같은 순간에 들었습니다. 가격은 이미 움직였습니다.";
        public static string TEXT_SCENE_PRIVATE = "아직 시장에 돌지 않은 이야기입니다.";
        public static string TEXT_INFO_EMPTY = "아직 들어온 소식이 없습니다.";
        public static string TEXT_HOLDINGS_EMPTY = "보유한 종목이 없습니다.";
        public static string TEXT_TAG_PUBLIC = "시장에 공개됨";
        public static string TEXT_TAG_PRIVATE = "나만 아는 정보";
        public static string TEXT_INFO_NOT_PUBLIC = "아직 시장에 공개되지 않음.";

        // 상장 흐름 (상장흐름 설계안 §3)
        public static string TEXT_LISTING_ARRIVED = "{0}{이} 영지에 도착했다. 다음 계절 상장 예정 · 예상 공모가 {1:N1}";
        public static string TEXT_BAND_ARRIVED = "갓 도착 · 다음 계절 상장 예정";
        public static string TEXT_BAND_LISTED = "상장 중";
        public static string TEXT_BAND_HELD = "보유 {0}주";
        public static string TEXT_INFO_HELD = "보유 {0}주";
        public static string TEXT_INFO_WHEN = "{0} · {1}일 {2}";
        public static string TEXT_INFO_PRIVATE_SHORT = "나만 앎";
        public static string TEXT_INFO_EMPTY_MINE = "가진 종목에 대한 소식이 아직 없습니다.";
        public static string TEXT_HELD_NOTE = "★ 내 보유 종목 · {0}주";

        public static string TEXT_SOURCE_TAVERN_TALK = "여관 소문";
        public static string TEXT_SOURCE_GUILD_RECORD = "길드 기록";
        public static string TEXT_SOURCE_PRIVATE_LETTER = "사적 서신";
        public static string TEXT_SOURCE_MARKET_BOARD = "시장 게시판";
        public static string TEXT_SOURCE_BROKER_LEAK = "중개인 누설";

        // 영지에서 묻기 전에 고르는 자리
        public static string TEXT_GATHER_ASK = "들은 게 있긴 한데, 공짜로 줄 수는 없지요.";
        public static string TEXT_GATHER_ASK_AGAIN = "또 볼일이 있으신가요?";
        public static string TEXT_GATHER_PAY = "{0:N0}을 내고 묻는다";
        public static string TEXT_GATHER_PAY_NOTE = "행동력 {0} · 가진 돈 {1:N0}";
        public static string TEXT_GATHER_NO_MONEY = "돈이 모자란다 · 가진 돈 {0:N0}";
        public static string TEXT_GATHER_LEAVE = "그냥 간다";

        // 여관 창고 아래 지하거래소 (웹툰 6화)
        public static string TEXT_CELLAR_GO = "창고로 내려간다";
        public static string TEXT_CELLAR_NOTE = "술통 너머, 지하거래소";
        public static string TEXT_GATHER_NO_TIME = "행동력이 없다";

        // 영지에서 맡는 일 (기획서 §10 종잣돈 퀘스트)
        public static string TEXT_WORK_NOTE = "행동력 {0} · {1:N0}을 받는다";
        public static string TEXT_WORK_NO_TIME = "행동력이 모자란다 · 남은 행동력 {0}";
        public static string TEXT_WORK_RUMOR = "그러고 보니 일하다 들었는데, {0} 쪽에 무슨 일이 있답니다.";
        public static string TEXT_WORK_RESULT = "{0:N0}을 받았다. 남은 행동력 {1}.";

        // 영지 메뉴와 원형 시계 (2026-10-17)
        public static string TEXT_MENU = "메뉴";
        public static string TEXT_MENU_MARKET = "지하거래소로";
        public static string TEXT_MENU_MARKET_LOCKED = "아직 모른다";
        public static string TEXT_MENU_NO_TIME = "다음 장이 코앞이다";
        public static string TEXT_CLOCK_TITLE = "몇 시까지 보낼까";
        public static string TEXT_CLOCK_PICK = "{0}까지 · {1}시간 {2}분";
        public static string TEXT_CLOCK_LIMIT = "다음 장은 {0}에 열린다. 그 직전까지 고를 수 있다.";
        public static string TEXT_CLOCK_CONFIRM = "그때까지 보낸다";
        public static string TEXT_CLOCK_CANCEL = "그만둔다";
        public static string TEXT_CLOCK_AM = "AM";
        public static string TEXT_CLOCK_PM = "PM";
        public static string TEXT_TIME_PASS = "{0}  →  {1}";
        public static string TEXT_TIME_PASS_NOTE = "{0}시간 {1}분이 흘렀다";
        public static string TEXT_CLOCK_TOMORROW = "내일 {0}";
        public static string TEXT_CLOCK_SKIP_SESSION = "{0} 건너뛰기 — 그다음 장 직전까지 돌릴 수 있다";
        public static string TEXT_CLOCK_SKIP_REOPENING = "다음 오후장은 재개장이다 — 건너뛸 수 없다";

        // 하루 끝 (기획서 §11의 넷째 단계 — 정보 정리 및 취침)
        public static string TEXT_SKIP_BREAK = "시간 넘기기";
        public static string TEXT_MARKET_OPENS_AT = "장이 닫혔다 · 다음 장 {0}";
        public static string TEXT_MARKET_CLOSED_NOTE = "장이 닫혔다";

        // 후원 — 익명의 의뢰주 (후원흐름 설계안). 용사단 이름은 임시다(2026-10-17).
        public static string TEXT_HERO_PARTY = "새벽별 용사단";
        public static string TEXT_REQUEST_POST = "익명 의뢰를 낸다";
        public static string TEXT_REQUEST_POST_NOTE = "봉인한 의뢰서와 보수 예치금";
        public static string TEXT_REQUEST_POST_DEADLINE = "봉인한 의뢰서와 보수 예치금 · 들은 사정의 기한까지 {0}일";
        public static string TEXT_REQUEST_CLERK = "길드 서기";
        public static string TEXT_REQUEST_ASK = "어떤 의뢰입니까? 의뢰주 이름은 비워 두셔도 됩니다. 보수만 먼저 맡기시면 됩니다.";
        public static string TEXT_REQUEST_PURPOSE_HEALING = "치료비를 선금으로 주는 의뢰";
        public static string TEXT_REQUEST_PURPOSE_GEAR = "장비 수리비를 선금으로 주는 의뢰";
        public static string TEXT_REQUEST_PURPOSE_SUPPLIES = "식량과 물자를 미리 대 주는 의뢰";
        public static string TEXT_REQUEST_PURPOSE_TRAINING = "교관을 붙여 주는 의뢰";
        public static string TEXT_REQUEST_COST_NOTE = "예치금 {0:N0} · 가진 돈 {1:N0}";
        public static string TEXT_REQUEST_CANCEL = "그만둔다";
        public static string TEXT_REQUEST_DONE = "봉인한 의뢰서를 창구에 맡겼다. 수신인은 {0}. 의뢰주 칸은 비워 두었다.";
        public static string TEXT_REQUEST_DONE_NOTE = "{0:N0}을 맡겼습니다. 반응은 여관에서 들을 수 있다.";

        public static string TEXT_NEED_HEALER_RUMOR = "(술을 나르다 들었다) \"치유사가 지난 원정에서 크게 다쳤어. 약값이 없으니 큰 의뢰는 전부 물렸지.\"";
        public static string TEXT_NEED_HEALER_MET = "(술을 나르다 들었다) \"우리 앞으로 의뢰가 들어왔어. 치료비를 선금으로 준다고… 누가 우리 사정을 이렇게 잘 알지?\"";
        public static string TEXT_NEED_SHIELD_RUMOR = "(술을 나르다 들었다) \"방패가 쪼개졌는데 대장간에 맡길 돈이 없어. 이대로 나가면 앞줄이 버티질 못해.\"";
        public static string TEXT_NEED_SHIELD_MET = "(술을 나르다 들었다) \"수리비를 먼저 주는 의뢰라니. 방패 얘기는 여기서밖에 안 했는데.\"";
        public static string TEXT_HERO_REACTION_MISSED = "(술을 나르다 들었다) \"이상한 의뢰가 하나 들어왔더라. 고마운 일이긴 한데, 지금 우리한테 필요한 건 그게 아니야.\"";

        public static string TEXT_DAY_END_DEADLINE = "들은 용사단 사정의 기한까지 {0}일.";
        // 「1년 봄 2일째」. 해·계절 이름·계절 안의 날.
        public static string TEXT_SEASON_DAY = "{0}년 {1} {2}일째";
        public static string TEXT_ARRIVALS_HEAD = "다음 계절 상장 예정";
        public static string TEXT_ARRIVAL_ROW = "{0} · 영지에 와 있다";
        // 재개장 때 시장이 남기는 알림. 상장 폐지(보유 없음 · 청산)와 신규 상장.
        public static string TEXT_NOTICE_DELIST_NONE = "{0} 상장 폐지. 보유분 없음.";
        public static string TEXT_NOTICE_DELIST_SOLD = "{0} 상장 폐지. {1}주를 주당 {2:N1}에 청산했습니다.";
        public static string TEXT_NOTICE_LISTED = "{0} 신규 상장. 공모가 {1:N1}.";
        // 지하 방 칠판의 분필 글씨. 종목(세 칸) · 현재가(한 칸) · 장 시작 대비(한 칸).
        public static string TEXT_CHALK_PRICE = "{0:N1}";
        public static string TEXT_CHALK_CHANGE = "{0}{1:N1}%";
        public static string TEXT_CHALK_UP = "▲";
        public static string TEXT_CHALK_DOWN = "▼";
        public static string TEXT_CHALK_FLAT = "";
        public static string TEXT_DELIST_NOTICE = "{0} 다음 계절 상장 폐지 예고. 청산율 {1:N0}퍼센트.";
        public static string TEXT_DELIST_ROW = " 다음 계절 상장 폐지 예고 · 청산율 {0:N0}퍼센트";
        public static string TEXT_REOPEN_GAP = "재개장 — 원정 결과가 알려진 만큼 값이 움직인 채 열렸다. 뛴 종목 {0} · 떨어진 종목 {1}";
        public static readonly string[] TEXT_SEASON_NAMES = { "봄", "여름", "가을", "겨울" };

        // 장소 이름표 아래 줄 — 거기서 무엇을 할 수 있는가(2026-10-17)
        public static string TEXT_SIGN_TAVERN = "소문 · 일거리 · 용사단";
        public static string TEXT_SIGN_GUILD = "기록 · 익명 의뢰 · 일거리";
        public static string TEXT_SIGN_SMITH = "장비 사정 · 일거리";
        public static string TEXT_SIGN_MANOR = "서신";
        public static string TEXT_SIGN_BOARD = "공개된 소식";

        // 영지에서 정보를 파는 주민(장소의 주인). 지하 정보 타임에서는 거래소를 운영하는 「정보상」이 전한다(2026-10-17).
        // 순서는 InfoSource와 같다: 여관 소문 · 길드 기록 · 서신 · 게시판 · 중개 귀띔 · 대장간
        public static string TEXT_FOLK_INNKEEPER = "여관 주인";
        public static string TEXT_FOLK_ARCHIVIST = "기록 담당";
        public static string TEXT_FOLK_STEWARD = "영주관 집사";
        public static string TEXT_FOLK_BROKER = "게시판 앞 중개인";
        public static string TEXT_FOLK_SMITH = "대장장이";
        public static string TEXT_INFORMANT = "정보상";

        // 지하 방 (2026-10-17). 거래판은 주식판에서 열고, 소식은 정보상에게 가서 듣는다.
        public static string TEXT_ROOM_BACK = "방으로 · Tab";
        public static string TEXT_ROOM_BOARD = "E · 거래판을 본다";
        public static string TEXT_ROOM_INFORMANT_WAITING = "E · 정보상에게 소식을 듣는다";
        public static string TEXT_ROOM_INFORMANT = "E · 정보상";
        public static string TEXT_ROOM_HINT = "Tab · 거래판";
        public static string TEXT_ROOM_CALL = "소식이 왔소.";
        public static string TEXT_ROOM_IDLE_1 = "장이 열리면 다시 오시오.";
        public static string TEXT_ROOM_IDLE_2 = "…볼일 있소?";
        public static string TEXT_ROOM_WAITING_NOTE = "정보상이 부른다 · 시간이 멈췄다";

        // 투자꾼의 혼잣말. 종목이 크게 움직이면 근처 사람이 한마디 한다.
        public static string TEXT_INVESTOR_UP_1 = "좋아, 좋아!";
        public static string TEXT_INVESTOR_UP_2 = "봐, 내 말이 맞지?";
        public static string TEXT_INVESTOR_UP_3 = "지금 들어갔어야 했는데…";
        public static string TEXT_INVESTOR_DOWN_1 = "망했네.";
        public static string TEXT_INVESTOR_DOWN_2 = "팔아, 지금 팔아!";
        public static string TEXT_INVESTOR_DOWN_3 = "…이럴 줄 알았지.";
        public static string TEXT_VILLAGE_NEXT_SESSION = "다음 장까지 {0}시간 {1}분";
        public static string TEXT_DAY_END_WARNING = "여관 단골들 사이가 뒤숭숭하다. 무슨 일이 있는 모양이다.";
        public static string TEXT_DAY_END_FAILED = "용사단의 사정 하나를 놓쳤다. 그 값은 원정에서 치르게 된다.";

        // 영지 사람에게 말 걸기
        public static string TEXT_NPC_TALK_KEY = "E — 말을 건다";
        public static string TEXT_NPC_ROLE_RESIDENT = "주민";
        public static string TEXT_NPC_ROLE_VENDOR = "노점상";
        public static string TEXT_NPC_ROLE_ADVENTURER = "모험단";
    }

    /// <summary>
    /// 숫자·조정값 상수. 가격 모델의 계수는 전부 여기에 모아 한곳에서 조정한다.
    /// </summary>
    /// <summary>
    /// 공용 상수.
    ///
    /// 밸런스를 보면서 고치는 값은 <c>static</c>이다. 표(MarketTuningData)가 장을 열기 전에
    /// 덮어쓰기 때문이다. <c>const</c>면 컴파일 시점에 박혀 표가 아무 소용이 없다.
    /// 구조와 안전장치(PRICE_FLOOR, BOOK_LEVELS 등)는 <c>const</c>로 남긴다.
    /// 표에서 0이나 음수로 바뀌면 시뮬레이션이 깨지는데 인스펙터에는 그걸 막을 방법이 없다.
    /// </summary>
    public static class Constants
    {
        // ── 시장 시간 ──────────────────────────────
        /// <summary>한 틱이 나타내는 게임 내 분. 가격은 이 간격으로 갱신된다.</summary>
        public const int MARKET_MINUTES_PER_TICK = 5;
        /// <summary>한 장의 길이(분). 6시간이다. 72틱이 된다.</summary>
        public const int MARKET_SESSION_MINUTES = 360;
        /// <summary>
        /// 실시간 1초가 흐르는 게임 내 분. 배속 1.0 기준.
        /// 0.6667이면 한 틱이 실시간 7.5초, 한 장이 약 9분이다.
        /// 2026-10-18 0.3333(장 18분)에서 올림: 목표 플레이 시간 20~30시간(사장님)에 40계절을 담으려면 장이 절반이어야 한다.
        /// 틱 하나의 게임 내 의미(5분, 가격 모델)는 그대로라 시장 균형은 바뀌지 않고 실시간 흐름만 빨라진다.
        /// 표(MarketTuningData)가 이 값을 덮어쓴다.
        /// </summary>
        public static float MARKET_MINUTES_PER_REAL_SECOND = 0.6667f;

        /// <summary>오전장 개장 시각. 08시부터 14시까지다(시간구조 설계안 §1).</summary>
        public const int MARKET_MORNING_START_HOUR = 8;

        /// <summary>오후장 개장 시각. 18시부터 자정까지다. 오후 2시~6시가 영지의 4시간이다.</summary>
        public const int MARKET_EVENING_START_HOUR = 18;

        /// <summary>
        /// 장중 정보 타임이 열리는 틱. 한 장에 두 번이다.
        /// 지하세계로 모험 진행 상황이 들어오는 순간이고, 그때 시장이 함께 반응한다.
        /// 72틱 장에서 24·48이면 장 시작 3분·6분 지점이다(1배속).
        /// </summary>
        public const int INFO_TIME_FIRST_TICK = 24;

        /// <summary>두 번째 정보 타임. 장의 2/3 지점이다.</summary>
        public const int INFO_TIME_SECOND_TICK = 48;

        /// <summary>한 장에 열리는 정보 타임 횟수.</summary>
        public const int INFO_TIMES_PER_SESSION = 2;

        /// <summary>
        /// 한 계절(= 주기, 원정 한 번)에 플레이하는 날 수. 40계절이 10년이다. 시간구조 설계안 §2.
        /// 첫날 귀환·재개장(오후장만) → 준비 2일 → 출발하는 날(밤에 출발). 장은 1 + 2 × 3 = 7번이다.
        /// 2026-10-18 사장님 결정으로 4일.
        /// </summary>
        public static int SEASON_DAYS = 4;
        /// <summary>
        /// 겨울(해마다 넷째 계절)의 플레이 날 수. 재개장과 출발뿐이라 장이 3번이다(2026-10-18).
        /// 10년이 약 34시간 → 장 건너뛰기를 섞으면 25~30시간(결정기록의 추산).
        /// </summary>
        public static int WINTER_SEASON_DAYS = 2;
        public const int SEASONS_PER_YEAR = 4;

        /// <summary>
        /// 거품 시장(기획서 §7, 2026-10-18 과장 결정). 해마다 계절이 열릴 때 시장에 들어오는 돈의 비율 [해 - 1].
        /// 누적 규모(첫 계절 = 1): 1년 끝 약 1.1 · 3년 약 1.9 · 5년 약 6 · 7년 약 18 · 8년 끝 약 27(정점) → 10년 끝 약 5.5.
        /// 9~10년째 음수는 표의 자리만 잡아 둔 것 — 붕괴는 침략 징조 사건과 함께 따로 설계한다.
        /// 근거와 주인공 목표 자산 표: Documents/Claude/Underwrite_거품시장_설계안.md
        /// </summary>
        public static readonly float[] MARKET_INFLOW_BY_YEAR =
        {
            0.03f, 0.05f, 0.10f, 0.14f, 0.16f, 0.16f, 0.14f, 0.10f, -0.10f, -0.25f
        };

        // 모험단 도감의 주기 문장이 시장에 남기는 것. 순서는 StringConstants.CODEX_SMITH·CODEX_TAVERN과 같다.
        // 대장간 사정이 주기 끝에 가치에 더하는 변화율 [종목][주기 순번]. 대장간 장부는 틀리지 않는다.
        public static readonly float[][] CODEX_SMITH_EFFECT =
        {
            new[] { -0.08f, 0.06f, -0.04f },
            new[] { 0.02f, 0.04f, -0.10f },
            new[] { -0.08f, 0.15f, -0.10f },
            new[] { 0.03f, 0.08f, 0f },
            new[] { 0.05f, 0.08f, 0.01f }
        };

        // 여관의 말이 맞았을 때 가치에 더하는 변화율 [종목][주기 순번]
        public static readonly float[][] CODEX_TAVERN_EFFECT =
        {
            new[] { 0.12f, -0.05f, 0.18f },
            new[] { -0.12f, 0.05f, -0.08f },
            new[] { -0.15f, 0.15f, -0.10f },
            new[] { -0.06f, -0.06f, 0.10f },
            new[] { 0.03f, -0.12f, -0.14f }
        };

        // 여관의 말이 맞을 확률 [종목][주기 순번]. 주기 끝에 정해진다 — 판마다 다르다
        public static readonly float[][] CODEX_TAVERN_TRUTH =
        {
            new[] { 0.4f, 0.7f, 0.35f },
            new[] { 0.5f, 0.6f, 0.5f },
            new[] { 0.6f, 0.5f, 0.5f },
            new[] { 0.4f, 0.5f, 0.5f },
            new[] { 0.5f, 0.4f, 0.45f }
        };

        // 새 용병단(2026-10-17 사용자 결정). 상장 조건 없이 자동 상장 — 도착하고 한 주기 뒤에 오른다.
        // 매 주기 이만큼 도착하고, 상장됐거나 도착한 단이 이만큼을 넘지 않게 한다(용사단 제외).
        public static int BAND_ARRIVALS_PER_CYCLE = 2;
        public static int BAND_PRESENT_MAX = 10;

        // 한 단이 상장돼 머무는 주기 수(이상, 이하)
        public static int BAND_STAY_MIN = 3;
        public static int BAND_STAY_MAX = 6;

        // 도감 묶음별 성격. 순서는 CODEX_FIXED와 같다: 신생(붉은 칼날) · 노련(참나무) · 고위험(검은 늪) · 외지 신입(파수대) · 외지 노련(등불)
        public static readonly StockPersonality[] BAND_PROFILE_PERSONALITY =
        {
            StockPersonality.Personality_Rookie, StockPersonality.Personality_Veteran,
            StockPersonality.Personality_HighRisk, StockPersonality.Personality_Rookie,
            StockPersonality.Personality_Veteran
        };

        // 묶음별 내재가치·유동성 범위 [묶음][0=최소, 1=최대]. 처음 다섯 단의 값 언저리다.
        public static readonly float[][] BAND_PROFILE_VALUE =
        {
            new[] { 35f, 55f }, new[] { 100f, 170f }, new[] { 60f, 95f }, new[] { 45f, 70f }, new[] { 130f, 190f }
        };

        public static readonly float[][] BAND_PROFILE_LIQUIDITY =
        {
            new[] { 50f, 90f }, new[] { 300f, 420f }, new[] { 120f, 180f }, new[] { 70f, 110f }, new[] { 260f, 360f }
        };

        // 묶음별 떠나는 사유. 고위험은 이 확률로 전멸(예고 없음), 아니면 다른 영지로 옮긴다.
        public static readonly DelistReason[] BAND_PROFILE_DEPARTURE =
        {
            DelistReason.Delist_Moved, DelistReason.Delist_Retired, DelistReason.Delist_Wiped,
            DelistReason.Delist_Moved, DelistReason.Delist_Moved
        };

        public static float BAND_WIPE_CHANCE = 0.5f;

        // 영지를 걷는 파티. 한 주(이 날 수)에 한 번은 모든 단이 나오게 한다.
        public static int PARTY_WEEK_DAYS = 7;

        // 여관 소문은 장 사람들도 듣는다. 주기 초에 말하는 변화의 이만큼이 기대로 먼저 붙는다.
        public static float CODEX_TAVERN_CROWD_SHARE = 0.5f;

        // 재개장 갭(시간구조 설계안 §2). 원정 결과로 바뀐 목표가까지의 거리 가운데 이만큼을 장이 열리자마자 건너뛴다.
        // 나머지는 장중에 따라간다 — 열자마자 다 반영되면 정보 타임에 할 일이 없다.
        public static float REOPEN_GAP_SHARE = 0.6f;
        // 이보다 작게 움직인 종목은 재개장 알림에서 오르내린 수로 세지 않는다.
        public static float REOPEN_GAP_MIN_RATIO = 0.005f;
        // 재개장 장의 정보 타임에 원정 결과가 공개될 때, 남은 거리의 이만큼을 한 번 더 건너뛴다.
        public static float INFO_RELEASE_JUMP = 0.5f;

        // 계절 계획(시간구조 설계안 §3). 여관 일 한 달에 버는 돈, 짐수레 한 달에 오르는 호감.
        // 여관 일 셋이면 시작 자금(500)의 절반쯤 — 투자 자금이 필요한 계절에 고를 만하게.
        public static float PLAN_INN_EARN = 80f;
        public static int PLAN_WAGON_AFFINITY = 1;
        // 가장 친한 용사의 호감이 이만큼이면 원정지에서 편지가 온다.
        public static int HERO_LETTER_AFFINITY = 2;

        // 데모는 이 계절의 귀환 장면에서 끝난다(2026-10-18 인터뷰 — 데모는 계절 1~2개).
        // 셋째 계절 첫날 귀환이 둘째 계절 원정(방패 사정)의 결과다.
        public static int DEMO_END_SEASON = 3;

        // 지하 방 칠판에 쓰는 줄 수(칠판 격자의 가로줄 수와 같다)와 다시 쓰는 간격(초).
        public const int CHALK_ROWS = 4;
        public static float CHALK_REFRESH_SECONDS = 0.5f;

        // 인트로 움직이는 웹툰. 칸을 옮기는 시간(초), 칸이 떠 있는 동안 다가가는 정도와 걸리는 시간.
        // 칸 경계는 장마다 다르다 — 그림의 흰 칸 사이 띠를 재서 적었다(원본 887×1774 기준 px, 칸마다 위·아래).
        // 장이 바뀌면 다시 재야 한다. 표에 없는 장은 네 칸으로 고르게 나눈다.
        public static readonly int[][] INTRO_PANELS =
        {
            new[] { 0, 397, 403, 824, 830, 1298, 1303, 1774 },
            new[] { 0, 429, 437, 900, 908, 1283, 1291, 1774 },
            new[] { 0, 435, 445, 887, 893, 1311, 1320, 1774 },
            new[] { 0, 395, 405, 790, 798, 1193, 1202, 1774 },
            new[] { 0, 449, 456, 792, 799, 1161, 1166, 1774 },
            new[] { 0, 386, 391, 811, 816, 1209, 1215, 1711 }
        };
        public const int INTRO_PANELS_PER_PAGE = 4;
        public static float INTRO_PAN_SECONDS = 0.7f;
        public static float INTRO_ZOOM_AMOUNT = 0.04f;
        public static float INTRO_ZOOM_SECONDS = 8f;

        // 소리(2026-10-18). 설정 화면이 생기기 전까지의 기본 크기와 음악을 갈아 끼우는 시간(초).
        public static float AUDIO_MUSIC_VOLUME = 0.45f;
        public static float AUDIO_EFFECT_VOLUME = 0.7f;
        public static float AUDIO_MUSIC_FADE_SECONDS = 1.5f;
        // 메뉴 「소리」의 단계: 크게 · 작게 · 끔. 순서는 TEXT_VOLUME_STEPS와 같다.
        public static readonly float[] AUDIO_VOLUME_STEPS = { 1f, 0.4f, 0f };

        // 장부 맞추기(길드 기록실). 들어가는 데 드는 시간, 잘못 짚을 때마다 더 드는 시간, 쫓겨나는 의심.
        // 묻기(60분·돈)보다 짧지만 틀리면 길어진다. 장부의 단 수는 4~6.
        public static int LEDGER_MINUTES = 30;
        public static int LEDGER_WRONG_MINUTES = 10;
        public static int LEDGER_MAX_SUSPICION = 3;
        public static int LEDGER_MIN_ROWS = 4;
        public static int LEDGER_MAX_ROWS = 6;
        // 지급 기록이 며칠 전까지 거슬러 올라가는가.
        public static int LEDGER_DAYS_BACK = 3;

        // 주문서 읽기(대장간). 꽂이의 주문서 수, 걸리는 시간, 잘못 짚을 때마다 더 드는 시간, 쫓겨나는 의심.
        public static int SMITH_ORDER_SLIPS = 6;
        public static int SMITH_ORDER_MINUTES = 30;
        public static int SMITH_ORDER_WRONG_MINUTES = 10;
        public static int SMITH_ORDER_MAX_SUSPICION = 3;

        // 그 단의 지난 계절 미끼 주문서를 넣을 확률(둘째 계절부터). 취소된 미끼는 늘 넣는다.
        public static float SMITH_ORDER_OLD_DECOY_CHANCE = 0.6f;

        // 길드 기록 짜맞추기. 걸리는 시간, 맞춰 볼 기회, 틀릴 때마다 더 드는 시간.
        public static int DEDUCE_MINUTES = 30;
        public static int DEDUCE_TRIES = 3;
        public static int DEDUCE_WRONG_MINUTES = 10;

        // 연습 기록의 씨앗. 늘 같은 판이 나온다.
        public static int DEDUCE_PRACTICE_SEED = 7;

        // 프롤로그 순찰(일곱째 날). 정거장 순서와 품삯 — 여관 · 대장간 · 길드 · 게시판 · 영주관.
        // 시작 돈은 0이고 연습 판 미니게임 하나에 60씩(2026-10-18 사장님). 이 돈과 주방 일 품삯이 첫 거래 밑천이 된다.
        public static readonly InfoSource[] PROLOGUE_STOPS =
        {
            InfoSource.Source_TavernTalk, InfoSource.Source_SmithOrder, InfoSource.Source_GuildRecord,
            InfoSource.Source_MarketBoard, InfoSource.Source_PrivateLetter
        };

        public static readonly float[] PROLOGUE_WAGES = { 60f, 60f, 60f, 0f, 0f };
        public static float PROLOGUE_GOAL = 180f;

        // 잠에서 깨는 여덟째 날 아침(하루 기준 분). 시장 달력의 1년 봄 1일째다.
        public static int PROLOGUE_WAKE_MINUTE = 7 * 60 + 50;

        // 프롤로그 장소 이동(순간이동). 문 앞 칸에서 조금 아래에 세운다. 암전 시간(초).
        public static readonly UnityEngine.Vector2 PROLOGUE_WARP_OFFSET = new UnityEngine.Vector2(0f, -0.6f);
        public static float BLINK_FADE_SECONDS = 0.35f;
        public static float BLINK_HOLD_SECONDS = 0.2f;

        // 장소에 닿았을 때 돌아볼 주인을 찾는 거리(유닛).
        public static float PROLOGUE_GREET_RADIUS = 4f;

        // 장면 전환 제목 카드. 어두워지고 밝아지는 시간과 글씨가 머무는 시간(초, 실시간).
        public static float TITLE_CARD_FADE_SECONDS = 0.8f;
        public static float TITLE_CARD_HOLD_SECONDS = 1.6f;

        // 첫 지하 방문 — 「시간이 흐르기 시작했다」 안내가 떠 있는 시간(초).
        public static float MTUT_FLOW_COACH_SECONDS = 6f;

        // 영지 바닥 오브젝트 이름(VillagePartsSetup이 만든다). 미니맵이 그 그림을 쓴다.
        public const string VILLAGE_GROUND_NAME = "Ground";

        // 미니맵 그림의 가로 픽셀. 펼친 지도(화면의 84%)에서도 흐리지 않을 만큼.
        public static int MINIMAP_SNAPSHOT_WIDTH = 1600;

        // 영지 행동력(2026-10-19 사장님). 휴장마다 받는 양과 행동마다 드는 양.
        public static int AP_MORNING = 3;
        public static int AP_AFTERNOON = 3;
        public static int AP_NIGHT = 2;
        public static int AP_GATHER = 1;
        public static int AP_TALK = 1;
        public static int AP_MINIGAME = 1;
        public static int AP_WORK = 2;

        // 게임 안 웹툰: 액자 높이(화면 높이 대비), 7장 칸 경계(Outputs/Webtoon_ch7_v1/panels.json).
        public static float WEBTOON_FRAME_HEIGHT = 0.86f;
        public static float WEBTOON_FRAME_WIDTH = 0.9f;
        /// <summary>하루의 분.</summary>
        public static int MINUTES_PER_DAY = 24 * 60;
        /// <summary>이 의뢰 순번을 넘긴 저장은 용사단을 이미 만난 것이다(옛 저장 보정).</summary>
        public static int QUEST_HEROES_MET_INDEX = 3;
        /// <summary>규칙서 움직이는 그림 한 걸음(ms).</summary>
        public static long RULE_STEP_MS = 900;
        /// <summary>규칙서에서 눌린 칸이 원래대로 돌아오기까지(ms).</summary>
        public static long RULE_PRESS_MS = 150;
        public static readonly int[] WEBTOON_CH7_PANELS = { 0, 419, 427, 800, 808, 1212, 1220, 1774 };

        // 인트로에서 보이는 웹툰 장 수. 나머지(5~6장)는 창고 발견 장면에서 쓴다.
        public static int INTRO_PAGES_SHOWN = 4;

        // 여관 주사위 내기. 눈 수, 한 사람의 주사위 수, 이겨야 하는 판 수, 걸리는 시간, 지면 내는 술값.
        public const int DICE_FACES = 6;
        public static int DICE_PER_PLAYER = 4;
        public static int DICE_ROUNDS_TO_WIN = 2;
        public static int DICE_MINUTES = 30;
        public static float DICE_STAKE = 10f;

        // 상대의 판단. 플레이어가 부른 말이 맞을 확률이 (기준 − 대담함 × 폭)보다 낮으면 의심한다.
        // 올릴 말이 이 확률보다 낮으면 차라리 의심한다. 대담함 × 비율의 확률로 하나 더 얹어 허세를 부른다.
        public static float DICE_DOUBT_BASE = 0.45f;
        public static float DICE_DOUBT_BOLD = 0.25f;
        public static float DICE_GIVE_UP_CHANCE = 0.25f;
        public static float DICE_BLUFF_RATE = 0.4f;
        public static float DICE_BOLDNESS_MIN = 0.15f;
        public static float DICE_BOLDNESS_MAX = 0.85f;

        // 상대가 생각하는 척하는 시간(초, 실시간). 바로 받아치면 기계 같다.
        public static float DICE_THINK_SECONDS = 0.9f;

        // 주문서 카드의 기울기(도). 자리 순서대로 돌려 쓴다.
        public static readonly float[] SMITH_SLIP_TILTS = { -1.6f, 0.9f, -0.5f, 1.3f, -1.1f, 0.6f };

        // 주문 나르기(여관·대장간·길드의 일). 주문표 수, 한 장의 물건 가짓수, 외우는 시간(초, 실시간).
        // 품삯은 맞힌 비율만큼이되 다 틀려도 최저는 받는다. 대충 끝내면 절반.
        public static int WORK_ORDERS = 3;
        public static int WORK_ITEMS_MIN = 2;
        public static int WORK_ITEMS_MAX = 3;
        public static int WORK_MEMORIZE_SECONDS = 7;
        public static float WORK_MIN_PAY = 0.4f;
        public static float WORK_SLOPPY_PAY = 0.5f;

        // 여관 엿듣기. 귀 기울일 기회, 한 번에 드는 시간(분), 용병단 테이블 수, 한 줄을 다 듣는 데 필요한 머묾.
        public static int EAVES_TURNS = 4;
        public static int EAVES_MINUTES = 10;
        public static int EAVES_BAND_TABLES = 3;
        public static int EAVES_LISTENS_TO_FINISH = 2;

        /// <summary>
        /// 영지에서 흐르는 시간의 속도. 기획서 §11에서 실시간 1:1로 확정했다.
        /// 장중과 달리 압축하지 않는다. 그 시간이 플레이어의 것이기 때문이다.
        /// </summary>
        public static float BREAK_MINUTES_PER_REAL_SECOND = 1f / 60f;

        /// <summary>
        /// 「시간 넘기기」로 건너뛸 때 다음 장 몇 분 전에 멈추는가.
        /// 바로 개장에 떨어뜨리면 숨 돌릴 틈 없이 장이 시작된다.
        /// </summary>
        public static int BREAK_SKIP_MARGIN_MINUTES = 10;

        /// <summary>
        /// 영지에서 한 가지 일을 하는 데 드는 게임 시간(분).
        /// 영지의 자원은 횟수가 아니라 시간이다. 시간을 다 쓰면 장이 열린다.
        /// </summary>
        public static int GATHER_MINUTES_PER_VISIT = 60;

        /// <summary>상장 자리의 기본 수. 설계안 §6에 따라 분위기와 연차로 늘거나 준다.</summary>
        public static int LISTING_SLOTS = 3;

        /// <summary>전멸 청산율. 잔여 자산만 돌려받는다.</summary>
        public static float LIQUIDATION_WIPED = 0.15f;

        /// <summary>은퇴 청산율. 예고가 있으므로 미리 팔 수 있다.</summary>
        public static float LIQUIDATION_RETIRED = 0.85f;

        /// <summary>영지 이동 청산율.</summary>
        public static float LIQUIDATION_MOVED = 0.75f;

        // ── 가격 모델 (기획서 §7의 6요인) ──────────
        /// <summary>가격이 목표가를 향해 올라갈 때의 수렴 속도.</summary>
        public static float PRICE_REVERSION_PER_TICK = 0.02f;
        /// <summary>
        /// 가격이 목표가를 향해 내려갈 때의 수렴 속도. 올라갈 때보다 빠르다.
        /// 실제 급등주는 무너지는 속도가 오르는 속도보다 훨씬 빠르다. GameStop 2021은
        /// 오르는 데 20일, 정점에서 80퍼센트 빠지는 데 3거래일이 걸렸다.
        /// 상한(PRICE_MAX_DROP_PER_TICK)은 극단적 충격만 막고, 평소의 붕괴 속도는 이 값이 만든다.
        /// </summary>
        public static float PRICE_REVERSION_DOWN_PER_TICK = 0.035f;
        /// <summary>틱마다 더하는 무작위 변동의 기본 폭. 종목 성격과 분위기로 배율이 걸린다.</summary>
        public static float PRICE_NOISE_BASE = 0.0032f;

        /// <summary>
        /// 추세 지속. 직전 틱의 흐름이 다음 틱에 남는 비율.
        /// 0이면 매 틱이 서로 독립이라 가격이 한 수준에서 맴돌고, 그 수준만 알면 사고파는 게 공짜가 된다.
        /// 실제 주가가 예측하기 어려운 이유는 변동이 커서가 아니라 방향이 이어지기 때문이다.
        /// </summary>
        public static float PRICE_MOMENTUM_RETENTION = 0.62f;

        /// <summary>미세 호가를 다시 뽑는 간격(실시간 초). 틱과 무관하게 돈다.</summary>
        public static float MARKET_MICRO_SECONDS = 0.12f;

        /// <summary>미세 호가 흔들림 한 번의 크기. 틱 종가 대비 비율이다.</summary>
        public static float MICRO_AMPLITUDE = 0.0013f;

        /// <summary>미세 흔들림이 남는 비율. 1에 가까울수록 천천히 제자리로 돌아온다.</summary>
        public static float MICRO_RETENTION = 0.72f;

        /// <summary>미세 흔들림의 한계. 틱 종가에서 이보다 멀어지지 않는다.</summary>
        public static float MICRO_LIMIT = 0.007f;
        /// <summary>기대가 실제 가치로 식어가는 속도. 소문은 시간이 지나면 힘을 잃는다.</summary>
        public static float EXPECTATION_DECAY_PER_TICK = 0.02f;
        /// <summary>주문 한 주가 유동성 대비 가격을 밀어내는 정도.</summary>
        public static float ORDER_IMPACT_SCALE = 0.9f;
        /// <summary>한 틱에 허용하는 최대 상승률. 급등을 허용하되 폭주는 막는다.</summary>
        public static float PRICE_MAX_MOVE_PER_TICK = 0.18f;
        /// <summary>
        /// 한 틱에 허용하는 최대 하락률. 상승 상한보다 크다.
        /// 실제 급등주는 무너지는 속도가 오르는 속도보다 훨씬 빠르다.
        /// GameStop 2021은 오르는 데 20일, 정점에서 80퍼센트 빠지는 데 3거래일이 걸렸다.
        /// 조사 기록: Documents/Claude/Underwrite_시장모델_실제자료_샘플.md
        /// </summary>
        public static float PRICE_MAX_DROP_PER_TICK = 0.30f;
        /// <summary>가격 하한. 0 이하로 내려가지 않게 한다.</summary>
        public const float PRICE_FLOOR = 1f;

        // ── 종목 성격별 배율 ───────────────────────
        // 현실 대형주의 일 변동성은 1퍼센트대다. 모델은 짧은 장에서 체감이 필요해 더 크게 잡지만,
        // 베테랑만은 낮춰 종목 성격 사이의 대비를 키운다. 전부 현실에 맞추면 셋 다 밋밋해진다.
        public static float VOLATILITY_VETERAN = 0.45f;
        public static float VOLATILITY_ROOKIE = 1.8f;
        public static float VOLATILITY_HIGH_RISK = 2.4f;
        public static float VOLATILITY_FAMOUS = 1.0f;
        /// <summary>용사단 지수. 거의 흔들리지 않는다.</summary>
        public static float VOLATILITY_INDEX = 0.15f;

        // ── 용사단 지수 (상장흐름 설계안 §1-4·§1-5) ──
        public const string HERO_STOCK_ID = "STOCK_HEROES";
        /// <summary>처음 가치.</summary>
        public static float HERO_INDEX_VALUE = 100f;
        /// <summary>두꺼운 호가. 지수는 큰돈이 들어가도 잘 움직이지 않는다.</summary>
        public static float HERO_INDEX_LIQUIDITY = 2000f;
        /// <summary>
        /// 장마다 오르는 내재가치. 한 주기(4장) 약 2%, 40주기 약 2.2배 — 설계안의 "분기 +2%".
        /// 안전하게만 가면 반드시 모자라야 한다. 그래서 작다.
        /// </summary>
        public static float HERO_INDEX_GROWTH_PER_SESSION = 0.005f;
        /// <summary>시나리오가 깨지면 남는 가치의 비율. 실적이 끊기면 지수가 성립하지 않는다.</summary>
        // 놓친 사정의 대가가 지하에 알려질 때 용사단 종목이 떨어지는 비율. 다침 / 죽음.
        public static float HERO_WOUND_INDEX_DROP = 0.12f;
        public static float HERO_DEATH_INDEX_DROP = 0.35f;
        // 사정이 걸린 용사의 순서(HERO_NAMES와 같다). 피오(요정)·고드릭(전사).
        public const int HERO_INDEX_WARRIOR = 3;
        public const int HERO_INDEX_FAIRY = 4;

        // 파산(2026-10-18 인터뷰 — 재기는 한 번, 그다음은 게임오버). 장이 끝났을 때 가진 돈과 종목 값을 합쳐 이보다 적으면 파산이다.
        // 가장 싼 단 한 주도 못 살 만큼이다.
        public static float BANKRUPT_THRESHOLD = 30f;
        // 첫 파산 때 정보상이 쥐여 주는 재기 자금. 시작 자금의 절반.
        public static float BANKRUPT_RESCUE_CASH = 250f;

        // ── 시장 분위기별 배율 ────────────────────
        // NASDAQ 10년(2016~2025)은 상승 8년 / 하락 2년이었다. 평온한 장에 약한 상승 편향을 둔다.
        public static float SENTIMENT_DRIFT_CALM = 0.0005f;
        public static float SENTIMENT_DRIFT_FEVER = 0.004f;
        public static float SENTIMENT_DRIFT_FEAR = -0.006f;
        public static float SENTIMENT_DRIFT_DRAINED = -0.002f;

        public static float SENTIMENT_VOLATILITY_CALM = 1.0f;
        public static float SENTIMENT_VOLATILITY_FEVER = 1.6f;
        public static float SENTIMENT_VOLATILITY_FEAR = 2.0f;
        public static float SENTIMENT_VOLATILITY_DRAINED = 1.2f;

        // ── 플레이어 자금 ─────────────────────────
        /// <summary>시작 현금. 2026-10-18 사장님: 0에서 시작 — 프롤로그 품삯으로 첫 밑천을 번다.</summary>
        // 튜닝 에셋(MarketTuningData)도 같은 값 — 영지는 에셋을 읽기 전에 이 값을 쓴다.
        public static float STARTING_CASH = 0f;

        // ── 호가와 지정가 ─────────────────────────
        /// <summary>호가창에 보여줄 매수·매도 각 단계 수.</summary>
        public const int BOOK_LEVELS = 5;
        /// <summary>
        /// 시장가 주문이 먹어 들어갈 수 있는 호가 단계의 한도. 화면에는 5단계만 보이지만 그 밖에도 호가는 있다.
        /// 이것을 넘는 주문은 마지막 가격에 남은 수량을 채운다.
        /// </summary>
        public const int BOOK_SWEEP_LIMIT = 40;
        /// <summary>호가 단위를 가격의 몇 배로 잡을지. 126.8이면 약 0.5가 된다.</summary>
        public static float BOOK_TICK_RATIO = 0.004f;
        /// <summary>호가 단위의 하한.</summary>
        public const float BOOK_TICK_MIN = 0.1f;
        /// <summary>1단계 호가에 쌓이는 수량을 유동성의 몇 배로 잡을지.</summary>
        public static float BOOK_DEPTH_BASE = 0.35f;
        /// <summary>단계가 멀어질수록 수량이 늘어나는 비율.</summary>
        public static float BOOK_DEPTH_GROWTH = 0.45f;
        /// <summary>한 종목에 동시에 걸 수 있는 지정가 주문 수.</summary>
        public const int PENDING_MAX_PER_STOCK = 5;

        // ── 차트 ──────────────────────────────────
        /// <summary>종목별로 보관하는 가격 이력 길이.</summary>
        public const int PRICE_HISTORY_CAPACITY = 240;

        // ── 영지 앞뒤 정렬 ────────────────────────
        /// <summary>
        /// 발밑 높이 1유닛을 정렬 순서 몇 칸으로 나눌지. 100이면 0.01유닛 차이까지 앞뒤를 가린다.
        /// 영지가 위아래로 ±300유닛을 넘으면 sortingOrder 범위(±32767)를 벗어난다.
        /// </summary>
        public const int VILLAGE_SORT_PER_UNIT = 100;
        /// <summary>바닥판의 정렬 순서. 어떤 사물·사람보다도 뒤에 깔린다.</summary>
        public const int VILLAGE_SORT_GROUND = -32000;
        /// <summary>
        /// 주인공 루트에서 발바닥까지의 거리(월드 유닛). 스프라이트 피벗이 Center라서 루트는 몸 한가운데다.
        /// 캔버스 724px의 아래 13px이 발바닥이고, PPU 100 × 축척 0.27이면 (362 − 13) × 0.0027 ≈ 0.94다.
        /// </summary>
        public const float PROTAGONIST_FOOT_OFFSET = -0.94f;

        // ── 영지 NPC ──────────────────────────────
        /// <summary>길 찾기 격자 한 칸(월드 유닛). 사람 발 폭(0.55)보다 조금 작게 잡는다.</summary>
        public const float VILLAGE_NAV_CELL = 0.4f;
        /// <summary>격자 칸을 막혔다고 볼 때 더하는 여유. 벽에 몸을 비비며 걷지 않게 한다.</summary>
        public const float VILLAGE_NAV_CLEARANCE = 0.25f;
        /// <summary>NPC 걷는 속도. 주인공(3.0)보다 느려야 영지를 산책하는 사람으로 보인다.</summary>
        public static float NPC_WALK_SPEED = 1.5f;

        // 주인공 걸음 빠르기(PlayerCharacterInput). 걷기 그림의 보폭이 이 빠르기에 맞춰져 있다.
        public static float PROTAGONIST_WALK_SPEED = 3f;
        /// <summary>모험단 안에서 앞사람과의 간격(월드 유닛).</summary>
        public static float NPC_PARTY_SPACING = 0.8f;
        /// <summary>목적지에 닿은 뒤 머무는 시간 범위(초).</summary>
        public static float NPC_PARTY_REST_MIN = 4f;
        public static float NPC_PARTY_REST_MAX = 10f;
        /// <summary>모험단이 장소에 닿았을 때 문 앞에서 아래로 비켜 서는 거리(월드 유닛).</summary>
        public static float NPC_PARTY_DOOR_GAP = 2.2f;
        /// <summary>그 둘레에서 흩어지는 반지름. 단마다 같은 점에 겹쳐 서지 않게 한다.</summary>
        public static float NPC_PARTY_SPREAD = 1.5f;
        /// <summary>모험단이 닿았을 때 한 사람이 들떠 몸짓하는 확률.</summary>
        public static float NPC_PARTY_CHEER_CHANCE = 0.45f;
        /// <summary>말을 걸 수 있는 거리(월드 유닛). 사람 키(1.9)보다 조금 작다.</summary>
        public static float NPC_TALK_RANGE = 1.4f;

        // ── 용사단과 쉬는 날 ──────────────
        /// <summary>
        /// 용사단과 쉬는 날은 계절의 이 날이다 — 계절마다 큰 관계 장면이 하나. 영주 시찰(지하 휴장)은 뺐다(2026-10-19 사장님).
        /// 준비 둘째 날로 둔 까닭: 첫날은 재개장·귀환, 마지막 날은 출발 전이라 장면이 겹친다.
        /// </summary>
        public static int HERO_DAY_OFF_SEASON_DAY = 3;

        // ── 용사단 일정 (루틴 설계안) ─────────────
        /// <summary>여관 테이블에서 한 사람과 이야기하는 데 드는 영지 시간(분).</summary>
        public static int HERO_TALK_MINUTES = 60;
        /// <summary>이만큼 친해지면 현장 이야기를 덤으로 흘린다(관계와 투자의 다리).</summary>
        public static int HERO_FIELD_TALK_AFFINITY = 3;

        // 용사 이야기 주제가 열리는 호감. 따뜻하게 대답하면 하루에 2씩 오른다(HeroTopics).
        public static int HERO_TOPIC_AFFINITY_FIRST = 0;
        public static int HERO_TOPIC_AFFINITY_SECOND = 3;
        public static int HERO_TOPIC_AFFINITY_THIRD = 6;

        // 같은 사람이 낸 의뢰를 이만큼 받으면 학자가 의뢰주를 의심한다(후원흐름 설계안 §2-6).
        public static int HERO_SUSPECT_CLUES = 2;
        /// <summary>그 정도로 친할 때 현장 이야기가 나올 확률.</summary>
        public static float HERO_FIELD_TALK_CHANCE = 0.5f;

        // ── 후원 (익명의 의뢰주) ──────────────────
        /// <summary>못 들은 사정의 기한이 이만큼 남으면 하루 결산에 경고를 띄운다.</summary>
        public static int PATRONAGE_WARNING_DAYS = 2;

        // 저장. 판이 바뀌어 옛 저장을 읽을 수 없게 되면 판 번호를 올린다 — 이어하기 단추가 사라진다.
        public const int SAVE_VERSION = 2;

        // 영지에 있는 동안 이 간격(실제 초)마다 저장한다. 대화가 끝날 때·시간을 넘길 때·나갈 때도 저장한다.
        public static float SAVE_INTERVAL_SECONDS = 30f;

        // 지하 방. 주식판·정보상에 말을 걸 수 있는 거리(유닛), 정보상이 부르는 간격(초),
        // 투자꾼이 반응하는 한 틱 등락률과 그 사이 쉬는 시간(초).
        public static float ROOM_REACH_DISTANCE = 2.4f;

        // 장소 이름표를 문 앞 자리에서 얼마나 위에 띄우는가(유닛). 문 위, 처마 아래쯤.
        public static float SIGN_HEIGHT_UNITS = 2.9f;

        // 정보를 산 뒤 대화창을 닫고 건네는 장면을 보이는 시간(초). 건네기(Give) 1.3초 + 주인공 반응.
        public static float HAND_OVER_SECONDS = 1.8f;

        // 이 시각(시)부터 다락방에서 잘 수 있다. 오후장이 끝나는 자정부터다(설계안 §1). 24시 이상은 다음 날 새벽.
        public static int SLEEP_FROM_HOUR = 24;

        // 영지의 하루(시간구조 설계안 §1). 상점 앞·노점·문 앞 주민은 21시에 들어가고, 거리에 남은 모험단도
        // 03시에는 사라진다. 06시에 모두 다시 나온다.
        public static int VILLAGE_TOWNSFOLK_LEAVE_HOUR = 21;
        public static int VILLAGE_ADVENTURERS_LEAVE_HOUR = 3;
        public static int VILLAGE_WAKE_HOUR = 6;

        // 캐릭터 외곽선(SpriteOutline). 두께는 월드 유닛 — 화면 1배에서 약 1.5px, 확대하면 함께 굵어진다.
        // 2px·진하기 90%는 사람이 스티커처럼 떠 보였다(2026-10-17). 얇고 옅게, 색은 영지 건물 테두리의
        // 붉은 갈색을 어둡게 — 가까이서는 선이 있는지 모를 만큼, 멀리서는 윤곽이 또렷하게.
        // 2026-10-18 끔: 기본 화면 크기에서 사람이 탁해지고 작은 부분이 뭉개졌다(사장님 보고). 대신 발밑 그림자를 깐다.
        public static bool OUTLINE_ENABLED = false;
        public static float OUTLINE_WIDTH_UNITS = 0.021f;
        public static UnityEngine.Color OUTLINE_COLOR = new UnityEngine.Color(0.26f, 0.16f, 0.11f, 0.6f);
        public static float ROOM_CALL_INTERVAL_SECONDS = 3f;
        public static float ROOM_INVESTOR_MOVE_RATIO = 0.012f;
        public static float ROOM_INVESTOR_COOLDOWN_SECONDS = 2.5f;
        /// <summary>익명 의뢰를 꾸며 맡기는 데 드는 영지 시간(분).</summary>
        public static int REQUEST_MINUTES = 30;
        /// <summary>목적별 예치금. 시작 자금 500에 견주었다. 시제품 검증에서 조정한다.</summary>
        public static float REQUEST_COST_HEALING = 120f;
        // 원정지로 보내는 익명 의뢰는 길드를 한 번 더 거치므로 비싸다(시간구조 설계안 §3 — 원격 익명 의뢰).
        public static float REMOTE_REQUEST_MARKUP = 1.3f;
        public static float REQUEST_COST_GEAR = 150f;
        public static float REQUEST_COST_SUPPLIES = 80f;
        public static float REQUEST_COST_TRAINING = 100f;

        // ── 영지 카메라 확대 (2026-10-17) ─────────
        /// <summary>가장 가까이 당겼을 때의 Orthographic Size. 기본 7.5에서 약 1.7배 크게 보인다.</summary>
        public static float VILLAGE_ZOOM_NEAREST = 4.5f;
        /// <summary>휠 한 칸에 바뀌는 크기.</summary>
        public static float VILLAGE_ZOOM_STEP = 0.5f;
        /// <summary>화면이 목표 크기를 따라가는 빠르기. 클수록 빨리 붙는다.</summary>
        public static float VILLAGE_ZOOM_SMOOTH = 10f;
        /// <summary>이보다 작은 휠 값은 무시한다. 터치패드의 잔 움직임이다.</summary>
        public const float VILLAGE_ZOOM_SCROLL_THRESHOLD = 0.01f;

        // ── 필드 연기 (2026-10-17) ────────────────
        /// <summary>감정 표시가 떠 있는 시간(초).</summary>
        public static float ACTOR_EMOTE_SECONDS = 1.6f;
        /// <summary>말풍선이 떠 있는 시간(초).</summary>
        public static float ACTOR_SAY_SECONDS = 3.2f;
        /// <summary>그림이 없는 몸짓을 코드로 대신할 때의 길이(초).</summary>
        public static float ACTOR_FALLBACK_SECONDS = 0.6f;
        /// <summary>다가가 E를 누른 뒤 대화창이 열리기까지. 서로 돌아보는 모습이 보일 만큼.</summary>
        public static float ACTOR_APPROACH_SECONDS = 0.45f;
        /// <summary>이 거리 안에 들어오면 서 있는 사람이 주인공을 돌아본다.</summary>
        public static float ACTOR_NOTICE_RANGE = 2.6f;
        /// <summary>같은 사람이 다시 알아보는 표시를 띄우기까지(초).</summary>
        public static float ACTOR_NOTICE_COOLDOWN = 25f;
        /// <summary>노점상이 손님을 부르는 거리와 간격(초).</summary>
        public static float VENDOR_CALL_RANGE = 7f;
        public static float VENDOR_CALL_MIN = 7f;
        public static float VENDOR_CALL_MAX = 13f;
        /// <summary>시간을 넘긴 뒤 주민이 집 근처에서 옮겨 서는 반지름.</summary>
        public static float ACTOR_SHUFFLE_RADIUS = 2f;
        /// <summary>시간 넘기기 연출: 어두워지기·머물기·밝아지기(초).</summary>
        public static float TIME_PASS_FADE = 0.35f;
        public static float TIME_PASS_HOLD = 1.1f;
        /// <summary>영지 수첩에 보여 줄 소식 수.</summary>
        public const int NOTEBOOK_MAX_ROWS = 30;

        // ── 대화 ──────────────────────────────────
        /// <summary>선택지를 고르는 숫자 키. 화면에 붙는 번호 「1.」「2.」와 같은 순서다.</summary>
        public static readonly Key[] DIALOGUE_CHOICE_KEYS =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };
    }
}
