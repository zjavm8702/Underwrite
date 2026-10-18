using Underwrite.Enumeration;

namespace Underwrite.Story
{
    /// <summary>
    /// 이야기의 큰 고비를 지났는지 적어 둔다. 씬을 넘나들어야 하므로 정적이다.
    /// 저장 기능이 들어오면 함께 저장한다.
    /// </summary>
    public static class StoryProgress
    {
        /// <summary>
        /// 여관 창고 안쪽의 지하거래소를 찾았는가. 웹툰 5~6화를 플레이어가 직접 겪은 뒤에 참이 된다.
        /// 그 전에는 거래소로 내려가는 길이 보이지 않는다(2026-10-17 사용자 결정 — 마을 사건을 먼저 겪는다).
        /// </summary>
        public static bool CellarFound { get; set; }

        /// <summary>지하거래소 첫 안내를 마쳤는가(건너뛴 것도 마친 것으로 본다).</summary>
        public static bool MarketTutorialDone { get; set; }

        /// <summary>영지 첫 안내를 마쳤는가.</summary>
        public static bool VillageTutorialDone { get; set; }

        /// <summary>잠자리 혼잣말 가운데 이미 나온 것(상수 이름). 한 번 나온 고비의 혼잣말은 다시 나오지 않는다.</summary>
        public static readonly System.Collections.Generic.HashSet<string> SleepSeen =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>파산한 횟수. 한 번은 재기하고, 두 번째는 게임오버다(2026-10-18 인터뷰).</summary>
        public static int Bankruptcies { get; set; }

        /// <summary>프롤로그(지하를 찾기 전의 마을 생활)의 어디쯤인가. 옛 저장은 창고를 찾았으면 끝으로 친다.</summary>
        public static PrologueStage Prologue { get; set; }

        /// <summary>
        /// 여관에서 용사단을 알아보는 장면을 봤는가(온보딩의 동기 전환 — 「돈이 저들에게 가면 결말이 바뀔지도」).
        /// 그 전에는 용사단 테이블과 다음 할 일이 열리지 않는다.
        /// </summary>
        public static bool HeroesRecognized { get; set; }

        /// <summary>2일째 아침의 행동력 안내를 봤는가. 그 전(첫날 새벽)에는 행동력이 0이다.</summary>
        public static bool ActionTutorialDone { get; set; }

        /// <summary>첫 지하 방문 튜토리얼의 단계.</summary>
        public static MarketOnboardingStage MarketOnboarding { get; set; }

        /// <summary>
        /// 첫 지하 방문의 연습 매수 직전 현금(-1이면 아직). 연습 매도 뒤 이 금액으로 되돌린다 — 도중에 나갔다 와도 같은 값(코드 리뷰 10/18).
        /// </summary>
        public static float PracticeCash { get; set; } = -1f;

        // 하루에 한 번만 하는 정보 모으기의 마지막 날(0이면 아직). 불러온 뒤에도 같은 날 다시 못 하게 저장한다.
        public static int LedgerDay { get; set; }
        public static int SmithDay { get; set; }
        public static int DiceDay { get; set; }
        public static int DeduceDay { get; set; }

        /// <summary>데모 마무리 장면을 봤는가. 한 번만 보인다 — 그 뒤로는 그대로 이어서 둘러볼 수 있다.</summary>
        public static bool DemoEndSeen { get; set; }

        public static void Reset()
        {
            Bankruptcies = 0;
            DemoEndSeen = false;
            SleepSeen.Clear();
            CellarFound = false;
            Prologue = PrologueStage.Stage_Opening;
            MarketTutorialDone = false;
            MarketOnboarding = MarketOnboardingStage.Onboarding_Arrive;
            PracticeCash = -1f;
            HeroesRecognized = false;
            ActionTutorialDone = false;
            ActionPoints.Reset();
            LedgerDay = 0;
            SmithDay = 0;
            DiceDay = 0;
            DeduceDay = 0;
            VillageTutorialDone = false;
        }
    }
}
