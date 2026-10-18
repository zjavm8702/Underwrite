using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;

namespace Underwrite.Village
{
    /// <summary>
    /// 여관 엿듣기(시간구조 설계안 §7). 술을 나르며 테이블 사이를 돈다 — 누구 곁에 머물지 고르는 판단형이다
    /// (2026-10-18 인터뷰: 사장님은 「한정된 시간·의심으로 무엇을 들을지 고르는」 쪽에도 끌렸다).
    ///
    /// 귀 기울일 기회는 몇 번뿐이다. 한 테이블에 두 번 머물러야 소문 한 줄을 다 듣는데,
    /// 테이블마다 경계심이 달라 오래 붙어 있으면 들킨다. 분위기 실마리(자꾸 둘러본다 / 취해 떠든다)로 짐작한다.
    /// 들키면 그날 엿듣기는 끝이다. 용사단 테이블에서는 그들의 사정을 듣는다.
    /// </summary>
    public sealed class EavesdropSession
    {
        /// <summary>테이블 하나. 용병단이거나 용사단이다.</summary>
        public sealed class Table
        {
            /// <summary>용병단 종목 아이디. 용사단 테이블이면 비어 있다.</summary>
            public string BandId;
            public string Name;

            /// <summary>몇 번째 머묾에서 들키는가. 이 횟수까지는 괜찮다.</summary>
            public int Wariness;
            public int Listens;
            public bool IsHeroes;

            /// <summary>다 들었는가(소문 한 줄을 끝까지 / 용사단의 말 한마디).</summary>
            public bool IsDone;
        }

        public readonly List<Table> Tables = new List<Table>();

        public int TurnsLeft { get; private set; }

        /// <summary>들켰는가. 들키면 그날 엿듣기는 끝이다.</summary>
        public bool IsCaught { get; private set; }

        public bool IsOver
        {
            get
            {
                return IsCaught == true || TurnsLeft <= 0;
            }
        }

        private readonly int Turn;

        private EavesdropSession(int turn)
        {
            Turn = turn;
            TurnsLeft = Constants.EAVES_TURNS;
        }

        /// <summary>오늘의 테이블을 차린다. 들을 것이 하나도 없으면 null.</summary>
        public static EavesdropSession Create(MarketSimulation sim, bool heroesHaveTalk, System.Random rng)
        {
            var session = new EavesdropSession(sim == null ? 0 : BandCodex.TurnOf(sim.Clock.Cycle));

            // 경계심은 섞인 묶음에서 나눠 준다(1·2·3을 한 번씩, 남으면 다시). 모두 1이면 끝까지 들을 테이블이 없다.
            var pool = new List<int>();

            if (sim != null)
            {
                var subjects = sim.CodexSubjects();

                for (var i = 0; i < subjects.Count && session.Tables.Count < Constants.EAVES_BAND_TABLES; i++)
                {
                    if (sim.CodexProfileOf(subjects[i]) < 0
                        || BandCodex.IsKnown(subjects[i], CodexSource.Codex_Tavern) == true)
                    {
                        continue;
                    }

                    if (pool.Count == 0)
                    {
                        pool.AddRange(new[] { 1, 2, 3 });
                    }

                    var pick = rng.Next(pool.Count);
                    session.Tables.Add(new Table
                    {
                        BandId = subjects[i],
                        Name = sim.NameOf(subjects[i]),
                        Wariness = pool[pick]
                    });
                    pool.RemoveAt(pick);
                }
            }

            if (heroesHaveTalk == true)
            {
                // 늘 앉는 단골 자리다. 일꾼이 곁에 오래 서 있어도 그러려니 한다.
                session.Tables.Insert(rng.Next(session.Tables.Count + 1), new Table
                {
                    Name = StringConstants.TEXT_HERO_PARTY,
                    Wariness = 3,
                    IsHeroes = true
                });
            }

            return session.Tables.Count == 0 ? null : session;
        }

        /// <summary>분위기 실마리. 경계심을 말로 흘린다.</summary>
        public static string MoodOf(Table table)
        {
            var moods = StringConstants.TEXT_EAVES_MOODS;
            return moods[System.Math.Max(0, System.Math.Min(moods.Length - 1, table.Wariness - 1))];
        }

        /// <summary>
        /// 그 테이블 곁에 머문다. 기회를 하나 쓰고, 경계심을 넘으면 들킨다.
        /// 들키지 않았으면 참.
        /// </summary>
        public bool Listen(Table table)
        {
            if (IsOver == true || table.IsDone == true)
            {
                return false;
            }

            TurnsLeft--;
            table.Listens++;

            if (table.Listens > table.Wariness)
            {
                IsCaught = true;
                return false;
            }

            if (table.IsHeroes == true || table.Listens >= Constants.EAVES_LISTENS_TO_FINISH)
            {
                table.IsDone = true;
            }

            return true;
        }

        /// <summary>
        /// 첫 머묾에 들리는 조각. 그 단의 이번 계절 여관 소문에서 낱말 몇 개만 흘린다 — 두 번째에 문장이 다 들린다.
        /// </summary>
        public string FragmentOf(MarketSimulation sim, Table table)
        {
            var profile = sim.CodexProfileOf(table.BandId);

            if (profile < 0)
            {
                return string.Empty;
            }

            var words = StringConstants.CODEX_TAVERN[profile][Turn].TrimEnd('.').Split(' ');
            var take = System.Math.Min(2, words.Length);
            var start = System.Math.Max(0, (words.Length - take) / 2);
            return string.Format(StringConstants.TEXT_EAVES_FRAGMENT, table.Name,
                string.Join(" ", words, start, take));
        }
    }
}
