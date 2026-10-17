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
        private readonly Label Badge;
        private readonly Label Tag;
        private readonly Label Text;
        private readonly Label Note;
        private readonly Label Count;
        private readonly Button Next;
        private readonly VisualElement ChoiceBox;
        private readonly VisualElement Spacer;

        private readonly List<DialogueLine> Lines = new List<DialogueLine>();
        private readonly List<DialogueChoice> ShownChoices = new List<DialogueChoice>();

        private Action OnDone;
        private Tween Running;
        private Sprite FigureShown;
        private int Index = -1;

        // Play가 불릴 때마다 오른다. 선택지 안에서 대화가 이어졌는지 알아보는 데 쓴다.
        private int PlayCount;

        /// <summary>선택지가 떠 있어 고르기를 기다리는 중인가. 이때는 「다음」으로 넘길 수 없다.</summary>
        public bool IsChoosing
        {
            get
            {
                return ShownChoices.Count > 0;
            }
        }

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
            // 틀은 화면 전체를 덮는 투명한 상자다. 클릭을 받으면 대화가 닫혀 있어도 아래 화면이 전부 막힌다.
            // 클릭은 대화가 열렸을 때 dialogue-root가 받는다. 닫히면 hidden(display: none)이라 통과한다.
            built.pickingMode = PickingMode.Ignore;

            Veil = built.Q<VisualElement>("dialogue-root");
            Figure = built.Q<VisualElement>("dialogue-figure");
            Box = built.Q<VisualElement>("dialogue-box");
            Speaker = built.Q<Label>("dialogue-speaker");

            // 이름 옆 배지. 이름 줄에 끼워 넣는다.
            Badge = new Label();
            Badge.AddToClassList("dialogue-badge");
            Badge.AddToClassList("hidden");

            if (Speaker != null && Speaker.parent != null)
            {
                Speaker.parent.Insert(Speaker.parent.IndexOf(Speaker) + 1, Badge);
            }
            Tag = built.Q<Label>("dialogue-tag");
            Text = built.Q<Label>("dialogue-text");
            Note = built.Q<Label>("dialogue-note");
            Count = built.Q<Label>("dialogue-count");
            Next = built.Q<Button>("dialogue-next");
            ChoiceBox = built.Q<VisualElement>("dialogue-choices");
            // 본문과 「다음」 사이를 벌리는 빈 칸. 선택지가 뜨면 접는다.
            // 그대로 두면 선택지 위에 빈 칸이 끼어 선택지가 상자 아래로 삐져나간다(2026-10-17 보고).
            Spacer = FindSpacer(Box);

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

        // 상자의 직속 자식 가운데 빈 칸(grow)을 찾는다. 이름 줄의 말하는 이 라벨도 grow를 달고 있어
        // 깊이 찾으면 그것이 먼저 걸린다.
        private static VisualElement FindSpacer(VisualElement box)
        {
            if (box == null)
            {
                return null;
            }

            foreach (var child in box.Children())
            {
                if (child.ClassListContains("grow") == true && child.childCount == 0 && child is Label == false)
                {
                    return child;
                }
            }

            return null;
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

            // 선택지를 고른 자리에서 이어 말하는 경우다. 상자를 다시 띄우면 깜빡이므로 그대로 둔다.
            var continuing = IsOpen;

            PlayCount++;
            ClearChoices();
            OnDone = onDone;
            Index = 0;

            if (continuing == false)
            {
                FigureShown = null;
                Veil.RemoveFromClassList("hidden");
                PlayEnter();
            }

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

            // 고를 것이 있는 줄은 넘길 수 없다. 넘기면 아무것도 고르지 않은 채 대화가 끝난다.
            if (HasChoices(Lines[Index]) == true)
            {
                return;
            }

            Index++;

            if (Index < Lines.Count)
            {
                Show();
                return;
            }

            // 마무리가 대화를 이어 가면(Play를 다시 부르면) 상자를 닫지 않는다.
            // 닫았다 다시 열면 필드가 한 번 비쳐 깜빡인다(2026-10-17 사용자 보고).
            var done = OnDone;
            var before = PlayCount;
            OnDone = null;

            if (done != null)
            {
                done();
            }

            if (PlayCount == before)
            {
                Close();
            }
        }

        /// <summary>
        /// 떠 있는 선택지 가운데 하나를 고른다. 키보드 숫자 입력이 쓴다. 0부터 센다.
        /// 선택지가 없거나, 아직 글자가 흐르는 중이거나, 잠긴 것이면 고르지 않고 false를 돌려준다.
        /// </summary>
        public bool TryPick(int index)
        {
            if (index < 0 || index >= ShownChoices.Count)
            {
                return false;
            }

            return Pick(ShownChoices[index]);
        }

        public void Close()
        {
            KillRunning();
            ClearChoices();
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
            SetLabel(Badge, line.Badge);
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
                // 고르는 줄에서는 「다음」을 숨긴다. 둘 다 있으면 무엇을 누를지 망설인다.
                Next.EnableInClassList("hidden", HasChoices(line));
            }

            ShowFigure(line.Portrait);
            TypeOut(line.Text, HasChoices(line) == true ? line.Choices : null);
        }

        private static bool HasChoices(DialogueLine line)
        {
            return line != null && line.Choices != null && line.Choices.Count > 0;
        }

        /// <summary>
        /// 선택지를 세운다. 글자가 다 흐른 뒤에 띄운다.
        /// 말을 다 듣기 전에 고를 것이 보이면 읽지 않고 고른다.
        /// </summary>
        private void ShowChoices(List<DialogueChoice> choices)
        {
            ClearChoices();

            if (ChoiceBox == null || choices == null)
            {
                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];

                if (choice == null)
                {
                    continue;
                }

                ShownChoices.Add(choice);

                var button = new Button();
                button.AddToClassList("dialogue-choice");
                button.EnableInClassList("dialogue-choice--locked", choice.IsEnabled == false);

                // 번호를 붙여 둔다. 키보드 숫자로 고를 수 있다는 것을 따로 알려 줄 필요가 없다.
                var text = new Label($"{ShownChoices.Count}. {choice.Text}");
                text.AddToClassList("dialogue-choice-text");
                button.Add(text);

                if (string.IsNullOrEmpty(choice.Note) == false)
                {
                    var note = new Label(choice.Note);
                    note.AddToClassList("dialogue-choice-note");
                    button.Add(note);
                }

                button.clicked += () => Pick(choice);
                ChoiceBox.Add(button);
            }

            ChoiceBox.EnableInClassList("hidden", ShownChoices.Count == 0);

            if (Spacer != null)
            {
                Spacer.EnableInClassList("hidden", ShownChoices.Count > 0);
            }
        }

        private void ClearChoices()
        {
            ShownChoices.Clear();

            if (ChoiceBox == null)
            {
                return;
            }

            ChoiceBox.Clear();
            ChoiceBox.AddToClassList("hidden");

            if (Spacer != null)
            {
                Spacer.RemoveFromClassList("hidden");
            }
        }

        /// <summary>
        /// 고른다. 고른 것의 할 일이 대화를 이어 가면(Play를 다시 부르면) 상자를 그대로 둔다.
        /// 이어 가지 않으면 대화를 닫고 처음 Play에 넘긴 마무리를 부른다.
        /// </summary>
        private bool Pick(DialogueChoice choice)
        {
            if (choice == null || choice.IsEnabled == false || ShownChoices.Contains(choice) == false)
            {
                return false;
            }

            var before = PlayCount;
            var done = OnDone;
            ClearChoices();

            if (choice.OnPicked != null)
            {
                choice.OnPicked();
            }

            if (PlayCount != before)
            {
                return true;
            }

            Close();

            if (done != null)
            {
                done();
            }

            return true;
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
        private void TypeOut(string text, List<DialogueChoice> choices)
        {
            KillRunning();

            if (Text == null)
            {
                ShowChoices(choices);
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

            if (choices != null)
            {
                // 넘기기로 글자를 끝까지 보여 줘도 Complete가 이것을 부른다.
                Running.OnComplete(() => ShowChoices(choices));
            }
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
