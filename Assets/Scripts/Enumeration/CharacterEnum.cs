namespace Underwrite.Enumeration
{
    /// <summary>
    /// 대화에서 쓰는 표정. 상반신 그림이 표정마다 따로 있다.
    ///
    /// 기획서 §17의 인물별 연기 방향과 맞물린다.
    /// 소식의 무게에 따라 말하는 이의 얼굴이 달라져야 글자보다 먼저 분위기가 전해진다.
    /// </summary>
    public enum PortraitEmotion
    {
        Emotion_Neutral,
        Emotion_Happy,
        Emotion_Anxious,
        Emotion_Angry,
        Emotion_Shocked,
        Emotion_Sad,
        Emotion_Relieved
    }

    /// <summary>용사 이야기 주제가 열리는 조건. 호감과 별개로, 그 일이 있은 뒤에만 나오는 주제가 있다.</summary>
    public enum TopicGate
    {
        /// <summary>호감만 보면 된다.</summary>
        Gate_None,

        /// <summary>시찰일을 함께 보낸 뒤.</summary>
        Gate_DayOff,

        /// <summary>귀환했고 치료비 의뢰가 닿았을 때.</summary>
        Gate_Healed,

        /// <summary>귀환했고 학자가 의뢰주를 의심하기 시작했을 때.</summary>
        Gate_Suspect
    }
}
