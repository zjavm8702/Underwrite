using System.Collections.Generic;
using NUnit.Framework;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Village;
using UnityEditor;

namespace Underwrite.Editor.Tests
{
    /// <summary>영지 손놀림·정보 모으기의 판정(2026-10-18 — 장부 맞추기, 엿듣기, 주문 나르기).</summary>
    public sealed class VillageMiniGameTests
    {
        private MarketSimulation Sim;

        [SetUp]
        public void SetUp()
        {
            var guids = AssetDatabase.FindAssets("t:GameDataSet");
            Assert.IsNotEmpty(guids, "GameDataSet 에셋이 없습니다.");
            var data = AssetDatabase.LoadAssetAtPath<GameDataSet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            Sim = MarketFactory.Create(data, 11, MarketSentiment.Sentiment_Calm);
            BreakClock.Simulation = Sim;
            BreakClock.Current = Sim.Clock;
            BandCodex.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            BreakClock.Simulation = null;
            BreakClock.Current = null;
            BandCodex.Reset();
        }

        [Test]
        public void LedgerHasExactlyOneMismatchedBand()
        {
            for (var seed = 0; seed < 150; seed++)
            {
                var puzzle = LedgerPuzzle.Create(Sim, new System.Random(seed), 3);
                Assert.IsNotNull(puzzle, $"seed {seed}");

                var mismatched = 0;

                for (var i = 0; i < puzzle.Requests.Count; i++)
                {
                    var request = puzzle.Requests[i];
                    var paid = 0;

                    for (var p = 0; p < puzzle.Payments.Count; p++)
                    {
                        if (puzzle.Payments[p].BandName == request.BandName)
                        {
                            Assert.Greater(puzzle.Payments[p].Amount, 0, $"seed {seed}");
                            paid += puzzle.Payments[p].Amount;
                        }
                    }

                    if (paid != request.Reward)
                    {
                        mismatched++;
                        Assert.AreEqual(puzzle.TargetId, request.BandId, $"seed {seed}");
                        Assert.AreEqual(puzzle.TargetGains, paid > request.Reward, $"seed {seed}");
                    }
                }

                Assert.AreEqual(1, mismatched, $"seed {seed}");
            }
        }

        [Test]
        public void LedgerKicksOutAfterThreeWrongPicks()
        {
            var puzzle = LedgerPuzzle.Create(Sim, new System.Random(1), 3);
            var wrong = new List<int>();

            for (var i = 0; i < puzzle.Requests.Count; i++)
            {
                if (puzzle.Requests[i].BandId != puzzle.TargetId)
                {
                    wrong.Add(i);
                }
            }

            for (var n = 0; n < Constants.LEDGER_MAX_SUSPICION; n++)
            {
                Assert.IsFalse(puzzle.Check(wrong[n % wrong.Count]));
            }

            Assert.IsTrue(puzzle.IsKickedOut);
        }

        [Test]
        public void EavesdropCatchesWhenStayingPastWariness()
        {
            var session = EavesdropSession.Create(Sim, false, new System.Random(4));
            Assert.IsNotNull(session);

            EavesdropSession.Table wary = null;

            for (var i = 0; i < session.Tables.Count; i++)
            {
                if (session.Tables[i].Wariness == 1)
                {
                    wary = session.Tables[i];
                }
            }

            Assert.IsNotNull(wary, "경계심 1인 테이블이 있어야 한다(경계심은 1·2·3을 섞어 나눈다).");
            Assert.IsTrue(session.Listen(wary));
            Assert.IsFalse(wary.IsDone);
            Assert.IsFalse(session.Listen(wary));
            Assert.IsTrue(session.IsCaught);
            Assert.IsTrue(session.IsOver);
        }

        [Test]
        public void EavesdropFinishesALineAfterTwoStays()
        {
            var session = EavesdropSession.Create(Sim, false, new System.Random(4));
            EavesdropSession.Table calm = null;

            for (var i = 0; i < session.Tables.Count; i++)
            {
                if (session.Tables[i].Wariness >= Constants.EAVES_LISTENS_TO_FINISH)
                {
                    calm = session.Tables[i];
                }
            }

            Assert.IsNotNull(calm);
            Assert.IsTrue(session.Listen(calm));
            Assert.IsTrue(session.Listen(calm));
            Assert.IsTrue(calm.IsDone);
            Assert.IsFalse(session.IsCaught);
        }

        [Test]
        public void WorkOrderScoresOnlyExactTrays()
        {
            var task = WorkOrderTask.Create(StringConstants.WORK_TAVERN_ITEMS, StringConstants.WORK_TAVERN_PLACES,
                3, new System.Random(7));
            Assert.AreEqual(3, task.Orders.Count);

            Assert.IsTrue(task.Serve((int[])task.Current.Counts.Clone()));

            var wrong = (int[])task.Current.Counts.Clone();
            wrong[0]++;
            Assert.IsFalse(task.Serve(wrong));

            Assert.IsTrue(task.Serve((int[])task.Current.Counts.Clone()));
            Assert.IsTrue(task.IsDone);
            Assert.AreEqual(2f / 3f, task.Accuracy, 0.0001f);
        }
    }
}
