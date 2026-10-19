using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 할 일(QuestLog) — 화면 왼쪽 위 목표 한 줄과, 새 할 일을 맡는 사람이 직접 부탁하는 장면.
    /// 영지 화면(<see cref="VillageScreenController"/>)이 조립한다(2026-10-19 정리 — 화면에서 떼어 냄).
    /// </summary>
    public sealed class VillageQuestHandler
    {
        private const string OBJECTIVE = "village-objective";

        private readonly VillageScreenController Host;
        private readonly VillagePrologueHandler Prologue;

        // 매 프레임 불린다 — 할 일·진행도가 바뀔 때만 글을 다시 만든다.
        private Label ObjectiveLabel;
        private Quest ObjectiveQuest;
        private int ObjectiveProgress = -1;
        private string ObjectiveText = string.Empty;

        public VillageQuestHandler(VillageScreenController host, VillagePrologueHandler prologue)
        {
            Host = host;
            Prologue = prologue;
        }

        /// <summary>
        /// 화면 왼쪽 위의 「할 일」. 지금 할 일과 진행도. 일곱째 날은 할 일 대신 프롤로그 목표다.
        /// 마치면 주인공이 기뻐하고 머리 위에 알린다. 보상이 있으면 영지에서 번 돈으로 들어간다.
        /// </summary>
        public void RefreshObjective()
        {
            var label = EnsureLabel();
            var goal = Prologue.GoalText();

            if (goal != null)
            {
                label.text = goal;
                label.EnableInClassList("hidden", false);
                return;
            }

            var done = QuestLog.Tick();

            if (done != null)
            {
                Celebrate(done);
            }

            // 받은 것과 진행도만 보인다(2026-10-17 사장님 결정). 아직 받지 않은 일은 보이지 않는다.
            var active = QuestLog.HasPendingIntro == true ? null : QuestLog.Active;
            var progress = -1;

            if (active != null && active.Goal > 0 && active.Progress != null)
            {
                progress = System.Math.Min(active.Progress(), active.Goal);
            }

            if (active != ObjectiveQuest || progress != ObjectiveProgress)
            {
                ObjectiveQuest = active;
                ObjectiveProgress = progress;
                var text = string.Empty;

                if (active != null)
                {
                    text = string.Format(StringConstants.TEXT_QUEST_LABEL, active.Title);

                    if (progress >= 0)
                    {
                        text += "\n<size=15>" + string.Format(StringConstants.TEXT_QUEST_PROGRESS, progress, active.Goal) + "</size>";
                    }
                }

                ObjectiveText = text;
            }

            if (label.text != ObjectiveText)
            {
                label.text = ObjectiveText;
            }

            label.EnableInClassList("hidden", active == null);
        }

        private Label EnsureLabel()
        {
            if (ObjectiveLabel != null)
            {
                return ObjectiveLabel;
            }

            ObjectiveLabel = Host.Root.Q<Label>(OBJECTIVE);

            if (ObjectiveLabel == null)
            {
                ObjectiveLabel = new Label();
                ObjectiveLabel.name = OBJECTIVE;
                ObjectiveLabel.AddToClassList(OBJECTIVE);
                ObjectiveLabel.pickingMode = PickingMode.Ignore;
                Host.Root.Add(ObjectiveLabel);
            }

            return ObjectiveLabel;
        }

        private void Celebrate(Quest done)
        {
            if (done.Reward > 0f)
            {
                GatherSession.Record(0f, done.Reward, null);
            }

            Host.SayToSelf(done.Reward > 0f
                ? string.Format(StringConstants.TEXT_QUEST_DONE_REWARD, done.Title, done.Reward)
                : string.Format(StringConstants.TEXT_QUEST_DONE, done.Title));
            Host.QueueReaction(ActorMotion.Motion_Cheer, ActorEmote.Emote_Music);
        }

        /// <summary>
        /// 새 할 일을 받는 장면. 맡기는 사람이 직접 부탁한다 — 그 말이 어디로 가야 하는지 알려 주는 유일한 길잡이다.
        /// 부르는 쪽이 대화·연출·튜토리얼이 없는지 먼저 본다.
        /// </summary>
        public void PlayIntro()
        {
            var quest = QuestLog.Active;
            QuestLog.MarkIntroduced();
            DialogueLine line;

            // 부탁은 맡기는 사람의 자리에서 받는다. 혼잣말과 용사단의 부탁은 여관이다.
            var place = InfoSource.Source_TavernTalk;

            switch (quest.Giver)
            {
                case QuestGiver.Giver_Innkeeper:
                {
                    line = GiverLine(InfoSource.Source_TavernTalk, quest.Ask);
                    break;
                }
                case QuestGiver.Giver_Archivist:
                {
                    line = GiverLine(InfoSource.Source_GuildRecord, quest.Ask);
                    place = InfoSource.Source_GuildRecord;
                    break;
                }
                case QuestGiver.Giver_Broker:
                {
                    line = GiverLine(InfoSource.Source_MarketBoard, quest.Ask);
                    place = InfoSource.Source_MarketBoard;
                    break;
                }
                case QuestGiver.Giver_Swordswoman:
                case QuestGiver.Giver_Scholar:
                case QuestGiver.Giver_Elf:
                {
                    // 부탁할 사람이 떠났으면 혼잣말로 받는다.
                    var hero = HeroRoster.All[HeroIndexOf(quest.Giver)];
                    line = hero.Condition == HeroCondition.Condition_Dead
                        ? new DialogueLine(string.Empty, StringConstants.TEXT_QUEST_SELF_TAG, quest.Ask)
                        : Host.HeroLine(hero, quest.Ask, PortraitEmotion.Emotion_Neutral);
                    break;
                }
                default:
                {
                    line = new DialogueLine(string.Empty, StringConstants.TEXT_QUEST_SELF_TAG, quest.Ask);
                    break;
                }
            }

            line.Note = string.Format(StringConstants.TEXT_QUEST_LABEL, quest.Title);
            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(place));
            Host.Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        private static int HeroIndexOf(QuestGiver giver)
        {
            switch (giver)
            {
                case QuestGiver.Giver_Scholar:
                {
                    return 0;
                }
                case QuestGiver.Giver_Elf:
                {
                    return 2;
                }
                default:
                {
                    return 1;
                }
            }
        }

        private DialogueLine GiverLine(InfoSource source, string text)
        {
            var line = new DialogueLine(InfoPeople.Speaker(source), StringConstants.TEXT_QUEST_ASK_TAG, text);
            line.Portrait = InfoPeople.Face(Host.DataSet, source, PortraitEmotion.Emotion_Neutral);
            return line;
        }
    }
}
