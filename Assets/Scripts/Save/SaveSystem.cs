using System;
using System.IO;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using Underwrite.Utility;
using UnityEngine;

namespace Underwrite.Save
{
    /// <summary>
    /// 한 칸짜리 자동 저장. 영지에 있을 때(장 사이)만 저장하고, 이어하기는 늘 영지에서 시작한다.
    ///
    /// 장중을 저장하지 않는 이유: 대기 주문·정보 타임·틱 사이 흔들림까지 되살리려면 저장할 것이 배로 는다.
    /// 장은 길어야 실제 9분이라 잃는 것이 적다. 장 사이에 저장하면 시장·지갑·관계가 모두 한 순간에 맞는다.
    /// </summary>
    public static class SaveSystem
    {
        private static string FilePath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, StringConstants.SAVE_FILE_NAME);
            }
        }

        public static bool HasSave
        {
            get
            {
                return Peek() != null;
            }
        }

        /// <summary>지금 저장할 수 있는가. 장이 열려 있거나 영지 시간이 아니면 저장하지 않는다.</summary>
        public static bool CanSave
        {
            get
            {
                var clock = BreakClock.Current;
                return clock != null && clock.IsBreak == true && clock.Phase == Enumeration.MarketPhase.Phase_Closed;
            }
        }

        /// <summary>지금 상태를 저장한다. 저장할 수 없는 때면 아무것도 하지 않고 거짓.</summary>
        public static bool Save()
        {
            if (CanSave == false)
            {
                return false;
            }

            var data = new SaveData
            {
                Version = Constants.SAVE_VERSION,
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
                Clock = BreakClock.Current.Capture(),
                HasMarket = BreakClock.Simulation != null,
                Gather = GatherSession.Capture(),
                Patronage = PatronageLedger.Capture(),
                Heroes = HeroRoster.Capture(),
                Codex = BandCodex.Capture(),
                QuestIndex = QuestLog.CurrentIndex,
                QuestIntroduced = QuestLog.Introduced,
                SeasonPlan = SeasonPlan.Capture(),
                Story = new StoryState
                {
                    CellarFound = StoryProgress.CellarFound,
                    Prologue = StoryProgress.Prologue,
                    MarketOnboarding = StoryProgress.MarketOnboarding,
                    PracticeCash = StoryProgress.PracticeCash,
                    HeroesRecognized = StoryProgress.HeroesRecognized,
                    FirstSessionDone = StoryProgress.FirstSessionDone,
                    ActionTutorialDone = StoryProgress.ActionTutorialDone,
                    ActionPoints = ActionPoints.Current,
                    ActionFilledKey = ActionPoints.FilledKey,
                    LedgerDay = StoryProgress.LedgerDay,
                    SmithDay = StoryProgress.SmithDay,
                    DiceDay = StoryProgress.DiceDay,
                    DeduceDay = StoryProgress.DeduceDay,
                    MarketTutorialDone = StoryProgress.MarketTutorialDone,
                    VillageTutorialDone = StoryProgress.VillageTutorialDone,
                    Bankruptcies = StoryProgress.Bankruptcies,
                    DemoEndSeen = StoryProgress.DemoEndSeen,
                    SponsorGuideSeen = StoryProgress.SponsorGuideSeen
                }
            };

            data.Story.SleepSeen.AddRange(StoryProgress.SleepSeen);

            if (data.HasMarket == true)
            {
                data.Market = BreakClock.Simulation.Capture();
            }

            // 쓰다가 꺼지면 저장이 반쪽이 된다. 옆에 다 쓴 뒤 바꿔 끼운다.
            var path = FilePath;
            var temp = path + StringConstants.SAVE_TEMP_SUFFIX;

            try
            {
                File.WriteAllText(temp, JsonUtility.ToJson(data));

                if (File.Exists(path) == true)
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception exception)
            {
                UnderwriteLog.Warning($"저장하지 못했습니다: {exception.Message}");
                return false;
            }

            return true;
        }

        /// <summary>저장 파일을 읽기만 한다. 없거나, 깨졌거나, 판이 다르면 null.</summary>
        public static SaveData Peek()
        {
            var path = FilePath;

            if (File.Exists(path) == false)
            {
                return null;
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                return data != null && data.Version == Constants.SAVE_VERSION ? data : null;
            }
            catch (Exception exception)
            {
                UnderwriteLog.Warning($"저장을 읽지 못했습니다: {exception.Message}");
                return null;
            }
        }

        /// <summary>저장된 시각. 이어하기 단추에 며칠째 몇 시인지 적는 데 쓴다. 저장이 없으면 null.</summary>
        public static MarketClock PeekClock()
        {
            var data = Peek();
            return data == null ? null : MarketClock.Restore(data.Clock);
        }

        /// <summary>
        /// 저장을 불러와 정적 상태를 모두 되살린다. 시장은 표로 새로 짠 뒤 덮어쓴다.
        /// 부른 쪽이 영지 씬을 연다. 실패하면 아무것도 바꾸지 않고 거짓.
        /// </summary>
        public static bool Load(GameDataSet tables)
        {
            var data = Peek();

            if (data == null)
            {
                return false;
            }

            // 표의 수치와 문구를 먼저 덮어쓴다. 시장을 짜기 전에 해야 시작 자금·문구가 맞는다.
            if (tables != null)
            {
                tables.Apply();
            }

            var clock = MarketClock.Restore(data.Clock);

            if (data.HasMarket == true)
            {
                var sim = MarketFactory.Create(tables, data.Market.Seed, data.Market.Sentiment);
                sim.Restore(data.Market, clock);
                BreakClock.Simulation = sim;
            }
            else
            {
                BreakClock.Simulation = null;
            }

            BreakClock.Current = clock;
            GatherSession.Restore(data.Gather);
            PatronageLedger.Restore(data.Patronage);
            HeroRoster.Restore(data.Heroes);
            BandCodex.Restore(data.Codex);
            QuestLog.Restore(data.QuestIndex, data.QuestIntroduced);
            SeasonPlan.Restore(data.SeasonPlan);
            StoryProgress.CellarFound = data.Story.CellarFound;
            // 프롤로그가 생기기 전 저장(10/18)은 값이 0이다. 창고를 찾았으면 끝난 것으로 본다.
            // 범위를 벗어난 값(손상)도 끝난 것으로 본다 — 멈춤 순서 배열 밖을 읽지 않게.
            var prologue = (int)data.Story.Prologue;

            if (data.Story.CellarFound == true || prologue < 0 || prologue > (int)PrologueStage.Stage_Done)
            {
                prologue = (int)PrologueStage.Stage_Done;
            }

            StoryProgress.Prologue = (PrologueStage)prologue;
            StoryProgress.MarketTutorialDone = data.Story.MarketTutorialDone;
            // 튜토리얼이 생기기 전 저장(10/18)은 값이 0이다. 거래소 안내를 이미 봤으면 끝난 것으로 본다.
            // 범위를 벗어난 값(손상·새 판의 단계)은 끝난 것으로 본다 — 튜토리얼에 갇히는 것보다 낫다.
            var onboarding = (int)data.Story.MarketOnboarding;
            var done = (int)MarketOnboardingStage.Onboarding_Done;

            if ((data.Story.MarketTutorialDone == true && onboarding == 0) || onboarding < 0 || onboarding > done)
            {
                onboarding = done;
            }

            StoryProgress.MarketOnboarding = (MarketOnboardingStage)onboarding;
            StoryProgress.PracticeCash = data.Story.PracticeCash;
            // 행동력이 생기기 전 저장(10/18)은 값이 false다 — 이미 용사단을 만난 판이면 안내를 본 것으로 쳐서 행동력을 바로 채운다.
            // 옛 저장은 행동력 열쇠가 없어 기본값(-1)이다. 지금 저장은 지하를 찾은 뒤 열쇠가 늘 있으니 false를 그대로 둔다(Codex 리뷰 10/19).
            var legacy = data.Story.ActionFilledKey < 0 && data.QuestIndex > Constants.QUEST_HEROES_MET_INDEX;
            StoryProgress.ActionTutorialDone = data.Story.ActionTutorialDone == true || legacy == true;
            // 이 장면이 생기기 전 저장(10/18)에서 이미 용사단과 이야기했으면 본 것으로 친다. 첫 장 판정이 읽으니 먼저 복원한다.
            StoryProgress.HeroesRecognized = data.Story.HeroesRecognized == true || data.QuestIndex > Constants.QUEST_HEROES_MET_INDEX;
            StoryProgress.FirstSessionDone = data.Story.FirstSessionDone == true || StoryProgress.HeroesRecognized == true
                || StoryProgress.MarketOnboarding == MarketOnboardingStage.Onboarding_Done;
            ActionPoints.Restore(data.Story.ActionPoints, data.Story.ActionFilledKey);
            StoryProgress.LedgerDay = data.Story.LedgerDay;
            StoryProgress.SmithDay = data.Story.SmithDay;
            StoryProgress.DiceDay = data.Story.DiceDay;
            StoryProgress.DeduceDay = data.Story.DeduceDay;
            StoryProgress.VillageTutorialDone = data.Story.VillageTutorialDone;
            StoryProgress.Bankruptcies = data.Story.Bankruptcies;
            StoryProgress.DemoEndSeen = data.Story.DemoEndSeen;
            // 이 안내가 생기기 전 저장(10/19)에서 이미 첫 의뢰를 냈으면 본 것으로 친다.
            StoryProgress.SponsorGuideSeen = data.Story.SponsorGuideSeen == true
                || data.QuestIndex > Constants.QUEST_FIRST_REQUEST_INDEX;
            StoryProgress.SleepSeen.Clear();
            StoryProgress.SleepSeen.UnionWith(data.Story.SleepSeen);
            return true;
        }

        /// <summary>판이 끝났다(시나리오 붕괴). 이어할 것이 없다.</summary>
        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath) == true)
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception exception)
            {
                UnderwriteLog.Warning($"저장을 지우지 못했습니다: {exception.Message}");
            }
        }
    }
}
