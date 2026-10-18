using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Utility;

namespace Underwrite.Village
{
    /// <summary>
    /// 길드 기록 짜맞추기(2026-10-18 — 보드게임 「미니 크라임」을 줄인 추리. 사장님과 「구현해 보고 정한다」).
    ///
    /// 단 셋이 이번 계절 어느 목적지로, 누구의 의뢰를 받아 가는지 흩어진 기록 쪽지(단서)로 짜맞춘다.
    /// 단서는 정답이 하나뿐일 때까지 모으고, 없어도 되는 것은 뺀다. 플레이어는 표를 다 채워야만 맞춰 볼 수 있고,
    /// 틀리면 어디가 틀렸는지는 알려 주지 않는다(찍어서 맞히기를 막는다). 기회는 <see cref="Constants.DEDUCE_TRIES"/>번.
    /// 다 맞히면 가장 먼 원정(첫 목적지)을 가는 단의 대장간 사정을 안다 — 장부 맞추기와 같은 보상으로 둘을 견준다.
    /// </summary>
    public sealed class GuildDeduction
    {
        public const int SIZE = 3;

        /// <summary>표의 행. 단 이름(이름 없는 무리도 섞일 수 있다).</summary>
        public readonly List<string> BandNames = new List<string>();
        public readonly List<string> BandIds = new List<string>();

        /// <summary>열의 보기. 순서는 화면에 보이는 대로.</summary>
        public readonly string[] Destinations = new string[SIZE];
        public readonly string[] Clients = new string[SIZE];

        /// <summary>펼쳐 놓을 기록 쪽지.</summary>
        public readonly List<string> Clues = new List<string>();

        /// <summary>정답. [단 행] = 목적지·의뢰인 순번.</summary>
        public readonly int[] DestinationOf = new int[SIZE];
        public readonly int[] ClientOf = new int[SIZE];

        public int TriesLeft { get; private set; }

        /// <summary>보상받을 단(가장 먼 원정 = 0번 목적지를 가는 단).</summary>
        public string TargetId { get; private set; }

        /// <summary>프롤로그의 연습 기록인가. 기회가 줄지 않고 보상이 없다.</summary>
        public bool IsPractice { get; private set; }

        private readonly List<Clue> Facts = new List<Clue>();

        private GuildDeduction()
        {
            TriesLeft = Constants.DEDUCE_TRIES;
        }

        // 단서 한 장. 둘을 잇거나(같다) 떼어 놓는다(다르다). 종류: 0 단-목적지, 1 단-의뢰인, 2 목적지-의뢰인.
        private struct Clue
        {
            public int Kind;
            public int A;
            public int B;
            public bool Same;
        }

        /// <summary>
        /// 오늘의 기록을 짠다. 대장간 사정을 아직 모르고 이번 계절 움직임이 있는 단이 없으면 null.
        /// </summary>
        public static GuildDeduction Create(MarketSimulation sim, System.Random rng)
        {
            if (sim == null)
            {
                return null;
            }

            var turn = BandCodex.TurnOf(sim.Clock.Cycle);
            var subjects = sim.CodexSubjects();
            var candidates = new List<string>();
            var others = new List<string>();

            for (var i = 0; i < subjects.Count; i++)
            {
                var profile = sim.CodexProfileOf(subjects[i]);

                if (profile < 0)
                {
                    continue;
                }

                if (BandCodex.IsKnown(subjects[i], CodexSource.Codex_Smith) == false
                    && Constants.CODEX_SMITH_EFFECT[profile][turn] != 0f)
                {
                    candidates.Add(subjects[i]);
                }
                else
                {
                    others.Add(subjects[i]);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var puzzle = new GuildDeduction();
            puzzle.TargetId = candidates[rng.Next(candidates.Count)];
            puzzle.Fill(sim, candidates, others, rng);
            return puzzle;
        }

        /// <summary>
        /// 연습 기록(프롤로그 길드 정거장). 시장이 없어도 된다 — 이름 없는 무리 셋으로 같은 규칙의 퍼즐을 짠다.
        /// 늘 같은 판이 나오게 씨앗을 고정한다(설명과 화면이 어긋나지 않게).
        /// </summary>
        public static GuildDeduction CreatePractice()
        {
            var rng = new System.Random(Constants.DEDUCE_PRACTICE_SEED);
            var puzzle = new GuildDeduction();
            puzzle.IsPractice = true;

            foreach (var name in StringConstants.TEXT_LEDGER_FILLER_BANDS)
            {
                puzzle.BandIds.Add(null);
                puzzle.BandNames.Add(name);
            }

            PickDistinct(StringConstants.TEXT_DEDUCE_DESTINATIONS, puzzle.Destinations, rng);
            PickDistinct(StringConstants.TEXT_DEDUCE_CLIENTS, puzzle.Clients, rng);

            var destOrder = new List<int> { 0, 1, 2 };
            var clientOrder = new List<int> { 0, 1, 2 };
            Shuffle(destOrder, rng);
            Shuffle(clientOrder, rng);

            for (var row = 0; row < SIZE; row++)
            {
                puzzle.DestinationOf[row] = destOrder[row];
                puzzle.ClientOf[row] = clientOrder[row];
            }

            puzzle.BuildClues(rng);
            return puzzle;
        }

        /// <summary>
        /// 플레이어의 표를 맞춰 본다. 다 맞으면 참. 틀리면 기회가 하나 준다.
        /// 표에 빈칸(-1)이 있으면 맞춰 보지 않는다 — 부른 쪽이 막아야 한다.
        /// </summary>
        public bool Check(int[] destinations, int[] clients)
        {
            for (var i = 0; i < SIZE; i++)
            {
                if (destinations[i] != DestinationOf[i] || clients[i] != ClientOf[i])
                {
                    if (IsPractice == false)
                    {
                        TriesLeft--;
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>펼친 단서를 모두 만족하는 배정의 수. 늘 1이어야 한다(시험용).</summary>
        public int SolutionCount()
        {
            return CountSolutions(Facts);
        }

        public bool IsOutOfTries
        {
            get
            {
                return TriesLeft <= 0;
            }
        }

        private void Fill(MarketSimulation sim, List<string> candidates, List<string> others, System.Random rng)
        {
            // 행: 보상받을 단 + 다른 단 둘(모자라면 이름 없는 무리).
            var rows = new List<string> { TargetId };
            var pool = new List<string>();

            foreach (var id in candidates)
            {
                if (id != TargetId)
                {
                    pool.Add(id);
                }
            }

            pool.AddRange(others);
            Shuffle(pool, rng);

            for (var i = 0; i < pool.Count && rows.Count < SIZE; i++)
            {
                rows.Add(pool[i]);
            }

            var fillers = StringConstants.TEXT_LEDGER_FILLER_BANDS;

            while (rows.Count < SIZE)
            {
                rows.Add(null);
            }

            Shuffle(rows, rng);
            var fillerIndex = 0;

            foreach (var id in rows)
            {
                BandIds.Add(id);
                BandNames.Add(id == null ? fillers[fillerIndex++ % fillers.Length] : sim.NameOf(id));
            }

            PickDistinct(StringConstants.TEXT_DEDUCE_DESTINATIONS, Destinations, rng);
            PickDistinct(StringConstants.TEXT_DEDUCE_CLIENTS, Clients, rng);

            // 정답: 보상받을 단이 0번 목적지(가장 먼 원정)를 간다. 나머지는 섞는다.
            var destOrder = new List<int> { 1, 2 };
            Shuffle(destOrder, rng);
            var clientOrder = new List<int> { 0, 1, 2 };
            Shuffle(clientOrder, rng);
            var next = 0;

            for (var row = 0; row < SIZE; row++)
            {
                DestinationOf[row] = BandIds[row] == TargetId ? 0 : destOrder[next++];
                ClientOf[row] = clientOrder[row];
            }

            BuildClues(rng);
        }

        // 참인 단서를 섞어 하나씩 넣다가 답이 하나로 좁혀지면 멈추고, 빼도 답이 그대로인 단서는 뺀다.
        // 단을 목적지에 바로 잇는 단서는 넣지 않는다 — 너무 쉽다. 퍼즐을 짤 때 한 번만 돈다(저빈도).
        private void BuildClues(System.Random rng)
        {
            var all = new List<Clue>();

            for (var a = 0; a < SIZE; a++)
            {
                for (var b = 0; b < SIZE; b++)
                {
                    all.Add(new Clue { Kind = 0, A = a, B = b, Same = DestinationOf[a] == b });
                    all.Add(new Clue { Kind = 1, A = a, B = b, Same = ClientOf[a] == b });
                    all.Add(new Clue { Kind = 2, A = a, B = b, Same = ClientOfDestination(a) == b });
                }
            }

            all.RemoveAll(c => c.Kind == 0 && c.Same == true);
            Shuffle(all, rng);

            foreach (var clue in all)
            {
                Facts.Add(clue);

                if (CountSolutions(Facts) == 1)
                {
                    break;
                }
            }

            for (var i = Facts.Count - 1; i >= 0; i--)
            {
                var removed = Facts[i];
                Facts.RemoveAt(i);

                if (CountSolutions(Facts) != 1)
                {
                    Facts.Insert(i, removed);
                }
            }

            foreach (var clue in Facts)
            {
                Clues.Add(Describe(clue));
            }
        }

        private int ClientOfDestination(int destination)
        {
            for (var row = 0; row < SIZE; row++)
            {
                if (DestinationOf[row] == destination)
                {
                    return ClientOf[row];
                }
            }

            return -1;
        }

        // 가능한 배정(목적지 순열 × 의뢰인 순열 = 36가지) 가운데 단서를 모두 만족하는 수.
        private static int CountSolutions(List<Clue> clues)
        {
            var perms = Permutations();
            var count = 0;

            foreach (var dest in perms)
            {
                foreach (var client in perms)
                {
                    if (Satisfies(clues, dest, client) == true)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool Satisfies(List<Clue> clues, int[] dest, int[] client)
        {
            foreach (var clue in clues)
            {
                bool holds;

                if (clue.Kind == 0)
                {
                    holds = dest[clue.A] == clue.B;
                }
                else if (clue.Kind == 1)
                {
                    holds = client[clue.A] == clue.B;
                }
                else
                {
                    var row = System.Array.IndexOf(dest, clue.A);
                    holds = client[row] == clue.B;
                }

                if (holds != clue.Same)
                {
                    return false;
                }
            }

            return true;
        }

        private string Describe(Clue clue)
        {
            if (clue.Kind == 0)
            {
                return Two(StringConstants.TEXT_DEDUCE_BAND_NOT_DEST, BandNames[clue.A], Destinations[clue.B]);
            }

            if (clue.Kind == 1)
            {
                var format = clue.Same == true
                    ? StringConstants.TEXT_DEDUCE_BAND_CLIENT
                    : StringConstants.TEXT_DEDUCE_BAND_NOT_CLIENT;
                return Two(format, BandNames[clue.A], Clients[clue.B]);
            }

            var linkFormat = clue.Same == true
                ? StringConstants.TEXT_DEDUCE_DEST_CLIENT
                : StringConstants.TEXT_DEDUCE_DEST_NOT_CLIENT;
            return Two(linkFormat, Destinations[clue.A], Clients[clue.B]);
        }

        // 조사는 첫째 이름에만 붙는다(Josa는 {0} 하나만 다룬다). 둘째는 조사 없이 채운다.
        private static string Two(string template, string first, string second)
        {
            return string.Format(Josa.Format(template, first), first, second);
        }

        private static List<int[]> Permutations()
        {
            return new List<int[]>
            {
                new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 }
            };
        }

        private static void PickDistinct(string[] source, string[] target, System.Random rng)
        {
            var pool = new List<string>(source);
            Shuffle(pool, rng);

            for (var i = 0; i < target.Length; i++)
            {
                target[i] = pool[i];
            }
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
        }
    }
}
