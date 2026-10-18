using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;

namespace Underwrite.Village
{
    /// <summary>
    /// 주문서 읽기(대장간). 셋째 정보 모으기(2026-10-18) — 추리형.
    ///
    /// 여관에서 도는 소문 하나를 대장간 주문서로 가린다. 꽂이에는 여러 단의 주문서가 날짜순으로 꽂혀 있고,
    /// 소문 속 단의 주문은 둘이나 셋이다. 그 가운데 **이번 계절, 취소되지 않은 주문** 하나만 증거다.
    /// 지난 계절 주문과 취소된 주문은 반대 이야기를 하는 미끼다. 증거를 짚고 나서 맞는 말인지 헛말인지 판정한다.
    /// 소문의 진위는 시장이 처음부터 정해 두었다(<see cref="MarketSimulation.IsTavernTrue"/>).
    /// 잘못 짚으면 대장장이의 의심이 오른다.
    /// </summary>
    public sealed class SmithOrderPuzzle
    {
        public readonly List<SmithSlip> Slips = new List<SmithSlip>();

        public string TargetId { get; private set; }
        public string Rumor { get; private set; }
        public bool RumorTrue { get; private set; }

        /// <summary>증거 주문서의 자리.</summary>
        public int EvidenceIndex { get; private set; }

        public int Suspicion { get; private set; }

        public bool IsKickedOut
        {
            get
            {
                return Suspicion >= Constants.SMITH_ORDER_MAX_SUSPICION;
            }
        }

        public SmithSlip Evidence
        {
            get
            {
                return Slips[EvidenceIndex];
            }
        }

        private SmithOrderPuzzle()
        {
        }

        /// <summary>
        /// 오늘의 꽂이를 짠다. 들은 소문을 먼저 고른다. 이번 계절 소문을 모두 가렸으면 null.
        /// </summary>
        /// <param name="day">오늘(게임 전체의 날짜).</param>
        public static SmithOrderPuzzle Create(MarketSimulation sim, System.Random rng, int day)
        {
            if (sim == null)
            {
                return null;
            }

            var subjects = sim.CodexSubjects();
            var heard = new List<string>();
            var others = new List<string>();

            for (var i = 0; i < subjects.Count; i++)
            {
                if (sim.CodexProfileOf(subjects[i]) < 0 || BandCodex.IsVerified(subjects[i]) == true)
                {
                    continue;
                }

                if (BandCodex.IsKnown(subjects[i], CodexSource.Codex_Tavern) == true)
                {
                    heard.Add(subjects[i]);
                }
                else
                {
                    others.Add(subjects[i]);
                }
            }

            var pool = heard.Count > 0 ? heard : others;

            if (pool.Count == 0)
            {
                return null;
            }

            var cycle = sim.Clock.Cycle;
            var turn = BandCodex.TurnOf(cycle);
            var puzzle = new SmithOrderPuzzle();
            puzzle.TargetId = pool[rng.Next(pool.Count)];
            var profile = sim.CodexProfileOf(puzzle.TargetId);
            puzzle.Rumor = StringConstants.CODEX_TAVERN[profile][turn].TrimEnd('.');
            puzzle.RumorTrue = sim.IsTavernTrue(puzzle.TargetId, cycle);
            puzzle.Fill(sim, subjects, StringConstants.SMITH_EVIDENCE[profile][turn], rng, day);
            return puzzle;
        }

        /// <summary>짚은 주문서가 무엇인가. 증거가 아니면 의심이 하나 오른다.</summary>
        public SlipPick Check(int index)
        {
            if (index == EvidenceIndex)
            {
                return SlipPick.Pick_Evidence;
            }

            Suspicion++;

            if (index < 0 || index >= Slips.Count || Slips[index].BandId != TargetId)
            {
                return SlipPick.Pick_OtherBand;
            }

            return Slips[index].Day == 0 ? SlipPick.Pick_LastSeason : SlipPick.Pick_Cancelled;
        }

        private void Fill(MarketSimulation sim, List<string> subjects, string[] evidence, System.Random rng, int day)
        {
            var today = MarketClock.DayInSeason(day);
            var name = sim.NameOf(TargetId);
            var real = evidence[RumorTrue == true ? 0 : 1];
            var decoy = evidence[RumorTrue == true ? 1 : 0];
            var cancelled = StringConstants.TEXT_SMITH_STATUS.Length - 1;

            var proof = Slip(TargetId, name, real, rng.Next(1, today + 1), rng.Next(cancelled));
            Slips.Add(proof);

            // 취소된 미끼는 늘, 지난 계절 미끼는 둘째 계절부터 넣는다. 둘 다 반대 이야기를 한다.
            Slips.Add(Slip(TargetId, name, decoy, rng.Next(1, today + 1), cancelled));

            if (MarketClock.SeasonOf(day) > 1 && rng.NextDouble() < Constants.SMITH_ORDER_OLD_DECOY_CHANCE)
            {
                Slips.Add(Slip(TargetId, name, decoy, 0, rng.Next(cancelled)));
            }

            // 나머지는 다른 단의 평범한 주문. 모자라면 이름 없는 무리로 채운다.
            var bands = new List<string>();

            for (var i = 0; i < subjects.Count; i++)
            {
                if (subjects[i] != TargetId)
                {
                    bands.Add(subjects[i]);
                }
            }

            Shuffle(bands, rng);
            var noise = StringConstants.TEXT_SMITH_NOISE_ITEMS;
            var fillers = StringConstants.TEXT_LEDGER_FILLER_BANDS;
            var next = 0;

            while (Slips.Count < Constants.SMITH_ORDER_SLIPS)
            {
                var id = next < bands.Count ? bands[next] : null;
                var who = id != null ? sim.NameOf(id) : fillers[(next - bands.Count) % fillers.Length];
                Slips.Add(Slip(id, who, noise[rng.Next(noise.Length)], rng.Next(0, today + 1),
                    rng.Next(StringConstants.TEXT_SMITH_STATUS.Length)));
                next++;
            }

            // 꽂이는 날짜순이다(지난 계절이 맨 앞). 같은 날 안에서는 섞여 있다. 꽂이를 짤 때 한 번만 돈다(저빈도).
            Shuffle(Slips, rng);
            Slips.Sort((a, b) => a.Day.CompareTo(b.Day));
            EvidenceIndex = Slips.IndexOf(proof);
        }

        private static SmithSlip Slip(string id, string name, string item, int day, int status)
        {
            return new SmithSlip
            {
                BandId = id,
                BandName = name,
                Item = item,
                Day = day,
                Status = status
            };
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
