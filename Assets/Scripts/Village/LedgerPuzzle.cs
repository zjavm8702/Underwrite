using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;

namespace Underwrite.Village
{
    /// <summary>
    /// 장부 맞추기(시간구조 설계안 §7 — 길드 기록실). 첫 추리형 정보 모으기다(2026-10-18 인터뷰: 미니게임은 추리).
    ///
    /// 의뢰 기록의 약정 보수와 지급 기록의 실제 지급을 맞춰 본다. 지급은 선금·잔금으로 쪼개져 날짜순으로 섞여 있어
    /// 더해 봐야 맞는지 안다. 한 단만 어긋난다 — 그 단의 대장간 사정(원정 결과의 방향)이 장부에 새어 나온 것이다.
    /// 많이 받았으면 누군가 장비 값을 대고 있고, 덜 받았으면 보수가 묶였다.
    /// 잘못 짚으면 기록 담당의 의심이 오른다. 돈 대신 시간과 위험, 그리고 머리를 쓰는 길이다.
    /// </summary>
    public sealed class LedgerPuzzle
    {
        public readonly List<LedgerRequest> Requests = new List<LedgerRequest>();
        public readonly List<LedgerPayment> Payments = new List<LedgerPayment>();

        /// <summary>어긋난 단. 이 단의 대장간 사정이 보상이다.</summary>
        public string TargetId { get; private set; }

        /// <summary>약정보다 많이 나갔는가(원정을 크게 준비한다). 아니면 덜 나갔다(보수가 묶였다).</summary>
        public bool TargetGains { get; private set; }

        public int TargetReward { get; private set; }
        public int TargetPaid { get; private set; }

        /// <summary>기록 담당의 의심. <see cref="Constants.LEDGER_MAX_SUSPICION"/>이면 쫓겨난다.</summary>
        public int Suspicion { get; private set; }

        public bool IsKickedOut
        {
            get
            {
                return Suspicion >= Constants.LEDGER_MAX_SUSPICION;
            }
        }

        private LedgerPuzzle()
        {
        }

        /// <summary>
        /// 오늘의 장부를 짠다. 대장간 사정을 아직 모르고 이번 계절에 움직임이 있는 단이 없으면 null.
        /// </summary>
        public static LedgerPuzzle Create(MarketSimulation sim, System.Random rng, int day)
        {
            if (sim == null)
            {
                return null;
            }

            var turn = BandCodex.TurnOf(sim.Clock.Cycle);
            var subjects = sim.CodexSubjects();
            var candidates = new List<string>();

            for (var i = 0; i < subjects.Count; i++)
            {
                var profile = sim.CodexProfileOf(subjects[i]);

                if (profile < 0 || BandCodex.IsKnown(subjects[i], CodexSource.Codex_Smith) == true)
                {
                    continue;
                }

                if (Constants.CODEX_SMITH_EFFECT[profile][turn] != 0f)
                {
                    candidates.Add(subjects[i]);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var puzzle = new LedgerPuzzle();
            puzzle.TargetId = candidates[rng.Next(candidates.Count)];
            puzzle.TargetGains = Constants.CODEX_SMITH_EFFECT[sim.CodexProfileOf(puzzle.TargetId)][turn] > 0f;
            puzzle.Fill(sim, subjects, rng, day);
            return puzzle;
        }

        /// <summary>
        /// 이 의뢰의 단이 어긋난 단인가. 아니면 의심이 하나 오른다.
        /// </summary>
        public bool Check(int requestIndex)
        {
            if (requestIndex >= 0 && requestIndex < Requests.Count && Requests[requestIndex].BandId == TargetId)
            {
                return true;
            }

            Suspicion++;
            return false;
        }

        private void Fill(MarketSimulation sim, List<string> subjects, System.Random rng, int day)
        {
            // 어긋난 단을 꼭 넣고, 나머지 단으로 줄을 채운다. 모자라면 이름 없는 무리를 넣는다.
            var bands = new List<string> { TargetId };

            for (var i = 0; i < subjects.Count && bands.Count < Constants.LEDGER_MAX_ROWS; i++)
            {
                if (subjects[i] != TargetId)
                {
                    bands.Add(subjects[i]);
                }
            }

            Shuffle(bands, rng);
            var fillers = StringConstants.TEXT_LEDGER_FILLER_BANDS;

            for (var i = 0; bands.Count < Constants.LEDGER_MIN_ROWS && i < fillers.Length; i++)
            {
                bands.Insert(rng.Next(bands.Count + 1), null);
            }

            var fillerIndex = 0;
            var jobs = StringConstants.TEXT_LEDGER_JOBS;

            for (var i = 0; i < bands.Count; i++)
            {
                var id = bands[i];
                var request = new LedgerRequest
                {
                    BandId = id,
                    BandName = id == null ? fillers[fillerIndex++] : sim.NameOf(id),
                    Job = jobs[rng.Next(jobs.Length)],
                    Reward = rng.Next(8, 21) * 10
                };

                Requests.Add(request);
                AddPayments(request, rng, day);
            }

            // 날짜순으로 적는다. 같은 날 안에서는 선금 → 잔금 순이되 단끼리는 섞여 있다 — 단 이름으로 찾아 더해야 한다.
            // 장부를 짤 때 한 번만 돈다(저빈도).
            Shuffle(Payments, rng);
            var notes = StringConstants.TEXT_LEDGER_NOTES;
            Payments.Sort((a, b) =>
            {
                var byDay = a.Day.CompareTo(b.Day);
                return byDay != 0 ? byDay : System.Array.IndexOf(notes, a.Note).CompareTo(System.Array.IndexOf(notes, b.Note));
            });
        }

        private void AddPayments(LedgerRequest request, System.Random rng, int day)
        {
            var isTarget = request.BandId != null && request.BandId == TargetId;
            var total = request.Reward;

            if (isTarget == true)
            {
                var gap = rng.Next(3, 7) * 10;
                total = TargetGains == true ? request.Reward + gap : request.Reward - gap;
                TargetReward = request.Reward;
                TargetPaid = total;
            }

            // 어긋난 단도 다른 단과 똑같이 쪼갠다. 비고만 보고 찾을 수 없게.
            var splits = isTarget == true ? 2 : rng.Next(1, 4);
            var notes = StringConstants.TEXT_LEDGER_NOTES;

            // 쪼갠 지급은 날짜가 뒤로 갈 뿐 앞으로 오지 않는다. 선금이 잔금보다 늦게 나가면 장부가 거짓말을 하는 셈이다.
            var paidOn = System.Math.Max(1, day - rng.Next(0, Constants.LEDGER_DAYS_BACK + 1));

            if (splits == 1)
            {
                Payments.Add(Payment(request, total, notes[3], paidOn));
                return;
            }

            var first = System.Math.Max(10, total * rng.Next(3, 6) / 100 * 10);
            Payments.Add(Payment(request, first, notes[0], paidOn));

            if (splits == 3)
            {
                var middle = System.Math.Max(10, (total - first) / 20 * 10);
                paidOn = System.Math.Min(day, paidOn + rng.Next(0, 2));
                Payments.Add(Payment(request, middle, notes[1], paidOn));
                first += middle;
            }

            paidOn = System.Math.Min(day, paidOn + rng.Next(0, 2));
            Payments.Add(Payment(request, total - first, notes[2], paidOn));
        }

        private static LedgerPayment Payment(LedgerRequest request, int amount, string note, int day)
        {
            return new LedgerPayment
            {
                Day = day,
                BandName = request.BandName,
                Amount = amount,
                Note = note
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
