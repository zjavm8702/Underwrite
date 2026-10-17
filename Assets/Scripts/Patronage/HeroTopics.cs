using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Patronage
{
    /// <summary>
    /// 용사와 나누는 이야기 주제를 짠다(2026-10-17, 용사 대화 늘리기).
    ///
    /// 사람마다 셋이 호감에 따라 차례로 열리고, 몇몇은 일이 있은 뒤에만 나온다(시찰일·귀환·의심).
    /// 한 번 나눈 주제는 다시 나오지 않는다. 다 나누면 평소의 인사로 돌아간다.
    /// 문구는 부를 때마다 상수에서 읽는다. 표가 덮어쓴 뒤의 값을 써야 하기 때문이다.
    /// </summary>
    public static class HeroTopics
    {
        /// <summary>
        /// 지금 그 사람이 꺼낼 주제. 일이 있은 뒤의 주제를 먼저, 그다음 호감으로 열린 것 중 가장 앞의 것. 없으면 null.
        /// </summary>
        public static HeroTopic Next(HeroProfile hero)
        {
            var topics = Build();
            HeroTopic ready = null;

            for (var i = 0; i < topics.Count; i++)
            {
                var topic = topics[i];

                if (topic.HeroIndex != hero.FaceIndex || hero.TopicsSeen.Contains(topic.Id) == true
                    || hero.Affinity < topic.MinAffinity || IsOpen(topic.Gate) == false)
                {
                    continue;
                }

                if (topic.Gate != TopicGate.Gate_None)
                {
                    return topic;
                }

                if (ready == null)
                {
                    ready = topic;
                }
            }

            return ready;
        }

        private static bool IsOpen(TopicGate gate)
        {
            if (gate == TopicGate.Gate_DayOff)
            {
                return HeroRoster.DayOffDone == true;
            }

            if (gate == TopicGate.Gate_Healed)
            {
                return HeroRoster.ReturnDone == true && PatronageLedger.IsMet(StringConstants.NEED_HEALER_ID) == true;
            }

            if (gate == TopicGate.Gate_Suspect)
            {
                return HeroRoster.ReturnDone == true && PatronageLedger.Clues >= Constants.HERO_SUSPECT_CLUES;
            }

            return true;
        }

        private static HeroTopic Topic(string id, int heroIndex, int minAffinity, TopicGate gate, PortraitEmotion emotion,
            string opening, params HeroTopicReply[] replies)
        {
            return new HeroTopic
            {
                Id = id,
                HeroIndex = heroIndex,
                MinAffinity = minAffinity,
                Gate = gate,
                OpeningEmotion = emotion,
                Opening = opening,
                Replies = replies
            };
        }

        private static HeroTopicReply Reply(string text, string answer, PortraitEmotion emotion, int affinity)
        {
            return new HeroTopicReply
            {
                Text = text,
                Answer = answer,
                AnswerEmotion = emotion,
                Affinity = affinity
            };
        }

        // 순서가 곧 우선순위다. 사람마다 호감 0 → 3 → 6 순으로 적는다.
        private static List<HeroTopic> Build()
        {
            var topics = new List<HeroTopic>();
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_SCHOLAR_NOTES_OPEN), 0, Constants.HERO_TOPIC_AFFINITY_FIRST, TopicGate.Gate_None, PortraitEmotion.Emotion_Neutral, StringConstants.TEXT_TOPIC_SCHOLAR_NOTES_OPEN,
                Reply(StringConstants.TEXT_TOPIC_SCHOLAR_NOTES_REPLY_1, StringConstants.TEXT_TOPIC_SCHOLAR_NOTES_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_SCHOLAR_NOTES_REPLY_2, StringConstants.TEXT_TOPIC_SCHOLAR_NOTES_ANSWER_2, PortraitEmotion.Emotion_Happy, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_SCHOLAR_LIBRARY_OPEN), 0, Constants.HERO_TOPIC_AFFINITY_SECOND, TopicGate.Gate_None, PortraitEmotion.Emotion_Neutral, StringConstants.TEXT_TOPIC_SCHOLAR_LIBRARY_OPEN,
                Reply(StringConstants.TEXT_TOPIC_SCHOLAR_LIBRARY_REPLY_1, StringConstants.TEXT_TOPIC_SCHOLAR_LIBRARY_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_SCHOLAR_LIBRARY_REPLY_2, StringConstants.TEXT_TOPIC_SCHOLAR_LIBRARY_ANSWER_2, PortraitEmotion.Emotion_Sad, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_SCHOLAR_RECKON_OPEN), 0, Constants.HERO_TOPIC_AFFINITY_THIRD, TopicGate.Gate_None, PortraitEmotion.Emotion_Anxious, StringConstants.TEXT_TOPIC_SCHOLAR_RECKON_OPEN,
                Reply(StringConstants.TEXT_TOPIC_SCHOLAR_RECKON_REPLY_1, StringConstants.TEXT_TOPIC_SCHOLAR_RECKON_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_SCHOLAR_RECKON_REPLY_2, StringConstants.TEXT_TOPIC_SCHOLAR_RECKON_ANSWER_2, PortraitEmotion.Emotion_Shocked, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_SCHOLAR_SUSPECT_OPEN), 0, Constants.HERO_TOPIC_AFFINITY_FIRST, TopicGate.Gate_Suspect, PortraitEmotion.Emotion_Anxious, StringConstants.TEXT_TOPIC_SCHOLAR_SUSPECT_OPEN,
                Reply(StringConstants.TEXT_TOPIC_SCHOLAR_SUSPECT_REPLY_1, StringConstants.TEXT_TOPIC_SCHOLAR_SUSPECT_ANSWER_1, PortraitEmotion.Emotion_Neutral, 2),
                Reply(StringConstants.TEXT_TOPIC_SCHOLAR_SUSPECT_REPLY_2, StringConstants.TEXT_TOPIC_SCHOLAR_SUSPECT_ANSWER_2, PortraitEmotion.Emotion_Anxious, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_SWORD_BLADE_OPEN), 1, Constants.HERO_TOPIC_AFFINITY_FIRST, TopicGate.Gate_None, PortraitEmotion.Emotion_Neutral, StringConstants.TEXT_TOPIC_SWORD_BLADE_OPEN,
                Reply(StringConstants.TEXT_TOPIC_SWORD_BLADE_REPLY_1, StringConstants.TEXT_TOPIC_SWORD_BLADE_ANSWER_1, PortraitEmotion.Emotion_Anxious, 2),
                Reply(StringConstants.TEXT_TOPIC_SWORD_BLADE_REPLY_2, StringConstants.TEXT_TOPIC_SWORD_BLADE_ANSWER_2, PortraitEmotion.Emotion_Neutral, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_SWORD_VANGUARD_OPEN), 1, Constants.HERO_TOPIC_AFFINITY_SECOND, TopicGate.Gate_None, PortraitEmotion.Emotion_Neutral, StringConstants.TEXT_TOPIC_SWORD_VANGUARD_OPEN,
                Reply(StringConstants.TEXT_TOPIC_SWORD_VANGUARD_REPLY_1, StringConstants.TEXT_TOPIC_SWORD_VANGUARD_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_SWORD_VANGUARD_REPLY_2, StringConstants.TEXT_TOPIC_SWORD_VANGUARD_ANSWER_2, PortraitEmotion.Emotion_Sad, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_SWORD_SISTER_OPEN), 1, Constants.HERO_TOPIC_AFFINITY_THIRD, TopicGate.Gate_None, PortraitEmotion.Emotion_Sad, StringConstants.TEXT_TOPIC_SWORD_SISTER_OPEN,
                Reply(StringConstants.TEXT_TOPIC_SWORD_SISTER_REPLY_1, StringConstants.TEXT_TOPIC_SWORD_SISTER_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_SWORD_SISTER_REPLY_2, StringConstants.TEXT_TOPIC_SWORD_SISTER_ANSWER_2, PortraitEmotion.Emotion_Angry, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_SWORD_AFTER_DAYOFF_OPEN), 1, Constants.HERO_TOPIC_AFFINITY_FIRST, TopicGate.Gate_DayOff, PortraitEmotion.Emotion_Anxious, StringConstants.TEXT_TOPIC_SWORD_AFTER_DAYOFF_OPEN,
                Reply(StringConstants.TEXT_TOPIC_SWORD_AFTER_DAYOFF_REPLY_1, StringConstants.TEXT_TOPIC_SWORD_AFTER_DAYOFF_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_SWORD_AFTER_DAYOFF_REPLY_2, StringConstants.TEXT_TOPIC_SWORD_AFTER_DAYOFF_ANSWER_2, PortraitEmotion.Emotion_Angry, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_ELF_SPARK_OPEN), 2, Constants.HERO_TOPIC_AFFINITY_FIRST, TopicGate.Gate_None, PortraitEmotion.Emotion_Happy, StringConstants.TEXT_TOPIC_ELF_SPARK_OPEN,
                Reply(StringConstants.TEXT_TOPIC_ELF_SPARK_REPLY_1, StringConstants.TEXT_TOPIC_ELF_SPARK_ANSWER_1, PortraitEmotion.Emotion_Happy, 2),
                Reply(StringConstants.TEXT_TOPIC_ELF_SPARK_REPLY_2, StringConstants.TEXT_TOPIC_ELF_SPARK_ANSWER_2, PortraitEmotion.Emotion_Neutral, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_ELF_YEARS_OPEN), 2, Constants.HERO_TOPIC_AFFINITY_SECOND, TopicGate.Gate_None, PortraitEmotion.Emotion_Sad, StringConstants.TEXT_TOPIC_ELF_YEARS_OPEN,
                Reply(StringConstants.TEXT_TOPIC_ELF_YEARS_REPLY_1, StringConstants.TEXT_TOPIC_ELF_YEARS_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_ELF_YEARS_REPLY_2, StringConstants.TEXT_TOPIC_ELF_YEARS_ANSWER_2, PortraitEmotion.Emotion_Anxious, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_ELF_OMEN_OPEN), 2, Constants.HERO_TOPIC_AFFINITY_THIRD, TopicGate.Gate_None, PortraitEmotion.Emotion_Shocked, StringConstants.TEXT_TOPIC_ELF_OMEN_OPEN,
                Reply(StringConstants.TEXT_TOPIC_ELF_OMEN_REPLY_1, StringConstants.TEXT_TOPIC_ELF_OMEN_ANSWER_1, PortraitEmotion.Emotion_Anxious, 2),
                Reply(StringConstants.TEXT_TOPIC_ELF_OMEN_REPLY_2, StringConstants.TEXT_TOPIC_ELF_OMEN_ANSWER_2, PortraitEmotion.Emotion_Neutral, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_WARRIOR_SHIELD_OPEN), 3, Constants.HERO_TOPIC_AFFINITY_FIRST, TopicGate.Gate_None, PortraitEmotion.Emotion_Happy, StringConstants.TEXT_TOPIC_WARRIOR_SHIELD_OPEN,
                Reply(StringConstants.TEXT_TOPIC_WARRIOR_SHIELD_REPLY_1, StringConstants.TEXT_TOPIC_WARRIOR_SHIELD_ANSWER_1, PortraitEmotion.Emotion_Happy, 2),
                Reply(StringConstants.TEXT_TOPIC_WARRIOR_SHIELD_REPLY_2, StringConstants.TEXT_TOPIC_WARRIOR_SHIELD_ANSWER_2, PortraitEmotion.Emotion_Neutral, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_WARRIOR_COUNT_OPEN), 3, Constants.HERO_TOPIC_AFFINITY_SECOND, TopicGate.Gate_None, PortraitEmotion.Emotion_Neutral, StringConstants.TEXT_TOPIC_WARRIOR_COUNT_OPEN,
                Reply(StringConstants.TEXT_TOPIC_WARRIOR_COUNT_REPLY_1, StringConstants.TEXT_TOPIC_WARRIOR_COUNT_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_WARRIOR_COUNT_REPLY_2, StringConstants.TEXT_TOPIC_WARRIOR_COUNT_ANSWER_2, PortraitEmotion.Emotion_Sad, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_WARRIOR_FARM_OPEN), 3, Constants.HERO_TOPIC_AFFINITY_THIRD, TopicGate.Gate_None, PortraitEmotion.Emotion_Sad, StringConstants.TEXT_TOPIC_WARRIOR_FARM_OPEN,
                Reply(StringConstants.TEXT_TOPIC_WARRIOR_FARM_REPLY_1, StringConstants.TEXT_TOPIC_WARRIOR_FARM_ANSWER_1, PortraitEmotion.Emotion_Happy, 2),
                Reply(StringConstants.TEXT_TOPIC_WARRIOR_FARM_REPLY_2, StringConstants.TEXT_TOPIC_WARRIOR_FARM_ANSWER_2, PortraitEmotion.Emotion_Neutral, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_FAIRY_HERBS_OPEN), 4, Constants.HERO_TOPIC_AFFINITY_FIRST, TopicGate.Gate_None, PortraitEmotion.Emotion_Happy, StringConstants.TEXT_TOPIC_FAIRY_HERBS_OPEN,
                Reply(StringConstants.TEXT_TOPIC_FAIRY_HERBS_REPLY_1, StringConstants.TEXT_TOPIC_FAIRY_HERBS_ANSWER_1, PortraitEmotion.Emotion_Happy, 2),
                Reply(StringConstants.TEXT_TOPIC_FAIRY_HERBS_REPLY_2, StringConstants.TEXT_TOPIC_FAIRY_HERBS_ANSWER_2, PortraitEmotion.Emotion_Neutral, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_FAIRY_BEFORE_OPEN), 4, Constants.HERO_TOPIC_AFFINITY_SECOND, TopicGate.Gate_None, PortraitEmotion.Emotion_Sad, StringConstants.TEXT_TOPIC_FAIRY_BEFORE_OPEN,
                Reply(StringConstants.TEXT_TOPIC_FAIRY_BEFORE_REPLY_1, StringConstants.TEXT_TOPIC_FAIRY_BEFORE_ANSWER_1, PortraitEmotion.Emotion_Happy, 2),
                Reply(StringConstants.TEXT_TOPIC_FAIRY_BEFORE_REPLY_2, StringConstants.TEXT_TOPIC_FAIRY_BEFORE_ANSWER_2, PortraitEmotion.Emotion_Anxious, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_FAIRY_SECRET_OPEN), 4, Constants.HERO_TOPIC_AFFINITY_THIRD, TopicGate.Gate_None, PortraitEmotion.Emotion_Anxious, StringConstants.TEXT_TOPIC_FAIRY_SECRET_OPEN,
                Reply(StringConstants.TEXT_TOPIC_FAIRY_SECRET_REPLY_1, StringConstants.TEXT_TOPIC_FAIRY_SECRET_ANSWER_1, PortraitEmotion.Emotion_Relieved, 2),
                Reply(StringConstants.TEXT_TOPIC_FAIRY_SECRET_REPLY_2, StringConstants.TEXT_TOPIC_FAIRY_SECRET_ANSWER_2, PortraitEmotion.Emotion_Sad, 1)));
            topics.Add(Topic(nameof(StringConstants.TEXT_TOPIC_FAIRY_HEALED_OPEN), 4, Constants.HERO_TOPIC_AFFINITY_FIRST, TopicGate.Gate_Healed, PortraitEmotion.Emotion_Happy, StringConstants.TEXT_TOPIC_FAIRY_HEALED_OPEN,
                Reply(StringConstants.TEXT_TOPIC_FAIRY_HEALED_REPLY_1, StringConstants.TEXT_TOPIC_FAIRY_HEALED_ANSWER_1, PortraitEmotion.Emotion_Happy, 2),
                Reply(StringConstants.TEXT_TOPIC_FAIRY_HEALED_REPLY_2, StringConstants.TEXT_TOPIC_FAIRY_HEALED_ANSWER_2, PortraitEmotion.Emotion_Anxious, 1)));
            return topics;
        }
    }
}
