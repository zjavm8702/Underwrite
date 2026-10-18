using System;
using Underwrite.Global;
using Underwrite.Village;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 장부 맞추기 화면. 길드 기록실 책상 위에 양피지 두 장을 펼친다 — 왼쪽 의뢰 기록, 오른쪽 지급 기록.
    /// 의뢰 줄을 짚으면 판정은 부른 쪽(<see cref="Picked"/>)이 한다. 이 화면은 보여 주고 받기만 한다.
    /// </summary>
    public sealed class UI_LedgerPuzzle
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly VisualElement RequestList;
        private readonly VisualElement PaymentList;
        private readonly Label Suspicion;
        private readonly Label Message;

        /// <summary>의뢰 줄을 짚었다. 줄 번호를 넘긴다.</summary>
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

        public UI_LedgerPuzzle(VisualElement root)
        {
            Root = root;

            Overlay = new VisualElement();
            Overlay.AddToClassList("ledger");

            var header = new VisualElement();
            header.AddToClassList("ledger-header");
            var title = new Label(StringConstants.TEXT_LEDGER_TITLE);
            title.AddToClassList("ledger-title");
            Suspicion = new Label();
            Suspicion.AddToClassList("ledger-suspicion");
            header.Add(title);
            header.Add(Suspicion);

            var hint = new Label(StringConstants.TEXT_LEDGER_HINT);
            hint.AddToClassList("ledger-hint");

            var pages = new VisualElement();
            pages.AddToClassList("ledger-pages");
            RequestList = Page(pages, StringConstants.TEXT_LEDGER_REQUESTS, StringConstants.TEXT_LEDGER_REQUEST_HEAD);
            PaymentList = Page(pages, StringConstants.TEXT_LEDGER_PAYMENTS, StringConstants.TEXT_LEDGER_PAYMENT_HEAD);

            var footer = new VisualElement();
            footer.AddToClassList("ledger-footer");
            Message = new Label();
            Message.AddToClassList("ledger-message");
            var leave = new Button(OnLeave);
            leave.text = StringConstants.TEXT_LEDGER_LEAVE;
            leave.AddToClassList("popup-button");
            leave.AddToClassList("ledger-leave");
            footer.Add(Message);
            footer.Add(leave);

            Overlay.Add(header);
            Overlay.Add(hint);
            Overlay.Add(pages);
            Overlay.Add(footer);
        }

        /// <summary>장부를 펼친다.</summary>
        public void Open(LedgerPuzzle puzzle)
        {
            RequestList.Clear();
            PaymentList.Clear();
            Message.text = string.Empty;

            for (var i = 0; i < puzzle.Requests.Count; i++)
            {
                var request = puzzle.Requests[i];
                var index = i;
                var row = new Button(() => OnPick(index));
                row.AddToClassList("ledger-row");
                row.AddToClassList("ledger-row--pick");
                var who = new Label(string.Format(StringConstants.TEXT_LEDGER_REQUEST_ROW, request.BandName, request.Job));
                who.AddToClassList("ledger-cell");
                who.AddToClassList("ledger-cell--grow");
                row.Add(who);
                row.Add(Cell(request.Reward.ToString("N0"), "ledger-cell--amount"));
                RequestList.Add(row);
            }

            for (var i = 0; i < puzzle.Payments.Count; i++)
            {
                var payment = puzzle.Payments[i];
                var row = new VisualElement();
                row.AddToClassList("ledger-row");
                row.Add(Cell(string.Format(StringConstants.TEXT_LEDGER_PAYMENT_DAY, payment.Day), "ledger-cell--day"));
                row.Add(Cell(payment.BandName, "ledger-cell--grow"));
                row.Add(Cell(payment.Amount.ToString("N0"), "ledger-cell--amount"));
                row.Add(Cell(payment.Note, "ledger-cell--note"));
                PaymentList.Add(row);
            }

            ShowSuspicion(puzzle.Suspicion);

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
        }

        /// <summary>잘못 짚은 줄을 표시하고 의심을 갱신한다.</summary>
        public void MarkWrong(int requestIndex, int suspicion)
        {
            if (requestIndex >= 0 && requestIndex < RequestList.childCount)
            {
                var row = RequestList[requestIndex];
                row.AddToClassList("ledger-row--wrong");
                row.SetEnabled(false);
            }

            Message.text = StringConstants.TEXT_LEDGER_WRONG;
            ShowSuspicion(suspicion);
        }

        public void Close()
        {
            Overlay.RemoveFromHierarchy();
        }

        private void ShowSuspicion(int suspicion)
        {
            Suspicion.text = string.Format(StringConstants.TEXT_LEDGER_SUSPICION, suspicion,
                Constants.LEDGER_MAX_SUSPICION);
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

        private static Label Cell(string text, string kind)
        {
            var cell = new Label(text);
            cell.AddToClassList("ledger-cell");
            cell.AddToClassList(kind);
            cell.pickingMode = PickingMode.Ignore;
            return cell;
        }

        private static VisualElement Page(VisualElement pages, string title, string head)
        {
            var page = new VisualElement();
            page.AddToClassList("popup");
            page.AddToClassList("ledger-page");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("popup-title");
            var headLabel = new Label(head);
            headLabel.AddToClassList("ledger-head");

            var list = new ScrollView();
            list.AddToClassList("ledger-list");

            page.Add(titleLabel);
            page.Add(headLabel);
            page.Add(list);
            pages.Add(page);
            return list;
        }
    }
}
