using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Save;

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

        // 여관 소문을 대장간 주문서로 확인한 주기와 그 답. 소문 줄과 같이 주기가 바뀌면 의미가 없어진다.
        private static readonly Dictionary<string, int> VerifiedCycle = new Dictionary<string, int>();
        private static readonly Dictionary<string, bool> VerifiedTrue = new Dictionary<string, bool>();

        public static void Reset()
        {
            LearnedCycle.Clear();
            VerifiedCycle.Clear();
            VerifiedTrue.Clear();
        }

        internal static List<CodexState> Capture()
        {
            var list = new List<CodexState>();

            foreach (var pair in LearnedCycle)
            {
                var row = new CodexState { StockId = pair.Key };
                row.LearnedCycle.AddRange(pair.Value);
                VerifiedCycle.TryGetValue(pair.Key, out row.VerifiedCycle);
                VerifiedTrue.TryGetValue(pair.Key, out row.VerifiedTrue);
                list.Add(row);
            }

            return list;
        }

        internal static void Restore(List<CodexState> rows)
        {
            LearnedCycle.Clear();
            VerifiedCycle.Clear();
            VerifiedTrue.Clear();

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].VerifiedCycle > 0)
                {
                    VerifiedCycle[rows[i].StockId] = rows[i].VerifiedCycle;
                    VerifiedTrue[rows[i].StockId] = rows[i].VerifiedTrue;
                }

                var cycles = CyclesOf(rows[i].StockId);

                for (var c = 0; c < cycles.Length && c < rows[i].LearnedCycle.Count; c++)
                {
                    cycles[c] = rows[i].LearnedCycle[c];
                }
            }
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

        /// <summary>이번 주기의 여관 소문이 맞는지 대장간 주문서로 확인했다. 소문 줄도 함께 안다.</summary>
        public static void Verify(string stockId, bool isTrue)
        {
            if (IndexOf(stockId) < 0)
            {
                return;
            }

            Learn(stockId, CodexSource.Codex_Tavern);
            VerifiedCycle[stockId] = CurrentCycle;
            VerifiedTrue[stockId] = isTrue;
        }

        /// <summary>이번 주기의 여관 소문을 이미 확인했는가.</summary>
        public static bool IsVerified(string stockId)
        {
            int cycle;
            return VerifiedCycle.TryGetValue(stockId, out cycle) == true && cycle == CurrentCycle;
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

        /// <summary>그 주기에 그 출처의 줄을 알아냈는가. 주기가 지난 뒤 결과를 낼 때 쓴다.</summary>
        public static bool LearnedIn(string stockId, CodexSource source, int cycle)
        {
            return IndexOf(stockId) >= 0 && CyclesOf(stockId)[(int)source] == cycle;
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

            var subjects = sim.CodexSubjects();

            for (var i = 0; i < subjects.Count; i++)
            {
                if (IsKnown(subjects[i], source) == false)
                {
                    return subjects[i];
                }
            }

            return null;
        }

        /// <summary>도감에 오르는 단. 시장이 아직 없으면 처음 다섯 단.</summary>
        public static List<string> Subjects()
        {
            var sim = BreakClock.Simulation;
            return sim == null ? new List<string>(StringConstants.CODEX_STOCK_IDS) : sim.CodexSubjects();
        }

        /// <summary>한 단에 대해 가장 많이 알아낸 줄 수(할 일 「새로 온 사람들」의 진행도).</summary>
        public static int MaxKnownCount()
        {
            var best = 0;

            foreach (var pair in LearnedCycle)
            {
                var count = KnownCount(pair.Key);

                if (count > best)
                {
                    best = count;
                }
            }

            return best;
        }

        /// <summary>두 가지 이상 알아낸 단이 하나라도 있는가(할 일 「새로 온 사람들」).</summary>
        public static bool AnyKnownTwice()
        {
            foreach (var pair in LearnedCycle)
            {
                if (KnownCount(pair.Key) >= 2)
                {
                    return true;
                }
            }

            return false;
        }

        // 이번 주기의 문장. 주기마다 다른 줄을 돌려 쓴다. 여관의 말에는 소문 표시를 붙인다 — 틀릴 수 있다.
        // 주기가 끝나면 시장이 같은 줄의 결과를 낸다(MarketSimulation.ResolveCodexCycle).
        private static string FactOf(string stockId, CodexSource source)
        {
            var index = IndexOf(stockId);
            var turn = TurnOf(CurrentCycle);

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
                    var mark = StringConstants.TEXT_CODEX_RUMOR;

                    if (IsVerified(stockId) == true)
                    {
                        mark = VerifiedTrue[stockId] == true
                            ? StringConstants.TEXT_CODEX_RUMOR_CHECKED_TRUE
                            : StringConstants.TEXT_CODEX_RUMOR_CHECKED_FALSE;
                    }

                    return mark + lines[turn % lines.Length];
                }
            }
        }

        /// <summary>그 주기에 쓰이는 주기 문장 순번. 시장이 주기 끝에 같은 줄의 결과를 낸다.</summary>
        public static int TurnOf(int cycle)
        {
            return (cycle - 1) % 3;
        }

        /// <summary>그 종목이 쓰는 도감 문장 묶음. 시장이 있으면 시장에 묻고, 없으면 처음 다섯 단의 순번. 없으면 -1.</summary>
        public static int IndexOf(string stockId)
        {
            var sim = BreakClock.Simulation;

            if (sim != null)
            {
                return sim.CodexProfileOf(stockId);
            }

            return System.Array.IndexOf(StringConstants.CODEX_STOCK_IDS, stockId);
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
