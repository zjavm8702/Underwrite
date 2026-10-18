using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Save;

namespace Underwrite.Story
{
    /// <summary>
    /// 계절 계획(시간구조 설계안 §3). 원정 기간 세 달에 맡을 일을 출발 밤에 고른다.
    ///
    /// 「몇 달이 흘렀다」 한 줄로 넘기지 않으려는 장치다. 돈·정보·관계·이야기 가운데 무엇을 쌓을지가
    /// 다음 계절의 판을 바꾼다. 바로 생기는 효과(돈·호감·단서)는 고른 즉시, 다음 계절의 단에 대한 것은
    /// 재개장 때(새 단이 도착한 뒤) 적용한다.
    /// 씬을 넘나들므로 정적이다. 함께 저장한다.
    /// </summary>
    public static class SeasonPlan
    {
        private static readonly HashSet<int> Departed = new HashSet<int>();
        private static int PendingGuild;
        private static int PendingSmith;
        private static int PendingTips;

        /// <summary>찾은 국경의 징조 수. 멸망의 단서다(설계안 §4). 엔딩·이야기 진행이 읽는다.</summary>
        public static int OmenClues { get; private set; }

        public static void Reset()
        {
            Departed.Clear();
            PendingGuild = 0;
            PendingSmith = 0;
            PendingTips = 0;
            OmenClues = 0;
        }

        /// <summary>그 계절의 출발(과 계절 계획)을 마쳤는가.</summary>
        public static bool HasDeparted(int season)
        {
            return Departed.Contains(season);
        }

        public static void MarkDeparted(int season)
        {
            Departed.Add(season);
        }

        /// <summary>
        /// 고른 세 달을 적용하고 몽타주 문장을 돌려준다.
        /// 여관 일로 번 돈은 영지 지갑(<see cref="GatherSession"/>)에 들어가 다음 장에 지갑으로 간다.
        /// </summary>
        public static List<string> Apply(IList<SeasonTask> months, HeroProfile companion)
        {
            var lines = new List<string>();

            for (var i = 0; i < months.Count; i++)
            {
                var task = months[i];
                var line = StringConstants.TEXT_PLAN_MONTAGE[(int)task];

                switch (task)
                {
                    case SeasonTask.Task_Inn:
                    {
                        GatherSession.Record(0f, Constants.PLAN_INN_EARN, null);
                        break;
                    }
                    case SeasonTask.Task_Guild:
                    {
                        PendingGuild++;
                        break;
                    }
                    case SeasonTask.Task_Smith:
                    {
                        PendingSmith++;
                        break;
                    }
                    case SeasonTask.Task_Underworld:
                    {
                        PendingTips++;
                        break;
                    }
                    case SeasonTask.Task_Wagon:
                    {
                        HeroRoster.AddAffinityToAll(Constants.PLAN_WAGON_AFFINITY);
                        line = string.Format(line, companion == null ? string.Empty : companion.Name);
                        break;
                    }
                    case SeasonTask.Task_Omens:
                    {
                        var omens = StringConstants.TEXT_PLAN_OMENS;
                        line = string.Format(line, omens[System.Math.Min(OmenClues, omens.Length - 1)]);
                        OmenClues++;
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }

                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 재개장 장이 열렸다. 새 단이 도착한 뒤라 다음 계절의 단에 대한 준비를 지금 푼다.
        /// 알릴 말을 돌려준다. 풀 것이 없으면 빈 목록.
        /// </summary>
        public static List<string> ApplyAtReopening(MarketSimulation sim)
        {
            var notes = new List<string>();

            for (; PendingGuild > 0; PendingGuild--)
            {
                AddLearned(sim, CodexSource.Codex_Guild, StringConstants.TEXT_PLAN_RESULT_GUILD, notes);
            }

            for (; PendingSmith > 0; PendingSmith--)
            {
                AddLearned(sim, CodexSource.Codex_Smith, StringConstants.TEXT_PLAN_RESULT_SMITH, notes);
            }

            if (PendingTips > 0)
            {
                sim.GrantInformantTips(PendingTips);
                notes.Add(string.Format(StringConstants.TEXT_PLAN_RESULT_TIPS, PendingTips));
                PendingTips = 0;
            }

            return notes;
        }

        private static void AddLearned(MarketSimulation sim, CodexSource source, string format, List<string> notes)
        {
            var id = BandCodex.PickUnknown(sim, source);

            if (id == null)
            {
                return;
            }

            var fact = BandCodex.Learn(id, source);

            if (fact != null)
            {
                notes.Add(string.Format(format, sim.NameOf(id), fact));
            }
        }

        internal static SeasonPlanState Capture()
        {
            var state = new SeasonPlanState
            {
                PendingGuild = PendingGuild,
                PendingSmith = PendingSmith,
                PendingTips = PendingTips,
                OmenClues = OmenClues
            };

            state.Departed.AddRange(Departed);
            return state;
        }

        internal static void Restore(SeasonPlanState state)
        {
            Reset();

            if (state == null)
            {
                return;
            }

            Departed.UnionWith(state.Departed);
            PendingGuild = state.PendingGuild;
            PendingSmith = state.PendingSmith;
            PendingTips = state.PendingTips;
            OmenClues = state.OmenClues;
        }
    }
}
