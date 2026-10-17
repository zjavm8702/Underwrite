using System;
using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;

namespace Underwrite.Story
{
    /// <summary>할 일 하나. 끝났는지는 게임 상태를 보고 판단한다.</summary>
    public sealed class Quest
    {
        public string Title;
        public string Hint;
        public float Reward;
        public Func<bool> IsDone;
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
        }

        /// <summary>지금 몇 번째 할 일까지 왔는가. 저장에 쓴다.</summary>
        public static int CurrentIndex
        {
            get
            {
                return Current;
            }
        }

        public static void Restore(int index)
        {
            Quests.Clear();
            Current = index < 0 ? 0 : index;
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

            Add(StringConstants.QUEST_TITLES[0], StringConstants.QUEST_HINTS[0], 0f,
                () => StoryProgress.CellarFound);
            Add(StringConstants.QUEST_TITLES[1], StringConstants.QUEST_HINTS[1], 0f,
                () => StoryProgress.MarketTutorialDone);
            Add(StringConstants.QUEST_TITLES[2], StringConstants.QUEST_HINTS[2], 20f, HoldsAnything);
            Add(StringConstants.QUEST_TITLES[3], StringConstants.QUEST_HINTS[3], 0f, TalkedToHero);
            Add(StringConstants.QUEST_TITLES[4], StringConstants.QUEST_HINTS[4], 0f, HeardNeed);
            Add(StringConstants.QUEST_TITLES[5], StringConstants.QUEST_HINTS[5], 0f,
                () => PatronageLedger.RequestsPosted > 0);
            Add(StringConstants.QUEST_TITLES[6], StringConstants.QUEST_HINTS[6], 30f, ScoutedBand);
            Add(StringConstants.QUEST_TITLES[7], StringConstants.QUEST_HINTS[7], 0f, () => HeroRoster.DayOffDone);
            Add(StringConstants.QUEST_TITLES[8], StringConstants.QUEST_HINTS[8], 0f, () => HeroRoster.ReturnDone);
        }

        private static void Add(string title, string hint, float reward, Func<bool> isDone)
        {
            Quests.Add(new Quest { Title = title, Hint = hint, Reward = reward, IsDone = isDone });
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
