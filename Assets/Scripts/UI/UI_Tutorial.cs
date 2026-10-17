using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>한 단계. 어디를 비추고 무엇을 말하는가.</summary>
    public sealed class TutorialStep
    {
        /// <summary>비출 요소의 이름. 그 요소가 든 패널(.panel)이 있으면 패널을 통째로 비춘다. 비우면 화면 가운데.</summary>
        public string Target;
        public string Title;
        public string Body;

        public TutorialStep(string target, string title, string body)
        {
            Target = target;
            Title = title;
            Body = body;
        }
    }

    /// <summary>
    /// 처음 들어온 화면을 한 곳씩 비추며 설명한다(2026-10-17 사용자 요청 — 튜토리얼 뼈대).
    ///
    /// 둘레를 어둡게 덮고 비춘 곳만 밝게 남긴다. 설명은 비춘 곳 옆에 붙인다.
    /// 화면 위의 실제 요소를 이름으로 찾으므로, 화면 구성이 바뀌어도 단계 목록만 고치면 된다.
    /// </summary>
    public sealed class UI_Tutorial
    {
        private const float CARD_WIDTH = 440f;
        private const float GAP = 18f;
        private const float PAD = 8f;

        private readonly VisualElement Screen;
        private readonly VisualElement Layer;
        private readonly VisualElement[] Shades = new VisualElement[4];
        private readonly VisualElement Frame;
        private readonly VisualElement Card;
        private readonly Label Title;
        private readonly Label Body;
        private readonly Label Count;
        private readonly Button Next;

        private readonly List<TutorialStep> Steps = new List<TutorialStep>();
        private Action OnDone;
        private int Index = -1;

        public bool IsOpen
        {
            get
            {
                return Index >= 0;
            }
        }

        public UI_Tutorial(VisualElement screen, string nextText, string doneText, string skipText)
        {
            Screen = screen;
            NextText = nextText;
            DoneText = doneText;

            Layer = new VisualElement();
            Layer.name = "tutorial";
            Stretch(Layer);
            Layer.pickingMode = PickingMode.Position;

            for (var i = 0; i < Shades.Length; i++)
            {
                Shades[i] = new VisualElement();
                Shades[i].AddToClassList("tutorial-shade");
                Shades[i].style.position = Position.Absolute;
                Layer.Add(Shades[i]);
            }

            Frame = new VisualElement();
            Frame.AddToClassList("tutorial-frame");
            Frame.style.position = Position.Absolute;
            Frame.pickingMode = PickingMode.Ignore;
            Layer.Add(Frame);

            Card = new VisualElement();
            Card.AddToClassList("tutorial-card");
            Card.style.position = Position.Absolute;
            Card.style.width = CARD_WIDTH;

            Count = new Label();
            Count.AddToClassList("tutorial-count");
            Title = new Label();
            Title.AddToClassList("tutorial-title");
            Body = new Label();
            Body.AddToClassList("tutorial-body");

            var row = new VisualElement();
            row.AddToClassList("tutorial-buttons");

            var skip = new Button(Finish);
            skip.text = skipText;
            skip.AddToClassList("btn");
            Next = new Button(Advance);
            Next.AddToClassList("btn");
            Next.AddToClassList("btn--selected");

            row.Add(skip);
            row.Add(Next);
            Card.Add(Count);
            Card.Add(Title);
            Card.Add(Body);
            Card.Add(row);
            Layer.Add(Card);

            Layer.style.display = DisplayStyle.None;
            Screen.Add(Layer);
        }

        private string NextText { get; }
        private string DoneText { get; }

        public void Play(IList<TutorialStep> steps, Action onDone)
        {
            Steps.Clear();

            for (var i = 0; i < steps.Count; i++)
            {
                Steps.Add(steps[i]);
            }

            OnDone = onDone;
            Index = 0;
            Layer.style.display = DisplayStyle.Flex;
            Layer.BringToFront();
            Show();
        }

        /// <summary>매 프레임 부른다. 비춘 요소의 자리가 늦게 정해지거나 바뀌어도 따라간다.</summary>
        public void Tick()
        {
            if (Index >= 0)
            {
                Place();
            }
        }

        private void Advance()
        {
            Index++;

            if (Index >= Steps.Count)
            {
                Finish();
                return;
            }

            Show();
        }

        private void Finish()
        {
            Index = -1;
            Layer.style.display = DisplayStyle.None;
            var done = OnDone;
            OnDone = null;

            if (done != null)
            {
                done();
            }
        }

        private void Show()
        {
            var step = Steps[Index];
            Count.text = $"{Index + 1} / {Steps.Count}";
            Title.text = step.Title;
            Body.text = step.Body;
            Next.text = Index + 1 >= Steps.Count ? DoneText : NextText;
            Place();
        }

        private void Place()
        {
            var area = Layer.worldBound;
            var hole = HoleFor(Steps[Index].Target, area);

            // 둘레 네 조각을 어둡게. 가운데 구멍만 밝게 남는다.
            SetRect(Shades[0], 0f, 0f, area.width, hole.yMin);
            SetRect(Shades[1], 0f, hole.yMax, area.width, area.height - hole.yMax);
            SetRect(Shades[2], 0f, hole.yMin, hole.xMin, hole.height);
            SetRect(Shades[3], hole.xMax, hole.yMin, area.width - hole.xMax, hole.height);
            SetRect(Frame, hole.xMin, hole.yMin, hole.width, hole.height);
            Frame.style.display = hole.width > 0f ? DisplayStyle.Flex : DisplayStyle.None;

            // 설명 카드는 구멍의 오른쪽, 자리가 없으면 왼쪽, 그것도 없으면 아래.
            var cardHeight = float.IsNaN(Card.resolvedStyle.height) == true ? 220f : Card.resolvedStyle.height;
            float x;
            float y = Mathf.Clamp(hole.yMin, GAP, area.height - cardHeight - GAP);

            if (hole.width <= 0f)
            {
                x = (area.width - CARD_WIDTH) * 0.5f;
                y = (area.height - cardHeight) * 0.5f;
            }
            else if (hole.xMax + GAP + CARD_WIDTH < area.width)
            {
                x = hole.xMax + GAP;
            }
            else if (hole.xMin - GAP - CARD_WIDTH > 0f)
            {
                x = hole.xMin - GAP - CARD_WIDTH;
            }
            else
            {
                x = Mathf.Clamp(hole.center.x - CARD_WIDTH * 0.5f, GAP, area.width - CARD_WIDTH - GAP);
                y = hole.yMax + GAP + cardHeight < area.height ? hole.yMax + GAP : hole.yMin - GAP - cardHeight;
            }

            Card.style.left = x;
            Card.style.top = y;
        }

        private Rect HoleFor(string target, Rect area)
        {
            if (string.IsNullOrEmpty(target) == true)
            {
                return new Rect(area.width * 0.5f, area.height * 0.5f, 0f, 0f);
            }

            var element = Screen.Q<VisualElement>(target);

            if (element == null)
            {
                return new Rect(area.width * 0.5f, area.height * 0.5f, 0f, 0f);
            }

            // 요소가 든 패널을 통째로 비춘다. 단추 하나만 비추면 무엇의 일부인지 모른다.
            var panel = element;

            for (var parent = element.parent; parent != null; parent = parent.parent)
            {
                if (parent.ClassListContains("panel") == true)
                {
                    panel = parent;
                    break;
                }
            }

            var bound = panel.worldBound;
            return new Rect(bound.x - area.x - PAD, bound.y - area.y - PAD, bound.width + PAD * 2f,
                bound.height + PAD * 2f);
        }

        private static void SetRect(VisualElement element, float x, float y, float width, float height)
        {
            element.style.left = x;
            element.style.top = y;
            element.style.width = Mathf.Max(0f, width);
            element.style.height = Mathf.Max(0f, height);
        }

        private static void Stretch(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.right = 0f;
            element.style.top = 0f;
            element.style.bottom = 0f;
        }
    }
}
