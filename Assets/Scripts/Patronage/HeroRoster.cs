using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Patronage
{
    /// <summary>
    /// 용사단과 그들의 한 주기 일정. 루틴 설계안의 세 겹 리듬 가운데 위의 두 겹을 맡는다.
    ///
    /// - 일주일: 영주 시찰일에는 지하도 용사단도 쉰다. 그날이 큰 관계 장면의 날이다
    /// - 한 주기: 원정을 떠났다가 돌아온다. 돌아온 날이 재회 장면의 날이다
    ///
    /// 시제품 한 주기(7일): 1~4일 영지, 4일 시찰, 5~6일 원정, 7일 귀환.
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

        /// <summary>그날 용사단이 원정을 떠나 영지에 없는가.</summary>
        public static bool IsAway(int day)
        {
            return day >= Constants.HERO_EXPEDITION_DAY
                && day < Constants.HERO_EXPEDITION_DAY + Constants.HERO_EXPEDITION_DAYS;
        }

        /// <summary>원정에서 돌아오는 날.</summary>
        public static int ReturnDay
        {
            get
            {
                return Constants.HERO_EXPEDITION_DAY + Constants.HERO_EXPEDITION_DAYS;
            }
        }

        /// <summary>오늘 시찰일 장면을 아직 보지 않았는가.</summary>
        public static bool HasDayOff(int day)
        {
            return Market.MarketClock.IsInspectionDay(day) == true && IsAway(day) == false
                && DayOffSeen.Contains(day) == false;
        }

        /// <summary>오늘 귀환 장면을 아직 보지 않았는가.</summary>
        public static bool HasReturn(int day)
        {
            return day == ReturnDay && ReturnSeen.Contains(day) == false;
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
        public static HeroProfile Favorite()
        {
            Ensure();
            var best = Heroes[0];

            for (var i = 1; i < Heroes.Count; i++)
            {
                if (Heroes[i].Affinity > best.Affinity)
                {
                    best = Heroes[i];
                }
            }

            return best;
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
