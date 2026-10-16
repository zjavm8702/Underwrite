using Underwrite.Enumeration;
using UnityEngine;

namespace Underwrite.UI
{
    /// <summary>
    /// 대화 한 줄.
    ///
    /// 누가, 어떤 자격으로, 무슨 말을 하는가. 세 가지가 한 줄을 이룬다.
    /// 출처(Tag)가 따로 있는 이유는 기획서 §8이 "출처와 함께 제공해 추론 근거로 쓴다"고 했기 때문이다.
    /// 같은 말이라도 여관에서 들은 것과 길드 기록에 적힌 것은 무게가 다르다.
    /// </summary>
    public sealed class DialogueLine
    {
        /// <summary>말하는 이의 이름. 비어 있으면 이름줄을 숨긴다.</summary>
        public string Speaker;

        /// <summary>출처나 자격. "여관 소문 · 셀라 (신입)" 같은 꼬리표다.</summary>
        public string Tag;

        public string Text;

        /// <summary>아래에 작게 붙는 말. 규칙 설명이나 상태 안내에 쓴다.</summary>
        public string Note;

        /// <summary>세울 그림. 없으면 자리를 비운다.</summary>
        public Sprite Portrait;

        /// <summary>
        /// 어떤 얼굴로 말하는가. 그림을 표정 묶음에서 고를 때 쓴다.
        /// 소식의 무게가 글자보다 먼저 전해지는 길이다.
        /// </summary>
        public PortraitEmotion Emotion = PortraitEmotion.Emotion_Neutral;

        /// <summary>시장이 아직 모르는 이야기인가. 꼬리표 색이 달라진다.</summary>
        public bool IsSecret;

        public DialogueLine(string speaker, string tag, string text)
        {
            Speaker = speaker;
            Tag = tag;
            Text = text;
        }
    }
}
