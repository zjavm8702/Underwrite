using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Patronage
{
    /// <summary>
    /// 주인공이 몰래 해 온 후원의 장부. 후원흐름 설계안 §3.
    ///
    /// 이 세계에서 후원을 생각하는 사람은 주인공 하나뿐이다. 그래서 이 장부도 주인공만 안다.
    /// 영지와 거래소를 넘나들어야 하므로 정적이다. 저장 기능이 들어오면 함께 저장한다.
    ///
    /// 단서(Clues)는 숫자로 보여 주지 않는다. 용사의 대사와 행동으로만 드러낸다(설계안 §2-6).
    /// </summary>
    public static class PatronageLedger
    {
        private static readonly List<HeroNeed> Needs = new List<HeroNeed>();
        // 의뢰에 대한 반응. 용사단은 다음 날 의뢰를 받아 보므로 낸 날 이후에만 들린다.
        private static readonly List<string> Reactions = new List<string>();
        private static readonly List<int> ReactionDays = new List<int>();

        /// <summary>기한을 넘긴 사정이 있다. 시나리오가 깨졌다.</summary>
        public static bool Collapsed { get; private set; }

        /// <summary>용사단이 의뢰주에 대해 쌓은 단서. 보여 주지 않는다.</summary>
        public static int Clues { get; private set; }

        /// <summary>낸 익명 의뢰 수.</summary>
        public static int RequestsPosted { get; private set; }

        public static IReadOnlyList<HeroNeed> All
        {
            get
            {
                EnsureScenario();
                return Needs;
            }
        }

        /// <summary>새 게임을 시작한다. 지금은 시제품 사정 두 건을 깐다.</summary>
        public static void Reset()
        {
            Needs.Clear();
            Reactions.Clear();
            ReactionDays.Clear();
            Collapsed = false;
            Clues = 0;
            RequestsPosted = 0;
        }

        /// <summary>그날이 된 사정을 연다. 하루가 시작되거나 영지에 나올 때 부른다.</summary>
        public static void Reveal(int day)
        {
            EnsureScenario();

            for (var i = 0; i < Needs.Count; i++)
            {
                if (Needs[i].State == NeedState.State_Hidden && day >= Needs[i].RevealDay)
                {
                    Needs[i].State = NeedState.State_Open;
                }
            }
        }

        /// <summary>
        /// 여관에서 엿들을 말을 하나 꺼낸다. 의뢰에 대한 반응이 먼저, 아직 못 들은 사정이 다음이다.
        /// 들을 것이 없으면 null.
        /// </summary>
        public static string Overhear(int day)
        {
            Reveal(day);

            for (var i = 0; i < Reactions.Count; i++)
            {
                if (ReactionDays[i] < day)
                {
                    var reaction = Reactions[i];
                    Reactions.RemoveAt(i);
                    ReactionDays.RemoveAt(i);
                    return reaction;
                }
            }

            for (var i = 0; i < Needs.Count; i++)
            {
                if (Needs[i].State == NeedState.State_Open && Needs[i].Heard == false)
                {
                    Needs[i].Heard = true;
                    return Needs[i].Rumor;
                }
            }

            return null;
        }

        /// <summary>
        /// 길드 창구에 익명 의뢰를 낸다. 맞는 사정이 열려 있으면 해결된다.
        /// 맞든 틀리든 돈은 나간다. 틀리면 용사단은 받기는 하되 처지에 맞지 않는다고 한다.
        /// </summary>
        public static HeroNeed Post(RequestPurpose purpose, int day)
        {
            Reveal(day);
            RequestsPosted++;

            for (var i = 0; i < Needs.Count; i++)
            {
                var need = Needs[i];

                if (need.State != NeedState.State_Open || need.Purpose != purpose)
                {
                    continue;
                }

                need.State = NeedState.State_Met;
                // 너무 정확한 의뢰는 의심을 남긴다. 여관에서 한 이야기를 아는 의뢰주다.
                Clues++;
                Reactions.Add(need.ReactionMet);
                ReactionDays.Add(day);
                return need;
            }

            Reactions.Add(StringConstants.TEXT_HERO_REACTION_MISSED);
            ReactionDays.Add(day);
            return null;
        }

        /// <summary>
        /// 하루가 끝났다. 기한이 지난 사정을 실패로 돌리고 그 수를 돌려준다.
        /// 하나라도 실패하면 시나리오가 깨진다.
        /// </summary>
        public static int EndOfDay(int day)
        {
            Reveal(day);
            var failed = 0;

            for (var i = 0; i < Needs.Count; i++)
            {
                if (Needs[i].State == NeedState.State_Open && day >= Needs[i].LastDay)
                {
                    Needs[i].State = NeedState.State_Failed;
                    failed++;
                }
            }

            if (failed > 0)
            {
                Collapsed = true;
            }

            return failed;
        }

        /// <summary>그 사정이 맞는 의뢰로 해결됐는가.</summary>
        public static bool IsMet(string needId)
        {
            EnsureScenario();

            for (var i = 0; i < Needs.Count; i++)
            {
                if (Needs[i].Id == needId)
                {
                    return Needs[i].State == NeedState.State_Met;
                }
            }

            return false;
        }

        /// <summary>들은 사정 가운데 기한이 가장 가까운 것까지 남은 날. 없으면 -1.</summary>
        public static int NearestDeadline(int day)
        {
            var best = -1;

            for (var i = 0; i < Needs.Count; i++)
            {
                var need = Needs[i];

                if (need.State != NeedState.State_Open || need.Heard == false)
                {
                    continue;
                }

                var left = need.LastDay - day + 1;

                if (best < 0 || left < best)
                {
                    best = left;
                }
            }

            return best;
        }

        /// <summary>
        /// 못 들은 채 기한이 다가오는 사정이 있는가. 마지막 단계는 숨기지 않는다(설계안 §1-5 완화책 1).
        /// </summary>
        public static bool HasUnheardUrgent(int day)
        {
            for (var i = 0; i < Needs.Count; i++)
            {
                var need = Needs[i];

                if (need.State == NeedState.State_Open && need.Heard == false
                    && need.LastDay - day <= Constants.PATRONAGE_WARNING_DAYS)
                {
                    return true;
                }
            }

            return false;
        }

        // 시제품 사정 두 건. 사용자 결정(2026-10-17): 2건, 기한 5일.
        private static void EnsureScenario()
        {
            if (Needs.Count > 0)
            {
                return;
            }

            Needs.Add(new HeroNeed
            {
                Id = StringConstants.NEED_HEALER_ID,
                Purpose = RequestPurpose.Purpose_Healing,
                RevealDay = 1,
                DeadlineDays = Constants.PATRONAGE_DEADLINE_DAYS,
                Rumor = StringConstants.TEXT_NEED_HEALER_RUMOR,
                ReactionMet = StringConstants.TEXT_NEED_HEALER_MET
            });

            Needs.Add(new HeroNeed
            {
                Id = "need_shield",
                Purpose = RequestPurpose.Purpose_Gear,
                RevealDay = 3,
                DeadlineDays = Constants.PATRONAGE_DEADLINE_DAYS,
                Rumor = StringConstants.TEXT_NEED_SHIELD_RUMOR,
                ReactionMet = StringConstants.TEXT_NEED_SHIELD_MET
            });
        }
    }
}
