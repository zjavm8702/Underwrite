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

        [Test]
        public void GuildDeductionHasOneAnswerAndMinimalClues()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var puzzle = GuildDeduction.Create(Sim, new System.Random(seed));
                Assert.IsNotNull(puzzle, $"seed {seed}");
                Assert.AreEqual(1, puzzle.SolutionCount(), $"seed {seed} — 답이 하나여야 한다");
                Assert.GreaterOrEqual(puzzle.Clues.Count, 2, $"seed {seed}");
                Assert.LessOrEqual(puzzle.Clues.Count, 9, $"seed {seed}");

                var target = puzzle.BandIds.IndexOf(puzzle.TargetId);
                Assert.AreEqual(0, puzzle.DestinationOf[target], $"seed {seed} — 보상 단이 먼 원정");

                var wrong = (int[])puzzle.DestinationOf.Clone();
                var swap = wrong[0];
                wrong[0] = wrong[1];
                wrong[1] = swap;
                Assert.IsFalse(puzzle.Check(wrong, puzzle.ClientOf));
                Assert.AreEqual(Constants.DEDUCE_TRIES - 1, puzzle.TriesLeft);
                Assert.IsTrue(puzzle.Check(puzzle.DestinationOf, puzzle.ClientOf));
            }
        }

        [Test]
        public void DiceChanceMatchesBinomial()
        {
            Assert.AreEqual(1f, LiarsDiceGame.ChanceAtLeast(0, 4));
            Assert.AreEqual(0f, LiarsDiceGame.ChanceAtLeast(5, 4));
            Assert.AreEqual(1f - System.Math.Pow(5.0 / 6.0, 4), LiarsDiceGame.ChanceAtLeast(1, 4), 0.0001f);
            Assert.AreEqual(System.Math.Pow(1.0 / 6.0, 4), LiarsDiceGame.ChanceAtLeast(4, 4), 0.0001f);
        }

        [Test]
        public void DiceMatchesAlwaysEndWithFairReveals()
        {
            for (var seed = 0; seed < 2000; seed++)
            {
                var rng = new System.Random(seed);
                var game = new LiarsDiceGame(new System.Random(seed * 7 + 1), (float)rng.NextDouble(), Constants.DICE_PER_PLAYER);
                var guard = 0;

                while (game.IsOver == false)
                {
                    Assert.Less(++guard, 500, $"seed {seed} — 끝나지 않는다");

                    if (game.IsRevealed == true)
                    {
                        var holds = game.CountAll(game.BidFace) >= game.BidCount;
                        Assert.AreEqual(holds == game.BidByPlayer, game.RevealPlayerWon, $"seed {seed} — 판정");
                        game.StartRound();
                        continue;
                    }

                    if (game.IsPlayerTurn == false)
                    {
                        var count = game.BidCount;
                        var face = game.BidFace;

                        if (game.OpponentAct() == true)
                        {
                            Assert.IsTrue(game.BidCount > count || (game.BidCount == count && game.BidFace > face),
                                $"seed {seed} — 상대가 낮게 불렀다");
                            Assert.LessOrEqual(game.BidCount, game.TotalDice, $"seed {seed}");
                        }

                        continue;
                    }

                    // 플레이어 대신: 반쯤은 의심하고, 아니면 한 단계 올린다.
                    if (game.BidCount > 0 && rng.Next(2) == 0)
                    {
                        Assert.IsTrue(game.PlayerCall());
                        continue;
                    }

                    var nextCount = game.BidCount == 0 ? 1 : game.BidCount;
                    var nextFace = game.BidCount == 0 ? 1 : game.BidFace + 1;

                    if (nextFace > Constants.DICE_FACES)
                    {
                        nextFace = 1;
                        nextCount++;
                    }

                    if (game.PlayerBid(nextCount, nextFace) == false)
                    {
                        Assert.IsTrue(game.PlayerCall(), $"seed {seed} — 올릴 수도 의심할 수도 없다");
                    }
                }

                Assert.AreEqual(Constants.DICE_ROUNDS_TO_WIN, System.Math.Max(game.PlayerWins, game.OpponentWins));
            }
        }

        [Test]
        public void SmithOrdersHaveExactlyOneEvidence()
        {
            var cancelled = StringConstants.TEXT_SMITH_STATUS.Length - 1;
            var turn = BandCodex.TurnOf(Sim.Clock.Cycle);

            for (var seed = 0; seed < 150; seed++)
            {
                // 둘째 계절 이틀째 — 지난 계절 미끼도 들어갈 수 있는 날.
                var puzzle = SmithOrderPuzzle.Create(Sim, new System.Random(seed), Constants.SEASON_DAYS + 2);
                Assert.IsNotNull(puzzle, $"seed {seed}");
                Assert.AreEqual(Constants.SMITH_ORDER_SLIPS, puzzle.Slips.Count, $"seed {seed}");

                var evidence = -1;

                for (var i = 0; i < puzzle.Slips.Count; i++)
                {
                    var slip = puzzle.Slips[i];

                    if (i > 0)
                    {
                        Assert.LessOrEqual(puzzle.Slips[i - 1].Day, slip.Day, $"seed {seed} — 날짜순이어야 한다");
                    }

                    if (slip.BandId == puzzle.TargetId && slip.Day > 0 && slip.Status != cancelled)
                    {
                        Assert.AreEqual(-1, evidence, $"seed {seed} — 증거가 둘이다");
                        evidence = i;
                    }
                }

                Assert.AreEqual(puzzle.EvidenceIndex, evidence, $"seed {seed}");

                var profile = Sim.CodexProfileOf(puzzle.TargetId);
                var expected = StringConstants.SMITH_EVIDENCE[profile][turn][puzzle.RumorTrue == true ? 0 : 1];
                Assert.AreEqual(expected, puzzle.Evidence.Item, $"seed {seed}");
                Assert.AreEqual(Sim.IsTavernTrue(puzzle.TargetId, Sim.Clock.Cycle), puzzle.RumorTrue, $"seed {seed}");
            }
        }

        [Test]
        public void SmithOrdersJudgePicksAndKickOut()
        {
            var puzzle = SmithOrderPuzzle.Create(Sim, new System.Random(3), Constants.SEASON_DAYS + 2);
            var wrong = new List<int>();

            for (var i = 0; i < puzzle.Slips.Count; i++)
            {
                if (i != puzzle.EvidenceIndex)
                {
                    wrong.Add(i);
                }
            }

            for (var i = 0; i < Constants.SMITH_ORDER_MAX_SUSPICION; i++)
            {
                var slip = puzzle.Slips[wrong[i]];
                var pick = puzzle.Check(wrong[i]);

                if (slip.BandId != puzzle.TargetId)
                {
                    Assert.AreEqual(SlipPick.Pick_OtherBand, pick);
                }
                else if (slip.Day == 0)
                {
                    Assert.AreEqual(SlipPick.Pick_LastSeason, pick);
                }
                else
                {
                    Assert.AreEqual(SlipPick.Pick_Cancelled, pick);
                }
            }

            Assert.IsTrue(puzzle.IsKickedOut);
            Assert.AreEqual(SlipPick.Pick_Evidence, puzzle.Check(puzzle.EvidenceIndex));
        }

        [Test]
        public void TavernTruthIsFixedAndFollowsTable()
        {
            var id = Sim.CodexSubjects()[0];
            var profile = Sim.CodexProfileOf(id);
            var hits = 0;
            var expected = 0f;
            const int cycles = 600;

            for (var cycle = 1; cycle <= cycles; cycle++)
            {
                var truth = Sim.IsTavernTrue(id, cycle);
                Assert.AreEqual(truth, Sim.IsTavernTrue(id, cycle), $"cycle {cycle} — 다시 물어도 같아야 한다");
                hits += truth == true ? 1 : 0;
                expected += Constants.CODEX_TAVERN_TRUTH[profile][BandCodex.TurnOf(cycle)];
            }

            Assert.AreEqual(expected / cycles, (float)hits / cycles, 0.08f);
        }

        [Test]
        public void VerifyMarksTavernLine()
        {
            var id = Sim.CodexSubjects()[0];
            Assert.IsFalse(BandCodex.IsVerified(id));

            BandCodex.Verify(id, false);

            Assert.IsTrue(BandCodex.IsVerified(id));
            Assert.IsTrue(BandCodex.IsKnown(id, CodexSource.Codex_Tavern));
            StringAssert.StartsWith(StringConstants.TEXT_CODEX_RUMOR_CHECKED_FALSE, BandCodex.LineOf(id, CodexSource.Codex_Tavern));
            // 확인한 소문은 다시 고르지 않는다.
            for (var seed = 0; seed < 50; seed++)
            {
                var puzzle = SmithOrderPuzzle.Create(Sim, new System.Random(seed), 2);

                if (puzzle != null)
                {
                    Assert.AreNotEqual(id, puzzle.TargetId, $"seed {seed}");
                }
            }
        }
    }
}
