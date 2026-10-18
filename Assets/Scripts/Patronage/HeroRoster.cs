using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;
using Underwrite.Save;

namespace Underwrite.Patronage
{
    /// <summary>
    /// 용사단과 그들의 한 주기 일정. 루틴 설계안의 세 겹 리듬 가운데 위의 두 겹을 맡는다.
    ///
    /// - 일주일: 영주 시찰일에는 지하도 용사단도 쉰다. 그날이 큰 관계 장면의 날이다
    /// - 한 주기: 원정을 떠났다가 돌아온다. 돌아온 날이 재회 장면의 날이다
    ///
    /// 한 계절(4일): 1일째 귀환(둘째 계절부터), 3일째 시찰, 4일째 밤 출발 → 휴장 동안 원정 → 다음 계절 1일째 귀환.
    /// 씬을 넘나들어야 하므로 정적이다. 저장 기능이 들어오면 함께 저장한다.
    /// </summary>
    public static class HeroRoster
    {
        private static readonly List<HeroProfile> Heroes = new List<HeroProfile>();

        // 하루에 한 번만 벌어지는 큰 장면을 이미 봤는가.
        private static readonly HashSet<int> DayOffSeen = new HashSet<int>();
        private static readonly HashSet<int> ReturnSeen = new HashSet<int>();

        public static IReadOnlyList<HeroProfile> All
        {
            get
            {
                Ensure();
                return Heroes;
            }
        }

        public static void Reset()
        {
            Heroes.Clear();
            DayOffSeen.Clear();
            ReturnSeen.Clear();
        }

        internal static HeroesState Capture()
        {
            Ensure();
            var state = new HeroesState();

            for (var i = 0; i < Heroes.Count; i++)
            {
                var hero = new HeroState
                {
                    Id = Heroes[i].Id,
                    Affinity = Heroes[i].Affinity,
                    LastTalkDay = Heroes[i].LastTalkDay,
                    Condition = Heroes[i].Condition,
                    ConditionDay = Heroes[i].ConditionDay
                };
                hero.TopicsSeen.AddRange(Heroes[i].TopicsSeen);
                state.People.Add(hero);
            }

            state.DayOffSeen.AddRange(DayOffSeen);
            state.ReturnSeen.AddRange(ReturnSeen);
            return state;
        }

        internal static void Restore(HeroesState state)
        {
            Reset();
            Ensure();

            for (var i = 0; i < state.People.Count; i++)
            {
                var row = state.People[i];

                for (var h = 0; h < Heroes.Count; h++)
                {
                    if (Heroes[h].Id != row.Id)
                    {
                        continue;
                    }

                    Heroes[h].Affinity = row.Affinity;
                    Heroes[h].LastTalkDay = row.LastTalkDay;
                    Heroes[h].Condition = row.Condition;
                    Heroes[h].ConditionDay = row.ConditionDay;
                    Heroes[h].TopicsSeen.UnionWith(row.TopicsSeen);
                }
            }

            DayOffSeen.UnionWith(state.DayOffSeen);
            ReturnSeen.UnionWith(state.ReturnSeen);
        }

        /// <summary>시찰일 장면을 한 번이라도 봤는가.</summary>
        public static bool DayOffDone
        {
            get
            {
                return DayOffSeen.Count > 0;
            }
        }

        /// <summary>귀환 장면을 한 번이라도 봤는가.</summary>
        public static bool ReturnDone
        {
            get
            {
                return ReturnSeen.Count > 0;
            }
        }

        /// <summary>
        /// 원정에서 돌아오는 날인가. 원정은 계절 사이의 휴장 동안 다녀오므로, 둘째 계절부터 계절 첫날이 귀환일이다.
        /// 플레이하는 날에 용사단이 영지를 비우는 날은 없다.
        /// </summary>
        public static bool IsReturnDay(int day)
        {
            return Market.MarketClock.DayInSeason(day) == 1 && Market.MarketClock.SeasonOf(day) > 1;
        }

        /// <summary>오늘 쉬는 날 장면을 아직 보지 않았는가.</summary>
        public static bool HasDayOff(int day)
        {
            return Market.MarketClock.IsHeroDayOff(day) == true && DayOffSeen.Contains(day) == false
                && Favorite() != null;
        }

        /// <summary>오늘 귀환 장면을 아직 보지 않았는가.</summary>
        public static bool HasReturn(int day)
        {
            return IsReturnDay(day) == true && ReturnSeen.Contains(day) == false;
        }

        public static void MarkDayOff(int day)
        {
            DayOffSeen.Add(day);
        }

        public static void MarkReturn(int day)
        {
            ReturnSeen.Add(day);
        }

        /// <summary>호감이 가장 높은 사람. 같으면 앞사람. 시찰일 장면의 마지막 한마디를 이 사람이 한다.</summary>
        /// <summary>가장 친한 살아 있는 용사. 모두 떠났으면 null.</summary>
        public static HeroProfile Favorite()
        {
            Ensure();
            HeroProfile best = null;

            for (var i = 0; i < Heroes.Count; i++)
            {
                if (Heroes[i].Condition == HeroCondition.Condition_Dead)
                {
                    continue;
                }

                if (best == null || Heroes[i].Affinity > best.Affinity)
                {
                    best = Heroes[i];
                }
            }

            return best;
        }

        /// <summary>
        /// 놓친 사정의 대가. 성한 사람은 다치고, 이미 다친 사람이나 목숨이 걸린 사정이면 죽는다.
        /// 바뀐 몸 상태를 돌려준다.
        /// </summary>
        public static HeroCondition Strike(int index, bool fatal, int day)
        {
            Ensure();

            if (index < 0 || index >= Heroes.Count)
            {
                return HeroCondition.Condition_Healthy;
            }

            var hero = Heroes[index];

            if (hero.Condition == HeroCondition.Condition_Dead)
            {
                return hero.Condition;
            }

            hero.Condition = fatal == true || hero.Condition == HeroCondition.Condition_Wounded
                ? HeroCondition.Condition_Dead
                : HeroCondition.Condition_Wounded;
            hero.ConditionDay = day;
            return hero.Condition;
        }

        public static void AddAffinityToAll(int amount)
        {
            Ensure();

            for (var i = 0; i < Heroes.Count; i++)
            {
                Heroes[i].Affinity += amount;
            }
        }

        private static void Ensure()
        {
            if (Heroes.Count > 0)
            {
                return;
            }

            for (var i = 0; i < StringConstants.HERO_NAMES.Length; i++)
            {
                Heroes.Add(new HeroProfile
                {
                    Id = StringConstants.HERO_FACE_ASSETS[i],
                    Name = StringConstants.HERO_NAMES[i],
                    Role = StringConstants.HERO_ROLES[i],
                    FaceIndex = i,
                    Affinity = 0,
                    LastTalkDay = 0
                });
            }

            UnityEngine.Debug.Assert(Heroes.Count == StringConstants.HERO_GREET.Length, "Hero script arrays are out of sync.");
        }
    }
}
