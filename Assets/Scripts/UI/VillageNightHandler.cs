using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using Underwrite.Utility;
using UnityEngine;

namespace Underwrite.UI
{
    /// <summary>
    /// 하루와 계절의 끝 — 다락방의 잠(그날 정리·혼잣말)과 계절 마지막 밤의 출발(세 달 고르기·몽타주·원정지 편지).
    /// 영지 화면(<see cref="VillageScreenController"/>)이 조립한다(2026-10-19 정리 — 화면에서 떼어 냄).
    /// </summary>
    public sealed class VillageNightHandler
    {
        private readonly VillageScreenController Host;

        public VillageNightHandler(VillageScreenController host)
        {
            Host = host;
        }

        // ── 출발과 계절 계획 (시간구조 설계안 §2·§3) ───────
        // 계절의 마지막 밤이 지나면 용사단이 떠나고, 주인공은 휴장하는 석 달에 할 일을 고른다.
        // 고른 대로 몽타주가 흐르고, 친한 용사가 있으면 원정지에서 편지가 온다. 끝나면 새 계절 첫날 아침이다.

        private bool isDepartureRunning;

        /// <summary>출발 장면(작별 → 세 달 고르기 → 몽타주 → 편지)이 이어지는 중인가.</summary>
        public bool IsDepartureRunning
        {
            get
            {
                return isDepartureRunning;
            }
        }
        private readonly List<SeasonTask> PlanMonths = new List<SeasonTask>();

        /// <summary>
        /// 새 계절 첫날이 됐는데 지난 계절의 출발을 아직 치르지 않았으면 띄운다.
        /// 4일째 오후장을 거래소에서 마쳤든 영지에서 건너뛰었든, 자정이 지나면 같은 자리에서 시작한다.
        /// </summary>
        public void PlayDepartureIfDue()
        {
            if (isDepartureRunning == true || Host.Clock == null || Host.Clock.IsBreak == false || Host.IsApproaching == true
                || Host.Dialogue == null || Host.Dialogue.IsOpen == true
                || Host.IsTutorialOpen == true)
            {
                return;
            }

            var today = Host.Today();

            if (today <= 1 || MarketClock.DayInSeason(today) != 1
                || SeasonPlan.HasDeparted(MarketClock.SeasonOf(today) - 1) == true)
            {
                return;
            }

            isDepartureRunning = true;
            PlanMonths.Clear();
            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(InfoSource.Source_TavernTalk));

            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_DEPART_TAG, StringConstants.TEXT_DEPART_OPEN)
            };

            // 모두 떠났으면 작별을 건넬 사람이 없다.
            var farewell = HeroRoster.Favorite();

            if (farewell != null)
            {
                lines.Add(Host.HeroLine(farewell, StringConstants.TEXT_DEPART_FAREWELL, PortraitEmotion.Emotion_Happy));
            }

            lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_DEPART_TAG, StringConstants.TEXT_DEPART_PLAN));

            Host.Dialogue.Play(lines, AskPlanMonth);
        }

        // 한 달씩 묻는다. 세 달을 다 고르면 몽타주로.
        private void AskPlanMonth()
        {
            var month = PlanMonths.Count;

            if (month >= StringConstants.TEXT_PLAN_MONTH_ASK.Length)
            {
                PlayPlanMontage();
                return;
            }

            var choices = new List<DialogueChoice>();

            for (var i = 0; i < StringConstants.TEXT_PLAN_TASK.Length; i++)
            {
                var task = (SeasonTask)i;
                var choice = new DialogueChoice(StringConstants.TEXT_PLAN_TASK[i], () =>
                {
                    PlanMonths.Add(task);
                    AskPlanMonth();
                });
                choice.Note = string.Format(StringConstants.TEXT_PLAN_TASK_NOTE[i], Constants.PLAN_INN_EARN);
                choices.Add(choice);
            }

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_PLAN_TAG,
                StringConstants.TEXT_PLAN_MONTH_ASK[month]);
            line.Choices = choices;
            Host.Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        private void PlayPlanMontage()
        {
            var texts = SeasonPlan.Apply(PlanMonths, HeroRoster.Favorite());
            var lines = new List<DialogueLine>();

            for (var i = 0; i < texts.Count; i++)
            {
                lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_PLAN_TAG, texts[i]));
            }

            Host.Dialogue.Play(lines, PlayExpeditionLetter);
        }

        /// <summary>
        /// 원정지에서 온 편지(설계안 §3). 가장 친한 용사가 충분히 가까우면 온다.
        /// 다음 계절에 드러날 방패 사정을 미리 흘리고, 원격 익명 의뢰로 먼저 막을 수 있게 한다 — 대신 더 비싸다.
        /// </summary>
        private void PlayExpeditionLetter()
        {
            var favorite = HeroRoster.Favorite();

            if (favorite == null || favorite.Affinity < Constants.HERO_LETTER_AFFINITY)
            {
                FinishDeparture();
                return;
            }

            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_LETTER_TAG,
                    string.Format(StringConstants.TEXT_LETTER_BODY, favorite.Name))
            };

            if (PatronageLedger.IsHidden(StringConstants.NEED_SHIELD_ID) == false)
            {
                Host.Dialogue.Play(lines, FinishDeparture);
                return;
            }

            var cost = Constants.REQUEST_COST_GEAR * Constants.REMOTE_REQUEST_MARKUP;
            var money = GatherSession.Available;
            var send = new DialogueChoice(StringConstants.TEXT_LETTER_REMOTE, () => SendRemoteShield(cost));
            send.IsEnabled = money >= cost;
            send.Note = string.Format(StringConstants.TEXT_LETTER_REMOTE_NOTE, cost, money);

            var shield = new DialogueLine(string.Empty, StringConstants.TEXT_LETTER_TAG, StringConstants.TEXT_LETTER_SHIELD);
            shield.Choices = new List<DialogueChoice>
            {
                send,
                new DialogueChoice(StringConstants.TEXT_LETTER_KEEP, FinishDeparture)
            };
            lines.Add(shield);
            Host.Dialogue.Play(lines, null);
        }

        private void SendRemoteShield(float cost)
        {
            GatherSession.Record(cost, 0f, null);
            PatronageLedger.RevealEarly(StringConstants.NEED_SHIELD_ID);
            PatronageLedger.Post(RequestPurpose.Purpose_Gear, Host.Today());

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_LETTER_TAG, StringConstants.TEXT_LETTER_SENT);
            Host.Dialogue.Play(new List<DialogueLine> { line }, FinishDeparture);
        }

        // 석 달이 흐르고 새 계절 첫날 아침이 된다. 귀환은 여관에서 맞는다.
        private void FinishDeparture()
        {
            SeasonPlan.MarkDeparted(MarketClock.SeasonOf(Host.Today()) - 1);
            isDepartureRunning = false;

            var minutes = WakeMinute() - Host.Clock.BreakMinuteOfDay;

            if (minutes > 0)
            {
                Host.Staging.PassTime(minutes, StringConstants.TEXT_DEPART_FADE);
            }
        }

        // ── 잠자리 (2026-10-17 다시 넣음) ───────
        // 오후장이 끝난 밤에만 잔다. 그날을 정리하고 주인공이 혼잣말을 한 뒤 아침 장 직전으로 넘어간다.
        // 날이 바뀌는 처리(기한·붕괴)는 시간이 흐르는 연출(PassTime)의 CheckDayTurn이 맡는다 — 두 번 세지 않는다.

        /// <summary>
        /// 잘 수 있는 밤인가. 자정이 지났고 아직 깰 시각 전이면 된다.
        /// 오후장을 거래소에서 마쳤든 영지에서 건너뛰었든 같다(2026-10-17 — 건너뛰면 잠자리가 안 떴다는 사용자 보고).
        /// </summary>
        public bool CanSleep()
        {
            if (Host.Clock == null || Host.Clock.IsBreak == false)
            {
                return false;
            }

            var minute = Host.Clock.BreakMinuteOfDay;
            return minute >= Constants.SLEEP_FROM_HOUR * 60 && WakeMinute() > minute;
        }

        /// <summary>
        /// 잠에서 깨는 시각. 다음 장 직전이되 늦어도 그날 아침 장 시각 직전이다.
        /// 계절 첫날은 장이 저녁에야 열리지만 낮에 귀환을 맞아야 한다 — 다음 장까지 자면 그 낮을 통째로 건너뛴다.
        /// </summary>
        private int WakeMinute()
        {
            var dayStart = Host.Clock.BreakMinuteOfDay / Constants.MINUTES_PER_DAY * Constants.MINUTES_PER_DAY;
            var dawn = dayStart + Constants.MARKET_MORNING_START_HOUR * 60 - Constants.BREAK_SKIP_MARGIN_MINUTES;
            return Mathf.Min(Host.Clock.LatestBreakMinute, dawn);
        }

        public DialogueChoice SleepChoice()
        {
            var choice = new DialogueChoice(StringConstants.TEXT_SLEEP_CHOICE, Sleep);
            choice.IsEnabled = CanSleep();
            choice.Note = choice.IsEnabled == true
                ? string.Format(StringConstants.TEXT_SLEEP_CHOICE_NOTE, ClockText.FromMinutes(WakeMinute()))
                : StringConstants.TEXT_SLEEP_LOCKED;
            return choice;
        }

        public void Sleep()
        {
            if (CanSleep() == false)
            {
                return;
            }

            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(InfoSource.Source_TavernTalk));
            // 오후장은 자정에 끝나므로 영지 날짜는 이미 다음 날이다. 「며칠째 밤」은 장의 날짜로 적는다.
            var today = Host.Today();
            var tag = string.Format(StringConstants.TEXT_SLEEP_TAG, ClockText.SeasonDay(Host.Clock.Day));
            var lines = new List<DialogueLine>
            {
                new DialogueLine(StringConstants.TEXT_SLEEP_SPEAKER, tag, SleepRecap(today)),
                new DialogueLine(string.Empty, tag, PickMonologue(today))
            };

            Host.Dialogue.Play(lines, () =>
            {
                var minutes = WakeMinute() - Host.Clock.BreakMinuteOfDay;
                Host.Staging.PassTime(minutes, StringConstants.TEXT_SLEEP_FADE);
            });
        }

        // 그날의 정리. 기한은 보이고 금액은 숨긴다(상장흐름 설계안 §1-5 완화책).
        private string SleepRecap(int today)
        {
            var sim = BreakClock.Simulation;
            var cash = sim == null ? GatherSession.Available : sim.Wallet.Cash + GatherSession.NetChange;
            var held = sim == null ? 0f : sim.Wallet.HoldingsValue(sim.StockLookup);
            var text = string.Format(StringConstants.TEXT_SLEEP_RECAP, cash, held, GatherSession.Gathered.Count);
            var deadline = PatronageLedger.NearestDeadline(today);

            if (deadline > 0)
            {
                text = text + "\n" + string.Format(StringConstants.TEXT_SLEEP_DEADLINE, deadline);
            }

            return text;
        }

        /// <summary>
        /// 이야기의 고비를 지난 뒤 한 번씩 나오는 혼잣말을 먼저, 없으면 평소의 밤 혼잣말을 고른다.
        /// 열쇠는 상수 이름이다(저장에 남는다).
        /// </summary>
        private static string PickMonologue(int today)
        {
            var sim = BreakClock.Simulation;
            var traded = sim != null && sim.Wallet.AllHoldings.Count > 0;
            var heardNeed = false;

            foreach (var need in PatronageLedger.All)
            {
                if (need.Heard == true && need.State == NeedState.State_Open)
                {
                    heardNeed = true;
                }
            }

            var healed = PatronageLedger.IsMet(StringConstants.NEED_HEALER_ID);
            var candidates = new List<KeyValuePair<string, bool>>
            {
                Monologue(nameof(StringConstants.TEXT_SLEEP_RETURN_SAFE), HeroRoster.ReturnDone == true && healed == true),
                Monologue(nameof(StringConstants.TEXT_SLEEP_RETURN_HURT), HeroRoster.ReturnDone == true && healed == false),
                Monologue(nameof(StringConstants.TEXT_SLEEP_SUSPECT),
                    HeroRoster.ReturnDone == true && PatronageLedger.Clues >= Constants.HERO_SUSPECT_CLUES),
                Monologue(nameof(StringConstants.TEXT_SLEEP_DAY_OFF), HeroRoster.DayOffDone),
                Monologue(nameof(StringConstants.TEXT_SLEEP_FIRST_REQUEST), PatronageLedger.RequestsPosted > 0),
                Monologue(nameof(StringConstants.TEXT_SLEEP_HEARD_NEED), heardNeed),
                Monologue(nameof(StringConstants.TEXT_SLEEP_FIRST_TRADE), traded),
                Monologue(nameof(StringConstants.TEXT_SLEEP_CELLAR), StoryProgress.CellarFound)
            };

            for (var i = 0; i < candidates.Count; i++)
            {
                var key = candidates[i].Key;

                if (candidates[i].Value == true && StoryProgress.SleepSeen.Contains(key) == false)
                {
                    StoryProgress.SleepSeen.Add(key);
                    return MonologueText(key);
                }
            }

            switch (today % 3)
            {
                case 0:
                {
                    return StringConstants.TEXT_SLEEP_PLAIN_1;
                }
                case 1:
                {
                    return StringConstants.TEXT_SLEEP_PLAIN_2;
                }
                default:
                {
                    return StringConstants.TEXT_SLEEP_PLAIN_3;
                }
            }
        }

        private static KeyValuePair<string, bool> Monologue(string key, bool ready)
        {
            return new KeyValuePair<string, bool>(key, ready);
        }

        // 열쇠(상수 이름)로 지금 문구를 읽는다. 문구 표가 덮어쓴 값을 써야 한다.
        private static string MonologueText(string key)
        {
            var field = typeof(StringConstants).GetField(key);
            return field == null ? string.Empty : (string)field.GetValue(null);
        }
    }
}
