using System;
using Underwrite.Global;
using Underwrite.Village;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 엿듣기 화면. 여관 배경 위에 테이블 카드를 늘어놓고, 들은 말은 아래 쪽지에 쌓는다.
    /// 판정은 <see cref="EavesdropSession"/>이, 들은 말과 보상은 부른 쪽이 정한다.
    /// </summary>
    public sealed class UI_Eavesdrop
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly VisualElement TableRow;
        private readonly VisualElement Log;
        private readonly Label Turns;

        private EavesdropSession Session;

        /// <summary>테이블 곁에 머물기로 했다.</summary>
        public event Action<EavesdropSession.Table> Listened;

        /// <summary>그만둔다.</summary>
        public event Action Left;

        public bool IsOpen
        {
            get
            {
                return Overlay.parent != null;
            }
        }

        public UI_Eavesdrop(VisualElement root)
        {
            Root = root;
            Overlay = new VisualElement();
            Overlay.AddToClassList("work");
            Overlay.AddToClassList("dialogue-root--tavern");

            var panel = new VisualElement();
            panel.AddToClassList("popup");
            panel.AddToClassList("eaves-panel");

            var title = new Label(StringConstants.TEXT_EAVES_TITLE);
            title.AddToClassList("popup-title");
            var hint = new Label(StringConstants.TEXT_EAVES_HINT);
            hint.AddToClassList("work-hint");
            Turns = new Label();
            Turns.AddToClassList("work-head");

            TableRow = new VisualElement();
            TableRow.AddToClassList("eaves-tables");

            Log = new ScrollView();
            Log.AddToClassList("eaves-log");

            var leave = new Button(OnLeave);
            leave.text = StringConstants.TEXT_EAVES_LEAVE;
            leave.AddToClassList("popup-button");
            leave.AddToClassList("work-button");

            panel.Add(title);
            panel.Add(hint);
            panel.Add(Turns);
            panel.Add(TableRow);
            panel.Add(Log);
            panel.Add(leave);
            Overlay.Add(panel);
        }

        public void Open(EavesdropSession session)
        {
            Session = session;
            Log.Clear();

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
            Refresh();
        }

        public void Close()
        {
            Overlay.RemoveFromHierarchy();
        }

        /// <summary>들은 말을 쪽지에 더한다.</summary>
        public void AddLine(string text)
        {
            var line = new Label(text);
            line.AddToClassList("eaves-line");
            Log.Add(line);
        }

        /// <summary>테이블 카드와 남은 기회를 다시 그린다.</summary>
        public void Refresh()
        {
            Turns.text = string.Format(StringConstants.TEXT_EAVES_TURNS, Session.TurnsLeft);
            TableRow.Clear();

            for (var i = 0; i < Session.Tables.Count; i++)
            {
                var table = Session.Tables[i];
                var card = new VisualElement();
                card.AddToClassList("eaves-table");

                var name = new Label(table.Name);
                name.AddToClassList("eaves-table-name");
                var mood = new Label(EavesdropSession.MoodOf(table));
                mood.AddToClassList("eaves-table-mood");
                var count = new Label(table.IsDone == true
                    ? StringConstants.TEXT_EAVES_TABLE_DONE
                    : string.Format(StringConstants.TEXT_EAVES_LISTENED, table.Listens));
                count.AddToClassList("work-hint");

                var listen = new Button(() => OnListen(table));
                listen.text = StringConstants.TEXT_EAVES_LISTEN;
                listen.AddToClassList("popup-button");
                listen.AddToClassList("eaves-listen");
                listen.SetEnabled(Session.IsOver == false && table.IsDone == false);

                card.Add(name);
                card.Add(mood);
                card.Add(count);
                card.Add(listen);
                TableRow.Add(card);
            }
        }

        private void OnListen(EavesdropSession.Table table)
        {
            var handler = Listened;

            if (handler != null)
            {
                handler(table);
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
    }
}
