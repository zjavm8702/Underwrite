using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 공용 대화상자. 인물이 말하는 자리는 전부 이것을 쓴다.
    ///
    /// 정보 타임 연출과 영지에서 묻는 일이 서로 다른 틀을 쓰면
    /// 같은 세계에서 말투가 달라 보인다. 한 벌만 두고 돌려 쓴다.
    /// </summary>
    public sealed class UI_Dialogue
    {
        private const float RISE_SECONDS = 0.24f;
        private const float RISE_FROM = 40f;
        private const float FIGURE_SLIDE_FROM = 48f;
        private const float TYPE_PER_LETTER = 0.018f;

        private readonly VisualElement Root;
        private readonly VisualElement Veil;
        private readonly VisualElement Figure;
        private readonly VisualElement Box;
        private readonly Label Speaker;
        private readonly Label Tag;
        private readonly Label Text;
        private readonly Label Note;
        private readonly Label Count;
        private readonly Button Next;

        private readonly List<DialogueLine> Lines = new List<DialogueLine>();

        private Action OnDone;
        private Tween Running;
        private Sprite FigureShown;
        private int Index = -1;

        /// <summary>지금 말하는 중인가.</summary>
        public bool IsOpen
        {
            get
            {
                return Index >= 0;
            }
        }

        public UI_Dialogue(VisualElement root, VisualTreeAsset tree)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            Root = root;

            var built = tree.CloneTree();
            built.style.position = Position.Absolute;
            built.style.left = 0f;
            built.style.right = 0f;
            built.style.top = 0f;
            built.style.bottom = 0f;

            Veil = built.Q<VisualElement>("dialogue-root");
            Figure = built.Q<VisualElement>("dialogue-figure");
            Box = built.Q<VisualElement>("dialogue-box");
            Speaker = built.Q<Label>("dialogue-speaker");
            Tag = built.Q<Label>("dialogue-tag");
            Text = built.Q<Label>("dialogue-text");
            Note = built.Q<Label>("dialogue-note");
            Count = built.Q<Label>("dialogue-count");
            Next = built.Q<Button>("dialogue-next");

            if (Next != null)
            {
                Next.clicked += Advance;
            }

            Root.Add(built);
            Close();
        }

        /// <summary>배경 그림을 바꾼다. 지하실·여관·길드가 각자 다른 방이다.</summary>
        public void SetBackground(string className)
        {
            if (Veil == null)
            {
                return;
            }

            Veil.RemoveFromClassList("dialogue-root--underworld");
            Veil.RemoveFromClassList("dialogue-root--tavern");
            Veil.RemoveFromClassList("dialogue-root--guild");
            Veil.RemoveFromClassList("dialogue-root--board");

            if (string.IsNullOrEmpty(className) == false)
            {
                Veil.AddToClassList(className);
            }
        }

        /// <summary>대화를 시작한다. 줄이 없으면 바로 끝낸 것으로 본다.</summary>
        public void Play(IList<DialogueLine> lines, Action onDone)
        {
            if (Veil == null || lines == null || lines.Count == 0)
            {
                if (onDone != null)
                {
                    onDone();
                }

                return;
            }

            Lines.Clear();

            for (var i = 0; i < lines.Count; i++)
            {
                Lines.Add(lines[i]);
            }

            OnDone = onDone;
            Index = 0;
            FigureShown = null;

            Veil.RemoveFromClassList("hidden");
            PlayEnter();
            Show();
        }

        /// <summary>한 줄 넘긴다. 글자가 아직 흐르는 중이면 먼저 끝까지 보여 준다.</summary>
        public void Advance()
        {
            if (Index < 0)
            {
                return;
            }

            if (Running != null && Running.IsActive() == true && Running.IsPlaying() == true)
            {
                Running.Complete();
                return;
            }

            Index++;

            if (Index < Lines.Count)
            {
                Show();
                return;
            }

            var done = OnDone;
            Close();

            if (done != null)
            {
                done();
            }
        }

        public void Close()
        {
            KillRunning();
            Index = -1;
            Lines.Clear();
            OnDone = null;
            FigureShown = null;

            if (Veil != null)
            {
                Veil.AddToClassList("hidden");
            }
        }

        private void Show()
        {
            var line = Lines[Index];

            SetLabel(Speaker, line.Speaker);
            SetLabel(Tag, line.Tag);
            SetLabel(Note, line.Note);

            if (Tag != null)
            {
                Tag.EnableInClassList("dialogue-tag--secret", line.IsSecret);
            }

            if (Count != null)
            {
                Count.text = Lines.Count > 1 ? $"{Index + 1} / {Lines.Count}" : string.Empty;
            }

            if (Next != null)
            {
                Next.text = Index + 1 >= Lines.Count ? "끝" : "다음";
            }

            ShowFigure(line.Portrait);
            TypeOut(line.Text);
        }

        private void ShowFigure(Sprite portrait)
        {
            if (Figure == null)
            {
                return;
            }

            if (portrait == null)
            {
                Figure.style.backgroundImage = StyleKeyword.None;
                Figure.AddToClassList("hidden");
                return;
            }

            Figure.RemoveFromClassList("hidden");

            // 같은 사람이면 다시 세우지 않는다. 줄마다 깜빡이면 거슬린다.
            if (ReferenceEquals(FigureShown, portrait) == true)
            {
                return;
            }

            FigureShown = portrait;
            Figure.style.backgroundImage = new StyleBackground(portrait);

            var progress = 0f;
            DOTween.To(() => progress, value =>
            {
                progress = value;

                if (Figure.panel == null)
                {
                    return;
                }

                Figure.style.opacity = value;
                Figure.style.translate = new Translate(Mathf.Lerp(-FIGURE_SLIDE_FROM, 0f, value), 0f);
            }, 1f, 0.32f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        /// <summary>
        /// 글자가 흘러나온다. 한 번에 다 뜨면 읽기 전에 눈이 먼저 지나간다.
        /// 넘기기를 누르면 즉시 끝까지 보여 주므로 기다릴 필요는 없다.
        /// </summary>
        private void TypeOut(string text)
        {
            KillRunning();

            if (Text == null)
            {
                return;
            }

            var full = text == null ? string.Empty : text;
            Text.text = string.Empty;

            var shown = 0f;
            Running = DOTween.To(() => shown, value =>
            {
                shown = value;

                if (Text.panel == null)
                {
                    return;
                }

                var count = Mathf.Clamp(Mathf.RoundToInt(value), 0, full.Length);
                Text.text = full.Substring(0, count);
            }, full.Length, full.Length * TYPE_PER_LETTER)
                .SetEase(Ease.Linear).SetUpdate(true);
        }

        private void PlayEnter()
        {
            if (Box == null)
            {
                return;
            }

            Veil.style.opacity = 0f;
            Box.style.translate = new Translate(0f, RISE_FROM);

            var progress = 0f;
            DOTween.To(() => progress, value =>
            {
                progress = value;

                if (Veil.panel == null)
                {
                    return;
                }

                Veil.style.opacity = value;
                Box.style.translate = new Translate(0f, Mathf.Lerp(RISE_FROM, 0f, value));
            }, 1f, RISE_SECONDS).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void KillRunning()
        {
            if (Running == null)
            {
                return;
            }

            Running.Kill();
            Running = null;
        }

        private static void SetLabel(Label label, string value)
        {
            if (label == null)
            {
                return;
            }

            label.text = value == null ? string.Empty : value;
            label.EnableInClassList("hidden", string.IsNullOrEmpty(value));
        }
    }
}
