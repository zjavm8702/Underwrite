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
}
