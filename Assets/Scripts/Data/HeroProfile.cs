using System.Collections.Generic;

namespace Underwrite.Data
{
    /// <summary>
    /// 용사단 한 사람. 이름이 아직 정해지지 않아 역할 이름을 임시로 쓴다(2026-10-17).
    ///
    /// 관계는 두 갈래다(기획서 §8). 이 클래스가 드는 것은 **일상의 주인공**에 대한 호감뿐이다.
    /// 익명 의뢰주에 대한 감정과 단서는 <see cref="Patronage.PatronageLedger"/>가 든다.
    /// 숫자는 화면에 보이지 않는다. 표정과 말로만 드러난다.
    /// </summary>
    public sealed class HeroProfile
    {
        public string Id;
        public string Name;

        /// <summary>대화 꼬리표에 붙는 말. 단에서 맡은 자리.</summary>
        public string Role;

        /// <summary>표정 묶음의 인덱스. 영지 화면이 들고 있는 묶음 목록의 순서다.</summary>
        public int FaceIndex;

        /// <summary>일상의 주인공(여관 일꾼)에 대한 호감.</summary>
        public int Affinity;

        /// <summary>마지막으로 말을 나눈 날. 하루에 한 번만 이야기한다.</summary>
        public int LastTalkDay;

        /// <summary>이미 나눈 이야기 주제(HeroTopic.Id). 저장이 들어오면 함께 저장한다.</summary>
        public readonly HashSet<string> TopicsSeen = new HashSet<string>();
    }
}
