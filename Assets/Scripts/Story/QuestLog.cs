using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;

namespace Underwrite.Story
{
    /// <summary>할 일 하나. 끝났는지는 게임 상태를 보고 판단한다.</summary>
    public sealed class Quest
    {
        public string Title;

        /// <summary>맡기는 사람과 그 말. 어디로 가서 무엇을 누르라는 안내 대신 이 말이 길잡이다.</summary>
        public QuestGiver Giver;
        public string Ask;

        public float Reward;
        public Func<bool> IsDone;

        /// <summary>진행도. 목표가 0이면 진행도를 보이지 않는다(한 번에 끝나는 일).</summary>
        public Func<int> Progress;
        public int Goal;
    }

    /// <summary>
    /// 이야기의 할 일 목록(2026-10-17 사용자: 영지에서 퀘스트를 쭉 받으면서 진행할 것이다).
    ///
    /// 하나를 마치면 다음 것이 열린다. 할 일은 상태를 **보고** 끝났는지 안다 —
    /// 각 기능 쪽에서 "할 일을 마쳤다"고 알려 줄 필요가 없어, 기능과 이야기가 서로 묶이지 않는다.
    /// 영지와 거래소가 매 프레임 <see cref="Tick"/>을 부른다.
    /// </summary>
    public static class QuestLog
    {
        private static readonly List<Quest> Quests = new List<Quest>();
        private static int Current;

        /// <summary>
        /// 맡는 장면을 본 할 일의 수. <see cref="Current"/>보다 작으면 지금 할 일을 아직 받지 않은 것이다 —
        /// 영지가 한가할 때 받는 장면을 띄우고 이 값을 올린다.
        /// </summary>
        public static int Introduced { get; private set; }

        /// <summary>지금 할 일을 아직 받지 않았는가.</summary>
        public static bool HasPendingIntro
        {
            get
            {
                return Active != null && Introduced <= Current;
            }
        }

        /// <summary>지금 할 일을 받았다.</summary>
        public static void MarkIntroduced()
        {
            Introduced = Current + 1;
        }

        public static IReadOnlyList<Quest> All
        {
            get
            {
                Ensure();
                return Quests;
            }
        }

        /// <summary>지금 해야 할 일. 다 마쳤으면 null.</summary>
        public static Quest Active
        {
            get
            {
                Ensure();
                return Current < Quests.Count ? Quests[Current] : null;
            }
        }

        /// <summary>그 할 일을 마쳤는가.</summary>
        public static bool IsFinished(int index)
        {
            return index < Current;
        }

        public static void Reset()
        {
            Quests.Clear();
            Current = 0;
            Introduced = 0;
        }

        /// <summary>지금 몇 번째 할 일까지 왔는가. 저장에 쓴다.</summary>
        public static int CurrentIndex
        {
            get
            {
                return Current;
            }
        }

        public static void Restore(int index, int introduced)
        {
            Quests.Clear();
            Current = index < 0 ? 0 : index;
            Introduced = introduced;
        }

        /// <summary>
        /// 지금 할 일이 끝났는지 본다. 끝났으면 다음으로 넘어가고 마친 것을 돌려준다. 아니면 null.
        /// 한 번에 하나만 넘긴다 — 여러 개가 한꺼번에 끝나도 하나씩 알린다.
        /// </summary>
        public static Quest Tick()
        {
            var active = Active;

            if (active == null || active.IsDone() == false)
            {
                return null;
            }

            Current++;
            return active;
        }

        private static void Ensure()
        {
            if (Quests.Count > 0)
            {
                return;
            }

            Add(0, QuestGiver.Giver_Innkeeper, 0f, () => StoryProgress.CellarFound);
            Add(1, QuestGiver.Giver_Self, 0f, () => StoryProgress.MarketTutorialDone);
            Add(2, QuestGiver.Giver_Self, 20f, HoldsAnything);
            Add(3, QuestGiver.Giver_Innkeeper, 0f, TalkedToHero);
            Add(4, QuestGiver.Giver_Innkeeper, 0f, HeardNeed);
            Add(5, QuestGiver.Giver_Archivist, 0f, () => PatronageLedger.RequestsPosted > 0);
            Add(6, QuestGiver.Giver_Broker, 30f, ScoutedBand);
            Quests[6].Progress = BandCodex.MaxKnownCount;
            Quests[6].Goal = 2;
            Add(7, QuestGiver.Giver_Swordswoman, 0f, () => HeroRoster.DayOffDone);
            Add(8, QuestGiver.Giver_Scholar, 0f, () => HeroRoster.ReturnDone);
            // 둘째 계절 — 데모의 절정(방패 사정)과 이야기의 줄기(국경의 징조)로 이끈다(2026-10-18).
            Add(9, QuestGiver.Giver_Swordswoman, 0f, () => PatronageLedger.IsMet(StringConstants.NEED_SHIELD_ID));
            Add(10, QuestGiver.Giver_Elf, 0f, () => SeasonPlan.OmenClues > 0);
        }

        private static void Add(int index, QuestGiver giver, float reward, Func<bool> isDone)
        {
            Quests.Add(new Quest
            {
                Title = StringConstants.QUEST_TITLES[index],
                Giver = giver,
                Ask = StringConstants.QUEST_ASKS[index],
                Reward = reward,
                IsDone = isDone
            });
        }

        private static bool HoldsAnything()
        {
            var sim = BreakClock.Simulation;

            if (sim == null)
            {
                return false;
            }

            for (var i = 0; i < sim.Stocks.Count; i++)
            {
                if (sim.Wallet.QuantityOf(sim.Stocks[i].Id) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TalkedToHero()
        {
            var heroes = HeroRoster.All;

            for (var i = 0; i < heroes.Count; i++)
            {
                if (heroes[i].LastTalkDay > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HeardNeed()
        {
            var needs = PatronageLedger.All;

            for (var i = 0; i < needs.Count; i++)
            {
                if (needs[i].Heard == true)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ScoutedBand()
        {
            return BandCodex.AnyKnownTwice();
        }
    }
}
