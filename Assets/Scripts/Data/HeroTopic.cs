using Underwrite.Enumeration;

namespace Underwrite.Data
{
    /// <summary>용사가 꺼내는 이야기 하나. 주인공은 대답을 고르고, 대답에 따라 호감과 표정이 달라진다.</summary>
    public sealed class HeroTopic
    {
        /// <summary>한 번 나눈 주제를 기억하는 열쇠. 꺼내는 말의 상수 이름이다.</summary>
        public string Id;

        /// <summary>누가 꺼내는가. HERO_* 배열의 순서.</summary>
        public int HeroIndex;

        public int MinAffinity;
        public TopicGate Gate;
        public string Opening;
        public PortraitEmotion OpeningEmotion;
        public HeroTopicReply[] Replies;
    }

    /// <summary>주인공의 대답 하나와 그에 대한 용사의 말.</summary>
    public sealed class HeroTopicReply
    {
        public string Text;
        public string Answer;
        public PortraitEmotion AnswerEmotion;

        /// <summary>오르는 호감. 2면 주인공이 들뜨는 몸짓을 한다.</summary>
        public int Affinity;
    }
}
