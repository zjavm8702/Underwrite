using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Save;
using Underwrite.Story;
using UnityEngine.SceneManagement;

namespace Underwrite.UI
{
    /// <summary>
    /// 여관의 용사단과 길드 창구 — 용사단 테이블·대화·쉬는 날·귀환·데모 마무리, 그리고 익명 의뢰.
    /// 기획서 §8의 두 관계(일상의 주인공 · 익명의 후원자)가 영지에서 만나는 자리다.
    /// 영지 화면(<see cref="VillageScreenController"/>)이 조립한다(2026-10-19 정리 — 화면에서 떼어 냄).
    /// </summary>
    public sealed class VillageHeroHandler
    {
        private readonly VillageScreenController Host;

        public VillageHeroHandler(VillageScreenController host)
        {
            Host = host;
        }

        // ── 용사단 (루틴 설계안) ──────────────────

        /// <summary>
        /// 여관의 용사단 테이블. 쉬는 날에는 하루를 함께 보내는 장면으로 바뀐다.
        /// 원정 중이면 잠그고 언제 돌아오는지 적는다.
        /// </summary>
        public DialogueChoice HeroTableChoice(VillageSpot spot)
        {
            var day = Host.Today();

            if (HeroRoster.HasDayOff(day) == true)
            {
                var dayOff = new DialogueChoice(StringConstants.TEXT_HERO_DAY_OFF, PlayDayOff);
                dayOff.Note = StringConstants.TEXT_HERO_DAY_OFF_NOTE;
                return dayOff;
            }

            // 용사단과의 대화는 이야기다 — 행동력이 들지 않는다(2026-10-19).
            var table = new DialogueChoice(StringConstants.TEXT_HERO_TABLE, () => OpenHeroTable(spot));
            table.Note = StringConstants.TEXT_HERO_TABLE_NOTE;
            return table;
        }

        private void OpenHeroTable(VillageSpot spot)
        {
            var day = Host.Today();
            var choices = new List<DialogueChoice>();
            var heroes = HeroRoster.All;

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                var choice = new DialogueChoice(hero.Name, () => TalkToHero(hero));

                // 떠난 사람의 자리도 테이블에 남겨 둔다. 비어 있는 것이 보여야 한다.
                if (hero.Condition == HeroCondition.Condition_Dead)
                {
                    choice.IsEnabled = false;
                    choice.Note = StringConstants.TEXT_HERO_GONE;
                    choices.Add(choice);
                    continue;
                }

                choice.IsEnabled = hero.LastTalkDay != day;
                choice.Note = choice.IsEnabled == true ? hero.Role : StringConstants.TEXT_HERO_TALKED_TODAY;
                choices.Add(choice);
            }

            choices.Add(new DialogueChoice(StringConstants.TEXT_REQUEST_CANCEL, () => Host.Ask(spot, true)));

            var line = new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_HERO_TABLE_ASK);
            line.Choices = choices;
            Host.Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        /// <summary>
        /// 일꾼으로서 말을 건다. 기획서 §8의 두 관계 가운데 **일상의 주인공** 쪽이다.
        /// 대답에 따라 호감이 오르고 표정이 바뀐다. 숫자는 보여 주지 않는다.
        /// </summary>
        private void TalkToHero(HeroProfile hero)
        {
            hero.LastTalkDay = Host.Today();

            var topic = HeroTopics.Next(hero);

            if (topic != null)
            {
                TalkAbout(hero, topic);
                return;
            }

            var greet = Host.HeroLine(hero, StringConstants.HERO_GREET[hero.FaceIndex], PortraitEmotion.Emotion_Neutral);
            greet.Choices = new List<DialogueChoice>
            {
                new DialogueChoice(StringConstants.TEXT_HERO_REPLY_WARM, () => ReactToHero(hero, 2,
                    StringConstants.HERO_REACT_WARM[hero.FaceIndex], PortraitEmotion.Emotion_Relieved)),
                new DialogueChoice(StringConstants.TEXT_HERO_REPLY_CURIOUS, () => ReactToHero(hero, 1,
                    StringConstants.HERO_REACT_CURIOUS[hero.FaceIndex], PortraitEmotion.Emotion_Anxious)),
                new DialogueChoice(StringConstants.TEXT_HERO_REPLY_PLAIN, () => ReactToHero(hero, 0,
                    StringConstants.HERO_REACT_PLAIN[hero.FaceIndex], PortraitEmotion.Emotion_Neutral))
            };

            Host.Dialogue.Play(new List<DialogueLine> { greet }, null);
        }

        /// <summary>
        /// 용사가 이야기를 꺼낸다. 주인공은 대답을 고른다. 반응부터는 평소 대화와 같다(호감·몸짓·현장 이야기).
        /// </summary>
        private void TalkAbout(HeroProfile hero, HeroTopic topic)
        {
            hero.TopicsSeen.Add(topic.Id);

            var opening = Host.HeroLine(hero, topic.Opening, topic.OpeningEmotion);
            opening.Choices = new List<DialogueChoice>();

            for (var i = 0; i < topic.Replies.Length; i++)
            {
                var reply = topic.Replies[i];
                opening.Choices.Add(new DialogueChoice(reply.Text,
                    () => ReactToHero(hero, reply.Affinity, reply.Answer, reply.AnswerEmotion)));
            }

            Host.Dialogue.Play(new List<DialogueLine> { opening }, null);
        }

        /// <summary>
        /// 대답에 대한 반응. 충분히 친하면 다른 용병단의 현장 이야기를 덤으로 흘린다.
        /// 사랑하는 사람들과 가까워질수록 모르는 사람들에 대한 정보가 생긴다(루틴 설계안 — 관계와 투자의 다리).
        /// 다만 덤일 뿐이다. 용사를 정보 도구로 만들지 않도록 확률로만 나온다.
        /// </summary>
        private void ReactToHero(HeroProfile hero, int affinity, string text, PortraitEmotion emotion)
        {
            hero.Affinity += affinity;

            var lines = new List<DialogueLine> { Host.HeroLine(hero, text, emotion) };

            if (hero.Affinity >= Constants.HERO_FIELD_TALK_AFFINITY
                && UnityEngine.Random.value < Constants.HERO_FIELD_TALK_CHANCE)
            {
                var rows = Host.NextSlotRows();

                if (rows.Count > 0)
                {
                    var pick = rows[UnityEngine.Random.Range(0, rows.Count)];
                    var name = Host.StockName(pick.StockId);
                    var field = Host.HeroLine(hero, string.Format(StringConstants.TEXT_HERO_FIELD_TALK, name, pick.Headline),
                        PortraitEmotion.Emotion_Neutral);
                    field.IsSecret = true;
                    var heldTag = VillageScreenController.HeldNote(pick.StockId);

                    field.Badge = heldTag;
                    lines.Add(field);
                    GatherSession.Record(0f, 0f, new GatheredNote(pick.StockId, pick.Headline,
                        InfoSource.Source_TavernTalk));
                }
            }

            // 이야기를 마치면 테이블로 돌아간다. 다른 사람에게도 말을 걸 수 있다.
            System.Action back = () =>
            {
                if (Host.CurrentSpot != null)
                {
                    OpenHeroTable(Host.CurrentSpot);
                }
            };

            Host.Dialogue.Play(lines, back);
        }

        /// <summary>
        /// 용사단이 쉬는 날(계절의 셋째 날). 큰 관계 장면의 날이다(루틴 설계안 — 일주일의 리듬).
        /// 가장 친한 사람이 마지막 한마디를 한다. 이번 휴장의 남은 행동력을 함께 보내는 데 쓴다.
        /// </summary>
        private void PlayDayOff()
        {
            var day = Host.Today();
            HeroRoster.MarkDayOff(day);
            HeroRoster.AddAffinityToAll(1);

            var favorite = HeroRoster.Favorite();
            var sword = HeroRoster.All[1].Condition == HeroCondition.Condition_Dead ? favorite : HeroRoster.All[1];
            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_DAY_OFF_TAG, StringConstants.TEXT_DAY_OFF_OPEN),
                Host.HeroLine(sword, StringConstants.TEXT_DAY_OFF_INVITE, PortraitEmotion.Emotion_Neutral),
                Host.HeroLine(favorite, StringConstants.HERO_DAY_OFF_FAVORITE[favorite.FaceIndex],
                    PortraitEmotion.Emotion_Happy),
                new DialogueLine(string.Empty, StringConstants.TEXT_DAY_OFF_TAG, StringConstants.TEXT_DAY_OFF_CLOSE)
            };

            // 시계는 턴제라 휴장을 넘기면 장 직전에 설 뿐이다. 대신 남은 행동력을 다 쓴다.
            ActionPoints.Spend(ActionPoints.Current);

            Host.Dialogue.Play(lines, Host.Back(Host.CurrentSpot));
        }

        /// <summary>
        /// 원정에서 돌아온 날. 후원이 닿았는지가 그들의 몸으로 돌아온다(루틴 설계안 — 한 주기의 리듬).
        /// 의뢰가 두 번 이상 맞았으면 학자가 의뢰주를 의심하기 시작한다(후원흐름 설계안 §2-6).
        /// </summary>
        public void PlayReturn(System.Action onDone)
        {
            var day = Host.Today();

            // 데모의 마지막 귀환이면 귀환 장면 뒤에 마무리를 붙인다.
            if (StoryProgress.DemoEndSeen == false && MarketClock.SeasonOf(day) >= Constants.DEMO_END_SEASON)
            {
                var after = onDone;
                onDone = () => PlayDemoEnd(after);
            }

            HeroRoster.MarkReturn(day);
            var heroes = HeroRoster.All;
            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_EXPEDITION_TAG, StringConstants.TEXT_RETURN_OPEN)
            };

            // 지난 계절에 놓친 사정의 대가. 몸 상태가 바뀐 날이 지난 계절 안이면 이번 원정에서 일어난 일이다.
            var since = MarketClock.SeasonStartDay(MarketClock.SeasonOf(day) - 1);
            var struck = false;

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];

                if (hero.ConditionDay < since || hero.ConditionDay >= day)
                {
                    continue;
                }

                if (hero.Condition == HeroCondition.Condition_Dead)
                {
                    if (struck == false)
                    {
                        lines[0].Text = StringConstants.TEXT_RETURN_DEATH_OPEN;
                    }

                    lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_EXPEDITION_TAG,
                        string.Format(StringConstants.TEXT_RETURN_DEATH_NAME, hero.Name)));
                    var mourner = HeroRoster.Favorite();

                    if (mourner != null)
                    {
                        lines.Add(Host.HeroLine(mourner, StringConstants.TEXT_RETURN_DEATH_LINE, PortraitEmotion.Emotion_Sad));
                    }

                    struck = true;
                }
                else if (hero.Condition == HeroCondition.Condition_Wounded)
                {
                    lines.Add(Host.HeroLine(hero, StringConstants.HERO_RETURN_WOUNDED[i], PortraitEmotion.Emotion_Sad));
                    struck = true;
                }
            }

            if (struck == false)
            {
                var warrior = heroes[Constants.HERO_INDEX_WARRIOR];
                var speaker = warrior.Condition == HeroCondition.Condition_Dead ? HeroRoster.Favorite() : warrior;

                if (speaker != null)
                {
                    lines.Add(Host.HeroLine(speaker, StringConstants.TEXT_RETURN_GOOD, PortraitEmotion.Emotion_Happy));
                }
            }

            if (heroes[0].Condition != HeroCondition.Condition_Dead
                && PatronageLedger.Clues >= Constants.HERO_SUSPECT_CLUES)
            {
                lines.Add(Host.HeroLine(heroes[0], StringConstants.TEXT_RETURN_SUSPECT, PortraitEmotion.Emotion_Shocked));
            }

            Host.Dialogue.Play(lines, onDone);
        }

        /// <summary>
        /// 데모 마무리(2026-10-18). 두 계절 동안 무엇을 지켰고 무엇을 잃었는지 숫자로 한 번 보여 준다.
        /// 「조금 더 둘러본다」면 판은 그대로 이어진다 — 저장도 지우지 않는다.
        /// </summary>
        private void PlayDemoEnd(System.Action onStay)
        {
            StoryProgress.DemoEndSeen = true;

            var healthy = 0;
            var wounded = 0;
            var dead = 0;
            var heroes = HeroRoster.All;

            for (var i = 0; i < heroes.Count; i++)
            {
                switch (heroes[i].Condition)
                {
                    case HeroCondition.Condition_Dead:
                    {
                        dead++;
                        break;
                    }
                    case HeroCondition.Condition_Wounded:
                    {
                        wounded++;
                        break;
                    }
                    default:
                    {
                        healthy++;
                        break;
                    }
                }
            }

            var sim = BreakClock.Simulation;
            var worth = (sim == null ? 0f : sim.Wallet.TotalValue(sim.StockLookup)) + GatherSession.NetChange;
            var last = new DialogueLine(string.Empty, StringConstants.TEXT_DEMO_TAG, StringConstants.TEXT_DEMO_END_2);
            last.Choices = new List<DialogueChoice>
            {
                new DialogueChoice(StringConstants.TEXT_DEMO_TO_TITLE, () =>
                {
                    SaveSystem.Save();
                    SceneManager.LoadScene(SceneRoutes.INTRO);
                }),
                new DialogueChoice(StringConstants.TEXT_DEMO_STAY, onStay)
            };

            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(InfoSource.Source_TavernTalk));
            Host.Dialogue.Play(new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_DEMO_TAG, StringConstants.TEXT_DEMO_END_1),
                new DialogueLine(string.Empty, StringConstants.TEXT_DEMO_TAG, string.Format(
                    StringConstants.TEXT_DEMO_END_SUMMARY, worth, healthy, wounded, dead, SeasonPlan.OmenClues)),
                last
            }, null);
        }

        /// <summary>길드 창구의 「익명 의뢰를 낸다」. 들은 사정이 있으면 기한을 붙여 보여 준다.</summary>
        public DialogueChoice PostChoice(VillageSpot spot)
        {
            // 후원은 이 게임의 목적이다 — 행동력으로 막지 않는다. 돈은 든다(2026-10-19).
            var post = new DialogueChoice(StringConstants.TEXT_REQUEST_POST, () => OpenRequestForm(spot));

            var deadline = PatronageLedger.NearestDeadline(Host.Today());
            post.Note = deadline > 0
                ? string.Format(StringConstants.TEXT_REQUEST_POST_DEADLINE, deadline)
                : StringConstants.TEXT_REQUEST_POST_NOTE;
            return post;
        }

        /// <summary>
        /// 길드 창구에서 익명 의뢰를 꾸민다. 후원흐름 설계안 §3-2.
        /// 지금은 "무엇을 위한 의뢰인가" 하나만 고른다. 나머지 항목(선지급 물자, 보수, 위험도)은 하나씩 연다.
        /// 고르는 근거는 여관에서 엿들은 말뿐이다. 무엇이 필요한지는 화면에 적지 않는다.
        /// </summary>
        private void OpenRequestForm(VillageSpot spot)
        {
            var money = GatherSession.Available;
            var choices = new List<DialogueChoice>
            {
                RequestChoice(RequestPurpose.Purpose_Healing, StringConstants.TEXT_REQUEST_PURPOSE_HEALING,
                    Constants.REQUEST_COST_HEALING, money),
                RequestChoice(RequestPurpose.Purpose_Gear, StringConstants.TEXT_REQUEST_PURPOSE_GEAR,
                    Constants.REQUEST_COST_GEAR, money),
                RequestChoice(RequestPurpose.Purpose_Supplies, StringConstants.TEXT_REQUEST_PURPOSE_SUPPLIES,
                    Constants.REQUEST_COST_SUPPLIES, money),
                RequestChoice(RequestPurpose.Purpose_Training, StringConstants.TEXT_REQUEST_PURPOSE_TRAINING,
                    Constants.REQUEST_COST_TRAINING, money),
                new DialogueChoice(StringConstants.TEXT_REQUEST_CANCEL, () => Host.Ask(spot, true))
            };

            var line = new DialogueLine(StringConstants.TEXT_REQUEST_CLERK, spot.Name, StringConstants.TEXT_REQUEST_ASK);
            line.Choices = choices;
            Host.Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        private DialogueChoice RequestChoice(RequestPurpose purpose, string text, float cost, float money)
        {
            var choice = new DialogueChoice(text, () => PostRequest(purpose, cost));
            choice.IsEnabled = money >= cost;
            choice.Note = choice.IsEnabled == true
                ? string.Format(StringConstants.TEXT_REQUEST_COST_NOTE, cost, money)
                : string.Format(StringConstants.TEXT_GATHER_NO_MONEY, money);
            return choice;
        }

        /// <summary>
        /// 의뢰를 맡긴다. 돈은 영지에서 쓴 돈으로 적혀 거래 화면으로 돌아갈 때 빠진다.
        /// 맞았는지는 여기서 알려 주지 않는다. 다음에 여관에서 일할 때 그들의 말로 듣는다.
        /// </summary>
        private void PostRequest(RequestPurpose purpose, float cost)
        {
            GatherSession.Record(cost, 0f, null);
            PatronageLedger.Post(purpose, Host.Today());

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_REQUEST_CLERK,
                string.Format(StringConstants.TEXT_REQUEST_DONE, StringConstants.TEXT_HERO_PARTY));
            line.Note = string.Format(StringConstants.TEXT_REQUEST_DONE_NOTE, cost);
            Host.Dialogue.Play(new List<DialogueLine> { line },
                Host.Back(Host.CurrentSpot));
        }
    }
}
