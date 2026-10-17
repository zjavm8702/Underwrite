using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Market;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 그날 영지를 걷는 용병단을 고른다(2026-10-17 사용자 결정).
    ///
    /// 영지에 와 있거나 상장된 단 가운데 무작위로 고르되, 한 주(<see cref="Constants.PARTY_WEEK_DAYS"/>일) 안에
    /// 한 번은 모든 단이 나오게 한다 — 오래 안 보인 단을 먼저 세운다. 용사단은 걷지 않는다.
    /// 씬을 넘나들므로 정적이다. 저장하지 않는다(다시 켜면 처음부터 고른다).
    /// </summary>
    public static class PartyCasting
    {
        private static readonly Dictionary<string, int> LastSeenDay = new Dictionary<string, int>();
        private static int CastDay = -1;

        public static void Reset()
        {
            LastSeenDay.Clear();
            CastDay = -1;
        }

        /// <summary>오늘의 무리를 세운다. 같은 날 다시 부르면 <paramref name="force"/>가 아닌 한 그대로 둔다.</summary>
        public static void Cast(int day, bool force)
        {
            var sim = BreakClock.Simulation;

            // 시장이 아직 없으면(지하를 찾기 전) 처음 짠 무리를 그대로 둔다.
            if (sim == null || (force == false && day == CastDay))
            {
                return;
            }

            CastDay = day;
            var parties = Object.FindObjectsByType<NpcParty>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            var order = Order(sim.CodexSubjects(), day);

            for (var i = 0; i < parties.Length; i++)
            {
                if (i >= order.Count)
                {
                    parties[i].Assign(null, string.Empty, Color.white);
                    continue;
                }

                var id = order[i];
                LastSeenDay[id] = day;
                parties[i].Assign(id, sim.NameOf(id), TintOf(id));
            }
        }

        // 한 주 동안 안 보인 단이 먼저, 나머지는 섞어서 뒤에.
        private static List<string> Order(List<string> bands, int day)
        {
            var overdue = new List<string>();
            var rest = new List<string>();

            for (var i = 0; i < bands.Count; i++)
            {
                int seen;
                var due = LastSeenDay.TryGetValue(bands[i], out seen) == false
                    || day - seen >= Constants.PARTY_WEEK_DAYS - 1;
                (due == true ? overdue : rest).Add(bands[i]);
            }

            Shuffle(overdue);
            Shuffle(rest);
            overdue.AddRange(rest);
            return overdue;
        }

        private static void Shuffle(List<string> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                var swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
        }

        // 단마다 옷 색을 정해 둔다. 같은 단은 언제 봐도 같은 색이다.
        private static Color TintOf(string stockId)
        {
            var hue = (stockId.GetHashCode() & 0x7fffffff) % 360 / 360f;
            return Color.HSVToRGB(hue, 0.3f, 1f);
        }
    }
}
