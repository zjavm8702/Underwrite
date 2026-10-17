using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Market
{
    /// <summary>
    /// 모험단 도감. 플레이어가 조사한 만큼만 채워진다(2026-10-17 사용자 요청).
    ///
    /// 변하지 않는 것(말 걸기·길드)은 한 번 알면 남고, 변하는 것(대장간·여관)은 주기가 바뀌면 지워진다.
    /// 그래서 주기마다 다시 캐야 할 이유가 생긴다. 갓 도착한 단을 먼저 알려 준다 — 상장 전 조사(상장흐름 설계안 §3).
    /// 씬을 넘나들므로 정적이다. 저장이 들어오면 함께 저장한다.
    /// </summary>
    public static class BandCodex
    {
        // 종목마다 줄마다 알아낸 주기. 0이면 모른다.
        private static readonly Dictionary<string, int[]> LearnedCycle = new Dictionary<string, int[]>();

        public static void Reset()
        {
            LearnedCycle.Clear();
        }

        private static int CurrentCycle
        {
            get
            {
                return BreakClock.Current == null ? 1 : BreakClock.Current.Cycle;
            }
        }

        /// <summary>주기가 바뀌면 지워지는 줄인가.</summary>
        public static bool IsPerCycle(CodexSource source)
        {
            return source == CodexSource.Codex_Smith || source == CodexSource.Codex_Tavern;
        }

        /// <summary>그 출처로 알 수 있는 것을 안다. 새로 알았으면 그 문장, 이미 알았거나 없으면 null.</summary>
        public static string Learn(string stockId, CodexSource source)
        {
            if (IndexOf(stockId) < 0 || IsKnown(stockId, source) == true)
            {
                return null;
            }

            CyclesOf(stockId)[(int)source] = CurrentCycle;
            return FactOf(stockId, source);
        }

        public static bool IsKnown(string stockId, CodexSource source)
        {
            if (IndexOf(stockId) < 0)
            {
                return false;
            }

            var learned = CyclesOf(stockId)[(int)source];

            if (learned <= 0)
            {
                return false;
            }

            return IsPerCycle(source) == false || learned == CurrentCycle;
        }

        public static int KnownCount(string stockId)
        {
            var count = 0;

            for (var i = 0; i < StringConstants.CODEX_HINTS.Length; i++)
            {
                if (IsKnown(stockId, (CodexSource)i) == true)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>그 출처의 문장. 아직 모르면 어디서 알 수 있는지 알려 주는 말.</summary>
        public static string LineOf(string stockId, CodexSource source)
        {
            if (IndexOf(stockId) < 0)
            {
                return string.Empty;
            }

            return IsKnown(stockId, source) == true ? FactOf(stockId, source) : StringConstants.CODEX_HINTS[(int)source];
        }

        /// <summary>
        /// 그 출처에서 아직 모르는 것이 있는 단 하나를 고른다. 갓 도착한 단을 먼저 고른다.
        /// 영지에 없는 단은 고르지 않는다. 없으면 null.
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

        // 이번 주기의 문장. 주기마다 다른 줄을 돌려 쓴다. 여관의 말에는 소문 표시를 붙인다 — 틀릴 수 있다.
        private static string FactOf(string stockId, CodexSource source)
        {
            var index = IndexOf(stockId);
            var turn = (CurrentCycle - 1) % 3;

            switch (source)
            {
                case CodexSource.Codex_Talk:
                {
                    return StringConstants.CODEX_FIXED[index][0];
                }
                case CodexSource.Codex_Guild:
                {
                    return StringConstants.CODEX_FIXED[index][1];
                }
                case CodexSource.Codex_Smith:
                {
                    var lines = StringConstants.CODEX_SMITH[index];
                    return lines[turn % lines.Length];
                }
                default:
                {
                    var lines = StringConstants.CODEX_TAVERN[index];
                    return StringConstants.TEXT_CODEX_RUMOR + lines[turn % lines.Length];
                }
            }
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

        private static int[] CyclesOf(string stockId)
        {
            int[] cycles;

            if (LearnedCycle.TryGetValue(stockId, out cycles) == false)
            {
                cycles = new int[StringConstants.CODEX_HINTS.Length];
                LearnedCycle.Add(stockId, cycles);
            }

            return cycles;
        }
    }
}
