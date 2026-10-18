using System;
using Underwrite.Global;
using Underwrite.Village;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 주문서 읽기 화면. 대장간 배경 위에 여관 소문 한 줄과 주문서 꽂이(종이 여러 장)를 펼친다.
    /// 주문서를 짚으면 판정은 부른 쪽(<see cref="Picked"/>)이 한다. 이 화면은 보여 주고 받기만 한다.
    /// </summary>
    public sealed class UI_SmithOrders
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly Label Rumor;
        private readonly Label Hint;
        private readonly VisualElement Rack;
        private readonly Label Suspicion;
        private readonly Label Message;
        private readonly Button LeaveButton;
        private string Place;

        /// <summary>주문서를 짚었다. 꽂이 안의 순번을 넘긴다.</summary>
        public event Action<int> Picked;

        /// <summary>「그만둔다」를 눌렀다.</summary>
        public event Action Left;

        public bool IsOpen
        {
            get
            {
                return Overlay.parent != null;
            }
        }

        public UI_SmithOrders(VisualElement root)
        {
            Root = root;

            Overlay = new VisualElement();
            // 배경은 장소 클래스가 깐다. 머리말·꼬리말은 장부 맞추기와 같은 모양을 쓴다.
            Overlay.AddToClassList("smith-orders");

            var header = new VisualElement();
            header.AddToClassList("ledger-header");
            var title = new Label(StringConstants.TEXT_SMITH_TITLE);
            title.AddToClassList("ledger-title");
            Suspicion = new Label();
            Suspicion.AddToClassList("ledger-suspicion");
            header.Add(title);
            header.Add(Suspicion);

            Rumor = new Label();
            Rumor.AddToClassList("smith-rumor");

            Hint = new Label(StringConstants.TEXT_SMITH_HINT);
            Hint.AddToClassList("ledger-hint");

            Rack = new VisualElement();
            Rack.AddToClassList("smith-rack");

            var footer = new VisualElement();
            footer.AddToClassList("ledger-footer");
            Message = new Label();
            Message.AddToClassList("ledger-message");
            LeaveButton = new Button(OnLeave);
            LeaveButton.text = StringConstants.TEXT_SMITH_LEAVE;
            LeaveButton.AddToClassList("popup-button");
            LeaveButton.AddToClassList("ledger-leave");
            footer.Add(Message);
            footer.Add(LeaveButton);

            Overlay.Add(header);
            Overlay.Add(Rumor);
            Overlay.Add(Hint);
            Overlay.Add(Rack);
            Overlay.Add(footer);
        }

        /// <summary>
        /// 꽂이를 펼친다. <paramref name="placeClass"/>는 배경 그림 클래스(대화 배경과 같다).
        /// <paramref name="task"/>가 있으면 소문 줄 대신 그 부탁을 띄운다(연습 꽂이).
        /// </summary>
        public void Open(SmithOrderPuzzle puzzle, string rumorBand, string placeClass, string task = null)
        {
            Rack.Clear();
            Message.text = string.Empty;
            Rumor.text = task ?? string.Format(StringConstants.TEXT_SMITH_RUMOR, rumorBand, puzzle.Rumor);
            LeaveButton.style.display = puzzle.IsPractice == true ? DisplayStyle.None : DisplayStyle.Flex;
            Suspicion.style.display = puzzle.IsPractice == true ? DisplayStyle.None : DisplayStyle.Flex;
            Hint.text = puzzle.IsPractice == true ? StringConstants.TEXT_SMITH_TUT_HINT : StringConstants.TEXT_SMITH_HINT;

            if (string.IsNullOrEmpty(Place) == false)
            {
                Overlay.RemoveFromClassList(Place);
            }

            Place = placeClass;
            Overlay.AddToClassList(Place);

            for (var i = 0; i < puzzle.Slips.Count; i++)
            {
                var slip = puzzle.Slips[i];
                var index = i;
                var card = new Button(() => OnPick(index));
                card.AddToClassList("popup");
                card.AddToClassList("smith-slip");

                // 꽂이에 손으로 꽂은 종이라 반듯하지 않다. 자리마다 정해진 기울기.
                var tilts = Constants.SMITH_SLIP_TILTS;
                card.style.rotate = new Rotate(new Angle(tilts[i % tilts.Length], AngleUnit.Degree));

                var day = slip.Day == 0
                    ? StringConstants.TEXT_SMITH_LAST_SEASON
                    : string.Format(StringConstants.TEXT_SMITH_DAY, slip.Day);
                var top = new VisualElement();
                top.AddToClassList("smith-slip-top");
                top.pickingMode = PickingMode.Ignore;
                top.Add(Text(day, "smith-slip-day"));
                var status = Text(StringConstants.TEXT_SMITH_STATUS[slip.Status], "smith-slip-status");
                status.EnableInClassList("smith-slip-status--cancelled",
                    slip.Status == StringConstants.TEXT_SMITH_STATUS.Length - 1);
                top.Add(status);

                card.Add(top);
                card.Add(Text(slip.BandName, "smith-slip-band"));
                card.Add(Text(slip.Item, "smith-slip-item"));
                Rack.Add(card);
            }

            ShowSuspicion(puzzle.Suspicion);

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
        }

        /// <summary>잘못 짚은 주문서를 표시하고 의심을 갱신한다.</summary>
        public void MarkWrong(int index, int suspicion, string message)
        {
            if (index >= 0 && index < Rack.childCount)
            {
                var card = Rack[index];
                card.AddToClassList("smith-slip--wrong");
                card.SetEnabled(false);
            }

            Message.text = message;
            ShowSuspicion(suspicion);
        }

        public void Close()
        {
            Overlay.RemoveFromHierarchy();
        }

        private void ShowSuspicion(int suspicion)
        {
            Suspicion.text = string.Format(StringConstants.TEXT_SMITH_SUSPICION, suspicion,
                Constants.SMITH_ORDER_MAX_SUSPICION);
        }

        private void OnPick(int index)
        {
            var handler = Picked;

            if (handler != null)
            {
                handler(index);
            }
        }

        private void OnLeave()
        {
            var handler = Left;

            if (handler != null)
            {
                handler();
            }
        }

        private static Label Text(string text, string kind)
        {
            var label = new Label(text);
            label.AddToClassList(kind);
            label.pickingMode = PickingMode.Ignore;
            return label;
        }
    }
}
