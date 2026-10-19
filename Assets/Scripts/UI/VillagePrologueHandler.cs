using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Save;
using Underwrite.Story;
using Underwrite.Utility;

namespace Underwrite.UI
{
    /// <summary>
    /// 프롤로그 — 지하를 찾기 전의 마을 생활(2026-10-18 온보딩 설계안, 사장님 피드백 「동기 부여가 없다」).
    ///
    /// 일곱째 날: 혼잣말로 멸망과 「오늘 먹을 것부터」를 짚고, 시간을 멈춘 채 마을의 주요 장소를 차례로 돈다.
    /// 장소마다 주인이 무슨 일을 하는 곳인지 보여 주고 품삯을 준다. 다 돌면 여관 다락방에서 강제로 잔다.
    /// 여덟째 날(시장 달력의 1일째) 아침부터 시간이 흐르고, 여관 주방 일을 거들다 창고를 찾는다(웹툰 5~6화).
    /// 그동안 수첩·시간 넘기기·장 시각 같은 시장 쪽 기능은 보이지 않는다. 영지 화면(<see cref="VillageScreenController"/>)이 조립한다.
    /// </summary>
    public sealed class VillagePrologueHandler
    {
        private readonly VillageScreenController Host;
        private bool directing;

        // 이 단계의 길 안내를 이미 시작했는가. 단계가 바뀌면 다시 안내한다(불러온 판에서도).
        private PrologueStage? GuidedStage;

        public VillagePrologueHandler(VillageScreenController host)
        {
            Host = host;
        }

        private static PrologueStage Stage
        {
            get
            {
                return StoryProgress.Prologue;
            }
        }

        /// <summary>아직 지하를 찾기 전인가.</summary>
        public bool IsActive
        {
            get
            {
                return Stage != PrologueStage.Stage_Done;
            }
        }

        /// <summary>프롤로그가 장면을 띄우는 중인가(제목 카드 · 혼잣말 · 정거장). 다른 대화가 끼어들지 않게.</summary>
        public bool IsDirecting
        {
            get
            {
                return directing;
            }
        }

        /// <summary>프롤로그 동안은 주인공을 직접 움직이지 못한다 — 다음 장소로 데려간다(사장님 — 강제 진행).</summary>
        public bool LocksPlayer
        {
            get
            {
                return IsActive;
            }
        }

        /// <summary>할 일(QuestLog)을 받을 수 있는가 — 여덟째 날부터.</summary>
        public bool AllowsQuests
        {
            get
            {
                return Stage >= PrologueStage.Stage_Kitchen;
            }
        }

        private static bool IsPatrol
        {
            get
            {
                return Stage >= PrologueStage.Stage_Inn && Stage <= PrologueStage.Stage_Manor;
            }
        }

        private static int StopIndex
        {
            get
            {
                return (int)Stage - (int)PrologueStage.Stage_Inn;
            }
        }

        /// <summary>지하를 찾은 뒤 아직 용사단을 알아보지 못했는가. 그동안 다음 할 일과 용사단 테이블이 열리지 않는다.</summary>
        public bool AwaitsRecognition
        {
            get
            {
                return StoryProgress.FirstSessionDone == true && StoryProgress.HeroesRecognized == false;
            }
        }

        /// <summary>매 프레임. 첫 혼잣말을 띄우고, 다음에 갈 곳의 이름표를 밝힌다.</summary>
        public void Tick()
        {
            if (IsActive == false)
            {
                WorldSigns.Highlight(null);

                // 첫 장을 마치고 지하에서 올라오면 여관에서 용사단을 알아본다.
                if (AwaitsRecognition == true && directing == false && Host.CanDirect() == true)
                {
                    PlayRecognition();
                }

                // 다음 날(2일째) 아침 — 행동력 안내. (웹툰 7장 「개미의 결심」이 납품되면 그 앞에 붙는다.)
                if (StoryProgress.HeroesRecognized == true && StoryProgress.ActionTutorialDone == false
                    && Host.IsMorningBreak == true && directing == false && Host.CanDirect() == true)
                {
                    directing = true;
                    Host.PlayResolveWebtoon(() => Host.PlayActionTutorial(() => directing = false));
                }

                return;
            }

            WorldSigns.Highlight(TargetName());

            // 순찰을 다 돌면 해가 진다 — 시계가 오후에 멈춰 있으면 잠 단계도 한낮으로 비쳤다(데모 점검).
            if (Stage == PrologueStage.Stage_Sleep && Host.Clock != null
                && Host.Clock.BreakMinuteOfDay < Constants.PROLOGUE_DUSK_MINUTE)
            {
                Host.Clock.SetBreakMinute(Constants.PROLOGUE_DUSK_MINUTE);
            }

            // 첫 카드는 다른 조건을 기다리지 않고 영지가 뜨는 첫 프레임에 띄운다 — 영지가 먼저 보이지 않게.
            if (Stage == PrologueStage.Stage_Opening && directing == false)
            {
                PlayOpening();
                return;
            }

            if (directing == true || Host.Staging.IsGuiding == true || Host.CanDirect() == false)
            {
                return;
            }

            // 여덟째 날은 여관 주인의 부탁(할 일 「먹고살 길」)을 들은 뒤에 데려간다.
            if (Stage == PrologueStage.Stage_Kitchen && QuestLog.HasPendingIntro == true)
            {
                return;
            }

            if (GuidedStage != Stage)
            {
                GuidedStage = Stage;
                Guide();
            }
        }

        // 다음 장소를 비추고 문 앞까지 데려간 뒤, 그 장소의 장면을 연다.
        private void Guide()
        {
            var spot = Host.FindSpot(TargetSource());

            if (spot == null)
            {
                return;
            }

            Host.Staging.GuideTo(spot, () =>
            {
                Host.Staging.GreetAt(spot);

                if (Stage == PrologueStage.Stage_Kitchen)
                {
                    Host.StartKitchenWork(spot);
                    return;
                }

                HandleSpot(spot);
            });
        }

        /// <summary>프롤로그의 다음 목적지 이름(미니맵이 밝힌다). 없으면 null.</summary>
        public string CurrentTarget
        {
            get
            {
                return IsActive == true ? TargetName() : null;
            }
        }

        // 매 프레임 불리는 두 글의 캐시. 단계·시각·번 돈·갈 곳이 바뀔 때만 다시 만든다.
        private PrologueStage ClockStage = PrologueStage.Stage_Done;
        private int ClockSeconds = -1;
        private string ClockCache;
        private PrologueStage GoalStage = PrologueStage.Stage_Done;
        private float GoalEarned = -1f;
        private string GoalTarget;
        private string GoalCache;

        /// <summary>시계 칸. 시장 달력과 다음 장 시각은 숨긴다. <paramref name="secondsOfDay"/>는 그날 0시부터의 초.</summary>
        public string ClockLabel(int secondsOfDay)
        {
            if (ClockCache != null && ClockStage == Stage && ClockSeconds == secondsOfDay)
            {
                return ClockCache;
            }

            ClockStage = Stage;
            ClockSeconds = secondsOfDay;

            if (Stage == PrologueStage.Stage_Kitchen)
            {
                ClockCache = string.Format(StringConstants.TEXT_PROLOGUE_CLOCK, StringConstants.TEXT_PROLOGUE_DAY8,
                    ClockText.FromSeconds(secondsOfDay, true));
                return ClockCache;
            }

            var part = Stage == PrologueStage.Stage_Sleep
                ? StringConstants.TEXT_PROLOGUE_EVENING
                : StringConstants.TEXT_PROLOGUE_AFTERNOON;
            ClockCache = string.Format(StringConstants.TEXT_PROLOGUE_CLOCK, StringConstants.TEXT_PROLOGUE_DAY7, part);
            return ClockCache;
        }

        /// <summary>화면 위 목표. 여덟째 날부터는 할 일(QuestLog)이 대신한다 — 그때는 null.</summary>
        public string GoalText()
        {
            if (AllowsQuests == true)
            {
                return null;
            }

            var earned = Earned();
            var target = Stage == PrologueStage.Stage_Sleep ? null : TargetName();

            if (GoalCache != null && GoalStage == Stage && GoalEarned == earned && ReferenceEquals(GoalTarget, target) == true)
            {
                return GoalCache;
            }

            GoalStage = Stage;
            GoalEarned = earned;
            GoalTarget = target;
            var text = string.Format(StringConstants.TEXT_PROLOGUE_GOAL, earned, Constants.PROLOGUE_GOAL);

            if (Stage == PrologueStage.Stage_Sleep)
            {
                GoalCache = text + StringConstants.TEXT_PROLOGUE_GOAL_SLEEP;
            }
            else
            {
                GoalCache = target == null ? text : text + string.Format(StringConstants.TEXT_PROLOGUE_GOAL_NEXT, target);
            }

            return GoalCache;
        }

        /// <summary>
        /// 장소 앞에서 E를 눌렀다. 프롤로그가 맡으면 참 — 영지 화면은 평소 선택지를 띄우지 않는다.
        /// </summary>
        public bool HandleSpot(VillageSpot spot)
        {
            if (IsActive == false || spot == null)
            {
                return false;
            }

            if (Stage == PrologueStage.Stage_Opening)
            {
                return true;
            }

            if (Stage == PrologueStage.Stage_Kitchen)
            {
                // 여덟째 날은 여관 일만. 다른 곳은 혼잣말로 돌려보낸다.
                if (spot.InfoSource == InfoSource.Source_TavernTalk)
                {
                    return false;
                }

                Host.SayToSelf(StringConstants.TEXT_PROLOGUE_KITCHEN_ONLY);
                return true;
            }

            var target = TargetSource();

            if (spot.InfoSource != target)
            {
                var line = Stage == PrologueStage.Stage_Sleep
                    ? StringConstants.TEXT_PROLOGUE_GO_INN
                    : string.Format(StringConstants.TEXT_PROLOGUE_GO_FIRST, TargetName());
                Host.SayToSelf(line);
                return true;
            }

            if (Stage == PrologueStage.Stage_Sleep)
            {
                PlaySleep(spot);
                return true;
            }

            PlayStop(spot, StopIndex);
            return true;
        }

        /// <summary>창고를 찾았다(영지 화면의 발견 장면이 부른다). 해 질 녘으로 넘겨 첫 장(재개장)에 닿게 한다.</summary>
        public void FinishWithDiscovery(MarketClock clock)
        {
            if (Stage == PrologueStage.Stage_Done)
            {
                return;
            }

            StoryProgress.Prologue = PrologueStage.Stage_Done;

            if (clock != null)
            {
                clock.SkipBreak();
            }

            WorldSigns.Highlight(null);
        }

        private void PlayOpening()
        {
            directing = true;
            Host.Staging.ShowTitleFromDark(StringConstants.TEXT_PROLOGUE_DAY7, StringConstants.TEXT_PROLOGUE_CARD_DAY7_SUB, PlayOpeningLines);
        }

        private void PlayOpeningLines()
        {
            var lines = new List<DialogueLine>();

            foreach (var text in StringConstants.TEXT_PROLOGUE_OPENING)
            {
                lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_PROLOGUE_DAY7, text));
            }

            var inn = Host.FindSpot(InfoSource.Source_TavernTalk);

            if (inn != null)
            {
                lines.Add(Host.OwnerLineFor(inn, StringConstants.TEXT_PROLOGUE_CALL));
            }

            // 거리에 선 혼잣말이다. 여관 그림을 깔지 않는다.
            Host.PlayLines(null, lines, () =>
            {
                directing = false;
                StoryProgress.Prologue = PrologueStage.Stage_Inn;
                StoryProgress.VillageTutorialDone = true;
                Host.PlayPrologueTutorial();
                SaveSystem.Save();
            });
        }

        // 정거장 하나: 주인의 소개 → 연습 판(있으면) → 품삯 → 마무리.
        private void PlayStop(VillageSpot spot, int index)
        {
            directing = true;
            var intro = new List<DialogueLine>
            {
                Host.OwnerLineFor(spot, StringConstants.TEXT_PROLOGUE_STOP_INTRO[index])
            };

            Host.PlayLines(spot.InfoSource, intro, () =>
            {
                if (Host.StartStopPractice(spot.InfoSource, () => FinishStop(spot, index)) == false)
                {
                    FinishStop(spot, index);
                }
            });
        }

        private void FinishStop(VillageSpot spot, int index)
        {
            var lines = new List<DialogueLine>();
            var wage = Constants.PROLOGUE_WAGES[index];

            if (wage > 0f)
            {
                lines.Add(new DialogueLine(string.Empty, spot.Name, string.Format(StringConstants.TEXT_PROLOGUE_WAGE, wage)));
            }

            var outro = StringConstants.TEXT_PROLOGUE_STOP_OUTRO[index];
            lines.Add(StringConstants.TEXT_PROLOGUE_OUTRO_IS_SELF[index] == true
                ? new DialogueLine(string.Empty, spot.Name, outro)
                : Host.OwnerLineFor(spot, outro));

            Host.PlayLines(spot.InfoSource, lines, () =>
            {
                directing = false;

                if (wage > 0f)
                {
                    GatherSession.Record(0f, wage, null);
                }

                StoryProgress.Prologue = (PrologueStage)((int)Stage + 1);
                SaveSystem.Save();
            });
        }

        private void PlaySleep(VillageSpot spot)
        {
            directing = true;
            var lines = new List<DialogueLine>
            {
                Host.OwnerLineFor(spot, StringConstants.TEXT_PROLOGUE_SLEEP_OWNER),
                new DialogueLine(string.Empty, spot.Name, string.Format(StringConstants.TEXT_PROLOGUE_SLEEP_SELF, Earned()))
            };

            Host.PlayLines(spot.InfoSource, lines, () =>
            {
                // 어두운 동안 아침으로 넘긴다 — 여덟째 날, 시장 달력의 1년 봄 1일째. 여기서부터 시간이 흐른다.
                Host.Staging.ShowTitle(StringConstants.TEXT_PROLOGUE_DAY8, StringConstants.TEXT_PROLOGUE_CARD_DAY8_SUB, () =>
                {
                    StoryProgress.Prologue = PrologueStage.Stage_Kitchen;

                    if (Host.Clock != null)
                    {
                        Host.Clock.BeginBreakAt(Constants.PROLOGUE_WAKE_MINUTE);
                    }

                    SaveSystem.Save();
                }, PlayAtticMorning);
            });
        }

        // 다락방 아침 일러스트 위 혼잣말. 끝나면 여관 주인의 부탁(할 일)이 이어진다.
        private void PlayAtticMorning()
        {
            var lines = new List<DialogueLine>();

            foreach (var text in StringConstants.TEXT_PROLOGUE_ATTIC)
            {
                lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_PROLOGUE_DAY8, text));
            }

            Host.PlayScene(StringConstants.SCENE_CLASS_ATTIC, lines, () => directing = false);
        }

        // 동기 전환 장면: 여관 구석의 용사단을 알아본다(장면 일러스트 위). 끝나면 여관 주인이 다음 할 일로 이끈다.
        private void PlayRecognition()
        {
            directing = true;
            Host.Staging.ShowTitle(StringConstants.TEXT_HEROES_CARD, StringConstants.TEXT_HEROES_CARD_SUB, null, () =>
            {
                var lines = new List<DialogueLine>();

                foreach (var text in StringConstants.TEXT_HEROES_LINES)
                {
                    lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_HEROES_TAG, text));
                }

                var inn = Host.FindSpot(InfoSource.Source_TavernTalk);

                if (inn != null)
                {
                    lines.Add(Host.OwnerLineFor(inn, StringConstants.TEXT_HEROES_OWNER));
                }

                Host.PlayScene(StringConstants.SCENE_CLASS_HEROES, lines, () =>
                {
                    StoryProgress.HeroesRecognized = true;
                    directing = false;
                    SaveSystem.Save();
                });
            });
        }

        private static float Earned()
        {
            var total = 0f;
            var done = 0;

            if (IsPatrol == true)
            {
                done = StopIndex;
            }
            else if (Stage >= PrologueStage.Stage_Sleep)
            {
                done = Constants.PROLOGUE_WAGES.Length;
            }

            for (var i = 0; i < done && i < Constants.PROLOGUE_WAGES.Length; i++)
            {
                total += Constants.PROLOGUE_WAGES[i];
            }

            return total;
        }

        private static InfoSource TargetSource()
        {
            if (IsPatrol == true)
            {
                return Constants.PROLOGUE_STOPS[StopIndex];
            }

            return InfoSource.Source_TavernTalk;
        }

        private string TargetName()
        {
            if (IsPatrol == false && Stage != PrologueStage.Stage_Sleep && Stage != PrologueStage.Stage_Kitchen)
            {
                return null;
            }

            var spot = Host.FindSpot(TargetSource());
            return spot == null ? null : spot.Name;
        }
    }
}
