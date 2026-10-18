using NUnit.Framework;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using UnityEditor;

namespace Underwrite.Editor.Tests
{
    /// <summary>계절의 흐름 — 놓친 사정의 대가, 재개장, 계절 계획(2026-10-18).</summary>
    public sealed class SeasonFlowTests
    {
        private MarketSimulation Sim;

        [SetUp]
        public void SetUp()
        {
            var guids = AssetDatabase.FindAssets("t:GameDataSet");
            var data = AssetDatabase.LoadAssetAtPath<GameDataSet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            Sim = MarketFactory.Create(data, 1234, MarketSentiment.Sentiment_Calm);
            BreakClock.Simulation = Sim;
            BreakClock.Current = Sim.Clock;
            BandCodex.Reset();
            HeroRoster.Reset();
            PatronageLedger.Reset();
            SeasonPlan.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            BreakClock.Simulation = null;
            BreakClock.Current = null;
            BandCodex.Reset();
            HeroRoster.Reset();
            PatronageLedger.Reset();
            SeasonPlan.Reset();
        }

        [Test]
        public void MissedHealerWoundsFairyAndMissedShieldKillsWarrior()
        {
            for (var day = 1; day <= 8; day++)
            {
                PatronageLedger.EndOfDay(day);
            }

            Assert.AreEqual(HeroCondition.Condition_Wounded, HeroRoster.All[Constants.HERO_INDEX_FAIRY].Condition);
            Assert.AreEqual(HeroCondition.Condition_Dead, HeroRoster.All[Constants.HERO_INDEX_WARRIOR].Condition);

            var expected = (1f - Constants.HERO_WOUND_INDEX_DROP) * (1f - Constants.HERO_DEATH_INDEX_DROP) - 1f;
            Assert.AreEqual(expected, PatronageLedger.PendingIndexShock, 0.0001f);
            Assert.AreNotEqual(HeroRoster.All[Constants.HERO_INDEX_WARRIOR], HeroRoster.Favorite());
        }

        [Test]
        public void MetNeedsLeaveEveryoneHealthy()
        {
            PatronageLedger.Post(RequestPurpose.Purpose_Healing, 1);
            PatronageLedger.RevealEarly(StringConstants.NEED_SHIELD_ID);
            PatronageLedger.Post(RequestPurpose.Purpose_Gear, 2);

            for (var day = 1; day <= 8; day++)
            {
                PatronageLedger.EndOfDay(day);
            }

            for (var i = 0; i < HeroRoster.All.Count; i++)
            {
                Assert.AreEqual(HeroCondition.Condition_Healthy, HeroRoster.All[i].Condition);
            }

            Assert.AreEqual(0f, PatronageLedger.PendingIndexShock);
        }

        [Test]
        public void ReopeningAdvancesCycleAndHoldsResultsForInfoTimes()
        {
            Sim.OpenSession(false, MarketSession.Session_Evening);

            // 첫 계절: 1일째 오후장 + 2·3·4일째 두 번씩 = 일곱 장. 여덟째가 다음 계절 재개장이다.
            for (var n = 0; n < 7; n++)
            {
                Assert.IsFalse(Sim.JustReopened, $"session {n}");
                Sim.SkipToClose();
                Sim.EndSession();
                Sim.OpenNextSession(false);
            }

            Assert.IsTrue(Sim.JustReopened);
            Assert.AreEqual(2, Sim.Clock.Cycle);
            Assert.AreEqual(1, MarketClock.DayInSeason(Sim.Clock.Day));

            var outcomesAtOpen = CountOutcomes();
            Sim.SkipToClose();
            Assert.Greater(CountOutcomes(), outcomesAtOpen, "정보 타임에 원정 결과가 나와야 한다.");
        }

        [Test]
        public void SeasonPlanEffectsLandAtReopening()
        {
            SeasonPlan.Apply(new[] { SeasonTask.Task_Guild, SeasonTask.Task_Omens, SeasonTask.Task_Omens }, null);
            Assert.AreEqual(2, SeasonPlan.OmenClues);

            Sim.OpenSession(false, MarketSession.Session_Evening);

            for (var n = 0; n < 7; n++)
            {
                Sim.SkipToClose();
                Sim.EndSession();
                Sim.OpenNextSession(false);
            }

            var notes = SeasonPlan.ApplyAtReopening(Sim);
            Assert.AreEqual(1, notes.Count, "길드 심부름 하나 → 새 단 하나의 내력");
            Assert.AreEqual(0, SeasonPlan.ApplyAtReopening(Sim).Count, "두 번 풀리면 안 된다");
        }

        private int CountOutcomes()
        {
            var count = 0;

            for (var i = 0; i < Sim.InfoLog.Count; i++)
            {
                if (Sim.InfoLog[i].Stage == EventStage.Stage_Outcome)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
