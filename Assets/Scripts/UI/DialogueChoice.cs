using System;

namespace Underwrite.UI
{
    /// <summary>
    /// 대화 중에 고를 수 있는 것 하나.
    ///
    /// 기획서 §5는 대화 화면에 "큰 인물 초상화와 표정, 텍스트, **선택지**"를 둔다고 했다.
    /// 퀘스트를 받을지, 돈을 낼지, 후원을 할지가 전부 이 자리에서 갈린다.
    ///
    /// 고를 수 없는 것도 보여 준다. 돈이 모자라 못 고르는 선택지가 사라져 버리면
    /// 플레이어는 그런 길이 있었다는 것조차 모른다. 잠긴 채로 이유를 붙여 둔다.
    /// </summary>
    public sealed class DialogueChoice
    {
        /// <summary>단추에 적힐 말. 플레이어가 하는 말이나 행동이다.</summary>
        public string Text;

        /// <summary>단추 아래 작은 글씨. 값이나 걸리는 시간, 잠긴 이유를 적는다.</summary>
        public string Note;

        /// <summary>지금 고를 수 있는가. 꺼져 있으면 보이되 누를 수 없다.</summary>
        public bool IsEnabled = true;

        /// <summary>
        /// 골랐을 때 할 일. 이 안에서 <see cref="UI_Dialogue.Play"/>를 다시 부르면
        /// 대화상자가 닫히지 않고 그대로 이어진다. 부르지 않으면 대화가 끝난다.
        /// </summary>
        public Action OnPicked;

        public DialogueChoice(string text, Action onPicked)
        {
            Text = text;
            OnPicked = onPicked;
        }
    }
}
