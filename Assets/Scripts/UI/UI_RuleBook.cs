using System;
using System.Collections.Generic;
using Underwrite.Global;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>규칙서 한 쪽. 무대(<see cref="Stage"/>)에 그림 부품을 세우고 움직이는 것은 쪽마다 다르다.</summary>
    public sealed class RulePage
    {
        public string Title;
        public string Body;

        /// <summary>무대를 꾸민다. 빈 무대 요소를 받아 부품을 넣고, 필요하면 schedule로 움직인다.</summary>
        public Action<VisualElement> Stage;
    }

    /// <summary>
    /// 미니게임 규칙서(2026-10-18 사장님 — 「미니게임 설명을 움직이든 텍스트든 연출을 통해 풀로」).
    /// 연습 판 전에 쪽을 넘기며 본다. 쪽마다 가운데 무대에서 실제 게임 부품(주사위·주문서·표)이 움직이며 규칙을 보여 주고,
    /// 아래에 두세 줄 설명이 붙는다. 마지막 쪽의 단추가 「해 본다」다. 뒤로 갈 수 있고, 건너뛸 수 없다(강제 튜토리얼).
    /// </summary>
    public sealed class UI_RuleBook
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly Label Heading;
        private readonly Label Title;
        private readonly VisualElement Stage;
        private readonly Label Body;
        private readonly Label Count;
        private readonly Button Back;
        private readonly Button Next;

        private List<RulePage> Pages;
        private int Index;
        private Action Done;
        private string Place;

        public bool IsOpen
        {
            get
            {
                return Overlay.parent != null;
            }
        }

        public UI_RuleBook(VisualElement root)
        {
            Root = root;
            Overlay = new VisualElement();
            Overlay.AddToClassList("rulebook");

            var page = new VisualElement();
            page.AddToClassList("popup");
            page.AddToClassList("rulebook-page");

            Heading = new Label();
            Heading.AddToClassList("rulebook-heading");
            Title = new Label();
            Title.AddToClassList("rulebook-title");
            Stage = new VisualElement();
            Stage.AddToClassList("rulebook-stage");
            Body = new Label();
            Body.AddToClassList("rulebook-body");

            var footer = new VisualElement();
            footer.AddToClassList("rulebook-footer");
            Back = new Button(OnBack);
            Back.text = StringConstants.TEXT_RULE_BACK;
            Back.AddToClassList("popup-button");
            Back.AddToClassList("rulebook-button");
            Count = new Label();
            Count.AddToClassList("rulebook-count");
            Next = new Button(OnNext);
            Next.AddToClassList("popup-button");
            Next.AddToClassList("popup-button--main");
            Next.AddToClassList("rulebook-button");
            footer.Add(Back);
            footer.Add(Count);
            footer.Add(Next);

            page.Add(Heading);
            page.Add(Title);
            page.Add(Stage);
            page.Add(Body);
            page.Add(footer);
            Overlay.Add(page);
        }

        /// <summary>규칙서를 편다. 마지막 쪽에서 「해 본다」를 누르면 닫고 <paramref name="onDone"/>.</summary>
        public void Open(string heading, List<RulePage> pages, string placeClass, Action onDone)
        {
            Pages = pages;
            Done = onDone;
            Index = 0;
            Heading.text = heading;

            if (string.IsNullOrEmpty(Place) == false)
            {
                Overlay.RemoveFromClassList(Place);
            }

            Place = placeClass;
            Overlay.AddToClassList(Place);

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
            Show();
        }

        public void Close()
        {
            Stage.Clear();
            Overlay.RemoveFromHierarchy();
        }

        private void Show()
        {
            var page = Pages[Index];
            Title.text = page.Title;
            Body.text = page.Body;
            Count.text = string.Format(StringConstants.TEXT_RULE_COUNT, Index + 1, Pages.Count);
            Back.SetEnabled(Index > 0);
            Next.text = Index == Pages.Count - 1 ? StringConstants.TEXT_RULE_TRY : StringConstants.TEXT_RULE_NEXT;

            // 쪽을 넘길 때마다 무대를 새로 세운다. 이전 쪽의 움직임(schedule)은 요소와 함께 사라진다.
            Stage.Clear();
            var set = new VisualElement();
            set.AddToClassList("rulebook-set");
            Stage.Add(set);

            if (page.Stage != null)
            {
                page.Stage(set);
            }
        }

        private void OnBack()
        {
            if (Index > 0)
            {
                Index--;
                Show();
            }
        }

        private void OnNext()
        {
            if (Index < Pages.Count - 1)
            {
                Index++;
                Show();
                return;
            }

            Close();
            var done = Done;
            Done = null;

            if (done != null)
            {
                done();
            }
        }
    }
}
