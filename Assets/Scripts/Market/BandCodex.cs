using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Market
{
    /// <summary>
    /// 모험단 도감. 플레이어가 조사한 만큼만 채워진다(2026-10-17 사용자 요청).
    ///
    /// 상장 전 조사가 남보다 앞서는 길이다(상장흐름 설계안 §3). 갓 도착한 단을 먼저 캐면
    /// 상장되기 전에 신입인지 베테랑인지, 장비와 빚 사정을 안다.
    /// 씬을 넘나들므로 정적이다. 저장이 들어오면 함께 저장한다.
    /// </summary>
    public static class BandCodex
    {
        private static readonly Dictionary<string, bool[]> Known = new Dictionary<string, bool[]>();

        public static void Reset()
        {
            Known.Clear();
        }

        /// <summary>도감에 실린 종목인가.</summary>
        public static bool Has(string stockId)
        {
            return IndexOf(stockId) >= 0;
        }

        /// <summary>그 출처로 알 수 있는 것을 안다. 새로 알았으면 그 문장, 이미 알았거나 없으면 null.</summary>
        public static string Learn(string stockId, CodexSource source)
        {
            var index = IndexOf(stockId);

            if (index < 0)
            {
                return null;
            }

            var flags = FlagsOf(stockId);
            var slot = (int)source;

            if (flags[slot] == true)
            {
                return null;
            }

            flags[slot] = true;
            return StringConstants.CODEX_FACTS[index][slot];
        }

        public static bool IsKnown(string stockId, CodexSource source)
        {
            return IndexOf(stockId) >= 0 && FlagsOf(stockId)[(int)source] == true;
        }

        public static int KnownCount(string stockId)
        {
            if (IndexOf(stockId) < 0)
            {
                return 0;
            }

            var count = 0;
            var flags = FlagsOf(stockId);

            for (var i = 0; i < flags.Length; i++)
            {
                if (flags[i] == true)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>그 출처의 문장. 아직 모르면 어디서 알 수 있는지 알려 주는 말.</summary>
        public static string LineOf(string stockId, CodexSource source)
        {
            var index = IndexOf(stockId);

            if (index < 0)
            {
                return string.Empty;
            }

            return IsKnown(stockId, source) == true
                ? StringConstants.CODEX_FACTS[index][(int)source]
                : StringConstants.CODEX_HINTS[(int)source];
        }

        /// <summary>
        /// 그 출처에서 아직 모르는 것이 있는 단 하나를 고른다. 갓 도착한 단을 먼저 고른다 —
        /// 상장 전 조사가 값지기 때문이다. 영지에 없는 단은 고르지 않는다. 없으면 null.
        /// </summary>
        public static string PickUnknown(MarketSimulation sim, CodexSource source)
        {
            if (sim == null)
            {
                return null;
            }

            string listed = null;

            for (var i = 0; i < StringConstants.CODEX_STOCK_IDS.Length; i++)
            {
                var id = StringConstants.CODEX_STOCK_IDS[i];

                if (IsKnown(id, source) == true)
                {
                    continue;
                }

                var stage = sim.StageOf(id);

                if (stage == BandStage.Band_Arrived)
                {
                    return id;
                }

                if (stage == BandStage.Band_Listed && listed == null)
                {
                    listed = id;
                }
            }

            return listed;
        }

        private static int IndexOf(string stockId)
        {
            for (var i = 0; i < StringConstants.CODEX_STOCK_IDS.Length; i++)
            {
                if (StringConstants.CODEX_STOCK_IDS[i] == stockId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool[] FlagsOf(string stockId)
        {
            bool[] flags;

            if (Known.TryGetValue(stockId, out flags) == false)
            {
                flags = new bool[StringConstants.CODEX_HINTS.Length];
                Known.Add(stockId, flags);
            }

            return flags;
        }
    }
}
