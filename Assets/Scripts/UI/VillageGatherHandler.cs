using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using UnityEngine;

namespace Underwrite.UI
{
    /// <summary>
    /// 돈 내고 묻기 — 자리의 주인에게 값을 치르고 다음 장의 소식을 미리 듣는다(기획서 §11의 아래층).
    /// 값이 비쌀수록 또렷하다. 덤으로 그 자리에서 모험단 하나를 알게 된다(도감).
    /// 영지 화면(<see cref="VillageScreenController"/>)이 조립한다(2026-10-19 정리 — 화면에서 떼어 냄).
    /// </summary>
    public sealed class VillageGatherHandler
    {
        private readonly VillageScreenController Host;

        public VillageGatherHandler(VillageScreenController host)
        {
            Host = host;
        }

        /// <summary>
        /// 자리에 맞는 정보를 캔다.
        ///
        /// 값이 비쌀수록 또렷하다. 여관은 누구 이야기가 돌지만 알려 주고,
        /// 길드 기록은 내용까지 알려 준다. 게시판은 공짜지만 이미 다 아는 것만 있다.
        /// </summary>
        public void Gather(VillageSpot spot)
        {
            // 정보를 캐는 곳만 있는 것이 아니다. 지하시장 입구는 걸어 들어가는 문이다.
            if (spot.Kind == VillageActivity.Activity_Enter)
            {
                Host.GoToMarket();
                return;
            }

            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(spot.InfoSource));

            if (HasTime() == false)
            {
                Say(spot, InfoPeople.Pick(spot.InfoSource, StringConstants.TEXT_GATHER_TOO_LATE_BY), false);
                return;
            }

            var rows = Host.NextSlotRows();

            if (rows.Count == 0)
            {
                SpendTime(spot);
                GatherSession.Record(spot.Price, 0f, null);
                Say(spot, InfoPeople.Pick(spot.InfoSource, StringConstants.TEXT_GATHER_NOTHING_BY), false, null, ScoutBand(spot));
                return;
            }

            var pick = rows[Random.Range(0, rows.Count)];
            var name = Host.StockName(pick.StockId);

            string title;
            string body;
            GatheredNote note;

            switch (spot.InfoSource)
            {
                case InfoSource.Source_GuildRecord:
                {
                    title = StringConstants.TEXT_GATHER_GUILD_TITLE;
                    body = pick.Headline;
                    note = new GatheredNote(pick.StockId, pick.Headline, spot.InfoSource);
                    break;
                }
                case InfoSource.Source_MarketBoard:
                {
                    title = StringConstants.TEXT_GATHER_BOARD_TITLE;
                    body = StringConstants.TEXT_GATHER_BOARD_BODY;
                    note = null;
                    break;
                }
                case InfoSource.Source_SmithOrder:
                {
                    // 대장간은 내용이 아니라 **움직임의 크기**를 알려 준다.
                    // 누가 크게 준비하는지는 알지만 왜인지는 모른다.
                    title = StringConstants.TEXT_GATHER_SMITH_TITLE;
                    body = string.Format(StringConstants.TEXT_GATHER_SMITH_BODY, name);
                    note = new GatheredNote(pick.StockId, string.Format(StringConstants.TEXT_GATHER_SMITH_NOTE, name), spot.InfoSource);
                    break;
                }
                case InfoSource.Source_PrivateLetter:
                {
                    // 가장 비싼 만큼 가장 또렷하다. 소식의 내용이 그대로 나온다.
                    title = StringConstants.TEXT_GATHER_LETTER_TITLE;
                    body = pick.Headline;
                    note = new GatheredNote(pick.StockId, pick.Headline, spot.InfoSource);
                    break;
                }
                default:
                {
                    title = StringConstants.TEXT_GATHER_TAVERN_TITLE;
                    body = string.Format(StringConstants.TEXT_GATHER_TAVERN_BODY, name);
                    note = new GatheredNote(pick.StockId, string.Format(StringConstants.TEXT_GATHER_VAGUE_NOTE, name), spot.InfoSource);
                    break;
                }
            }

            SpendTime(spot);
            GatherSession.Record(spot.Price, 0f, note);
            Say(spot, body, note != null, VillageScreenController.HeldNote(pick.StockId), ScoutBand(spot));
        }

        /// <summary>
        /// 그 자리에서 모험단 하나에 대해 알게 된다(도감). 갓 도착한 단이 먼저다 — 상장 전 조사.
        /// 새로 알았으면 대화에 붙일 한 줄, 없으면 null.
        /// </summary>
        private static string ScoutBand(VillageSpot spot)
        {
            CodexSource source;

            switch (spot.InfoSource)
            {
                case InfoSource.Source_GuildRecord:
                {
                    source = CodexSource.Codex_Guild;
                    break;
                }
                case InfoSource.Source_SmithOrder:
                {
                    source = CodexSource.Codex_Smith;
                    break;
                }
                case InfoSource.Source_TavernTalk:
                {
                    source = CodexSource.Codex_Tavern;
                    break;
                }
                default:
                {
                    return null;
                }
            }

            var sim = BreakClock.Simulation;
            var id = BandCodex.PickUnknown(sim, source);
            var fact = id == null ? null : BandCodex.Learn(id, source);

            if (fact == null)
            {
                return null;
            }

            return string.Format(InfoPeople.Pick(spot.InfoSource, StringConstants.TEXT_CODEX_LEARNED_BY),
                string.Format(StringConstants.TEXT_CODEX_LEARNED_LINE, sim.NameOf(id), fact));
        }

        /// <summary>
        /// 자리의 주인이 말한다. 묻는 쪽은 플레이어이므로 말하는 이는 늘 그 자리의 사람이다.
        /// </summary>
        private void Say(VillageSpot spot, string text, bool secret, string heldNote = null, string scouted = null)
        {
            var line = Host.OwnerLineFor(spot, text);
            line.IsSecret = secret;
            // 가진 종목의 소문이면 이름 옆에 붉은 배지를 붙인다. 작은 글씨 안내로는 놓친다(2026-10-17 사용자 요청).
            line.Badge = heldNote;
            line.Note = BuildNote(spot, secret);

            var lines = new List<DialogueLine> { line };

            if (scouted != null)
            {
                lines.Add(Host.OwnerLineFor(spot, scouted));
            }

            // 대화창을 닫고 필드에서 건네는 장면은 뺐다 — 마을이 한 번 비쳤다가 다시 대화창이 떴다(2026-10-19 사장님).
            Host.Dialogue.Play(lines, Host.Back(spot));
        }

        /// <summary>돈 내고 묻기에 쓸 행동력이 남아 있는가.</summary>
        public bool HasTime()
        {
            return ActionPoints.Has(Constants.AP_GATHER);
        }

        /// <summary>돈 내고 묻기 — 행동력을 쓴다(돈은 부른 쪽이 기록한다).</summary>
        private void SpendTime(VillageSpot spot)
        {
            ActionPoints.Spend(Constants.AP_GATHER);
        }

        private string BuildNote(VillageSpot spot, bool secret)
        {
            var spent = spot.Price > 0f ? string.Format(StringConstants.TEXT_GATHER_SPENT, spot.Price) : string.Empty;
            var left = string.Format(StringConstants.TEXT_GATHER_TIME_LEFT, ActionPoints.Current);

            if (secret == false)
            {
                return spent + left;
            }

            return spent + StringConstants.TEXT_GATHER_SECRET + left;
        }
    }
}
