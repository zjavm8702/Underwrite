using System;
using System.Text;
using Underwrite.Data;
using Underwrite.Global;
using Underwrite.Village;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 주문 나르기 화면. 일하는 곳의 배경 위에 양피지 한 장.
    /// 처음엔 주문표를 모두 보여 주고(외우기), 시간이 지나거나 「다 외웠다」를 누르면 가린 뒤 한 장씩 쟁반을 채운다.
    /// 판정은 <see cref="WorkOrderTask"/>가 하고, 끝나면 맞힌 비율을 <see cref="Finished"/>로 넘긴다.
    /// </summary>
    public sealed class UI_WorkOrders
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly Label Title;
        private readonly Label Head;
        private readonly VisualElement Body;
        private readonly Label Message;
        private readonly Button Sloppy;

        private WorkOrderTask Task;
        private int[] Tray;
        private string Place = string.Empty;
        private IVisualElementScheduledItem Countdown;
        private int SecondsLeft;

        /// <summary>다 내놓았다. 맞힌 비율(0~1)을 넘긴다.</summary>
        public event Action<float> Finished;

        /// <summary>「대충 끝낸다」를 눌렀다.</summary>
        public event Action Skipped;

        public bool IsOpen
        {
            get
            {
                return Overlay.parent != null;
            }
        }

        public UI_WorkOrders(VisualElement root)
        {
            Root = root;
            Overlay = new VisualElement();
            Overlay.AddToClassList("work");

            var panel = new VisualElement();
            panel.AddToClassList("popup");
            panel.AddToClassList("work-panel");

            Title = new Label();
            Title.AddToClassList("popup-title");
            Head = new Label();
            Head.AddToClassList("work-head");
            Body = new VisualElement();
            Body.AddToClassList("work-body");
            Message = new Label();
            Message.AddToClassList("work-message");

            var footer = new VisualElement();
            footer.AddToClassList("work-footer");
            var hint = new Label(StringConstants.TEXT_WORK_SLOPPY_HINT);
            hint.AddToClassList("work-hint");
            Sloppy = new Button(OnSkip);
            Sloppy.text = StringConstants.TEXT_WORK_SLOPPY;
            Sloppy.AddToClassList("popup-button");
            Sloppy.AddToClassList("work-button");
            footer.Add(hint);
            footer.Add(Sloppy);

            panel.Add(Title);
            panel.Add(Head);
            panel.Add(Body);
            panel.Add(Message);
            panel.Add(footer);
            Overlay.Add(panel);
        }

        /// <param name="placeClass">일하는 곳의 배경 클래스(대화 배경과 같은 것).</param>
        public void Open(WorkOrderTask task, string title, string placeClass)
        {
            Task = task;
            Tray = new int[task.Items.Length];
            Title.text = title;
            Message.text = string.Empty;
            Overlay.RemoveFromClassList(Place);
            Place = placeClass;
            Overlay.AddToClassList(Place);
            Sloppy.SetEnabled(true);

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
            ShowTickets();
        }

        public void Close()
        {
            StopCountdown();
            Overlay.RemoveFromHierarchy();
        }

        // ── 외우기 ─────────────────────────────
        private void ShowTickets()
        {
            Body.Clear();
            SecondsLeft = Constants.WORK_MEMORIZE_SECONDS;
            Head.text = string.Format(StringConstants.TEXT_WORK_MEMORIZE, SecondsLeft);

            var tickets = new VisualElement();
            tickets.AddToClassList("work-tickets");

            // 주문표는 나르는 순서와 다르게 섞어 보인다. 순서대로 외우면 행선지를 볼 필요가 없어진다.
            var shown = new int[Task.Orders.Count];

            for (var i = 0; i < shown.Length; i++)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                shown[i] = shown[j];
                shown[j] = i;
            }

            for (var i = 0; i < shown.Length; i++)
            {
                var ticket = new Label(Describe(Task.Orders[shown[i]], true));
                ticket.AddToClassList("work-ticket");
                tickets.Add(ticket);
            }

            var ready = new Button(StartServing);
            ready.text = StringConstants.TEXT_WORK_READY;
            ready.AddToClassList("popup-button");
            ready.AddToClassList("popup-button--main");
            ready.AddToClassList("work-button");

            Body.Add(tickets);
            Body.Add(ready);

            StopCountdown();
            Countdown = Overlay.schedule.Execute(TickCountdown).Every(1000).StartingIn(1000);
        }

        private void TickCountdown()
        {
            SecondsLeft--;

            if (SecondsLeft <= 0)
            {
                StartServing();
                return;
            }

            Head.text = string.Format(StringConstants.TEXT_WORK_MEMORIZE, SecondsLeft);
        }

        private void StopCountdown()
        {
            if (Countdown != null)
            {
                Countdown.Pause();
                Countdown = null;
            }
        }

        // ── 나르기 ─────────────────────────────
        private void StartServing()
        {
            StopCountdown();
            Array.Clear(Tray, 0, Tray.Length);
            ShowServe();
        }

        private void ShowServe()
        {
            Body.Clear();
            var order = Task.Current;

            if (order == null)
            {
                Finish();
                return;
            }

            Head.text = string.Format(StringConstants.TEXT_WORK_SERVE_HEAD, Task.Served + 1, Task.Orders.Count,
                order.Destination);

            var shelf = new VisualElement();
            shelf.AddToClassList("work-shelf");

            for (var i = 0; i < Task.Items.Length; i++)
            {
                var index = i;
                var item = new Button(() => AddToTray(index));
                item.text = Task.Items[i];
                item.AddToClassList("btn");
                item.AddToClassList("work-item");
                shelf.Add(item);
            }

            var tray = new Label(TrayText());
            tray.name = "work-tray";
            tray.AddToClassList("work-tray");

            var actions = new VisualElement();
            actions.AddToClassList("work-actions");
            var clear = new Button(ClearTray);
            clear.text = StringConstants.TEXT_WORK_CLEAR;
            clear.AddToClassList("popup-button");
            clear.AddToClassList("work-button");
            var serve = new Button(ServeTray);
            serve.text = StringConstants.TEXT_WORK_SERVE;
            serve.AddToClassList("popup-button");
            serve.AddToClassList("popup-button--main");
            serve.AddToClassList("work-button");
            actions.Add(clear);
            actions.Add(serve);

            Body.Add(shelf);
            Body.Add(tray);
            Body.Add(actions);
        }

        private void AddToTray(int index)
        {
            Tray[index]++;
            RefreshTray();
        }

        private void ClearTray()
        {
            Array.Clear(Tray, 0, Tray.Length);
            RefreshTray();
        }

        private void RefreshTray()
        {
            var label = Body.Q<Label>("work-tray");

            if (label != null)
            {
                label.text = TrayText();
            }
        }

        private void ServeTray()
        {
            var order = Task.Current;
            var right = Task.Serve(Tray);
            Message.text = right == true
                ? StringConstants.TEXT_WORK_RIGHT
                : string.Format(StringConstants.TEXT_WORK_WRONG, Describe(order, false));
            Array.Clear(Tray, 0, Tray.Length);
            ShowServe();
        }

        private void Finish()
        {
            Sloppy.SetEnabled(false);
            var handler = Finished;

            if (handler != null)
            {
                handler(Task.Accuracy);
            }
        }

        private void OnSkip()
        {
            var handler = Skipped;

            if (handler != null)
            {
                handler();
            }
        }

        private string TrayText()
        {
            var text = new StringBuilder(StringConstants.TEXT_WORK_TRAY).Append("  ·  ");
            var any = false;

            for (var i = 0; i < Tray.Length; i++)
            {
                if (Tray[i] <= 0)
                {
                    continue;
                }

                if (any == true)
                {
                    text.Append(", ");
                }

                text.Append(string.Format(StringConstants.TEXT_WORK_ITEM_COUNT, Task.Items[i], Tray[i]));
                any = true;
            }

            if (any == false)
            {
                text.Append(StringConstants.TEXT_WORK_TRAY_EMPTY);
            }

            return text.ToString();
        }

        // 주문표 한 장을 글로. withPlace면 행선지를 첫 줄에 둔다.
        private string Describe(WorkOrder order, bool withPlace)
        {
            var text = new StringBuilder();

            if (withPlace == true)
            {
                text.Append(order.Destination).Append('\n');
            }

            var any = false;

            for (var i = 0; i < order.Counts.Length; i++)
            {
                if (order.Counts[i] <= 0)
                {
                    continue;
                }

                if (any == true)
                {
                    text.Append(", ");
                }

                text.Append(string.Format(StringConstants.TEXT_WORK_ITEM_COUNT, Task.Items[i], order.Counts[i]));
                any = true;
            }

            return text.ToString();
        }
    }
}
