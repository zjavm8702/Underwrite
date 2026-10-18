using System;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 영지의 수첩(2026-10-17 사용자 요청). 영지에서 알아낸 것을 모아 본다 — 들은 소식과 모험단 도감 두 갈래.
    /// 처음 열 때 한 번 만들고, 닫으면 숨긴다(2026-10-19 정리 — 영지 화면에서 떼어 냄).
    /// </summary>
    public sealed class UI_VillageNotebook
    {
        private readonly VisualElement Root;
        private readonly Func<MarketSimulation, string, string> NameOf;

        private VisualElement Book;
        private Button NewsTab;
        private Button BandsTab;
        private ScrollView List;

        /// <param name="nameOf">종목 표시 이름(시장에 없으면 표에서 찾는다).</param>
        public UI_VillageNotebook(VisualElement root, Func<MarketSimulation, string, string> nameOf)
        {
            Root = root;
            NameOf = nameOf;
        }

        public bool IsOpen
        {
            get
            {
                return Book != null && Book.style.display != DisplayStyle.None;
            }
        }

        public void Open(bool bands)
        {
            if (Book == null)
            {
                Build();
            }

            Book.style.display = DisplayStyle.Flex;
            Book.BringToFront();
            NewsTab.EnableInClassList("btn--selected", bands == false);
            BandsTab.EnableInClassList("btn--selected", bands == true);

            List.Clear();
            var sim = BreakClock.Simulation;

            if (bands == true)
            {
                var subjects = BandCodex.Subjects();

                for (var i = 0; i < subjects.Count; i++)
                {
                    var id = subjects[i];
                    List.Add(CodexCards.Build(sim, id, NameOf(sim, id)));
                }

                return;
            }

            // 이번에 영지에서 캔 것(가장 최근부터), 그 앞의 나만 아는 소식들.
            var shown = 0;
            var gathered = GatherSession.Gathered;

            for (var i = gathered.Count - 1; i >= 0; i--)
            {
                List.Add(NewsCard(sim, gathered[i].StockId, gathered[i].Headline));
                shown++;
            }

            if (sim != null)
            {
                var log = sim.InfoLog;

                for (var i = log.Count - 1; i >= 0 && shown < Constants.NOTEBOOK_MAX_ROWS; i--)
                {
                    if (log[i].IsPublic == true)
                    {
                        continue;
                    }

                    List.Add(NewsCard(sim, log[i].StockId, log[i].Headline));
                    shown++;
                }
            }

            if (shown == 0)
            {
                var empty = new Label(StringConstants.TEXT_NOTEBOOK_EMPTY);
                empty.AddToClassList("t-note");
                List.Add(empty);
            }
        }

        private void Build()
        {
            Book = new VisualElement();
            Book.name = "notebook";
            Book.AddToClassList("notebook");

            var panel = new VisualElement();
            panel.AddToClassList("popup");
            panel.AddToClassList("notebook-panel");

            var title = new Label(StringConstants.TEXT_NOTEBOOK);
            title.AddToClassList("popup-title");

            var tabs = new VisualElement();
            tabs.AddToClassList("notebook-tabs");
            NewsTab = new Button(() => Open(false));
            NewsTab.text = StringConstants.TEXT_NOTEBOOK_NEWS;
            NewsTab.AddToClassList("btn");
            BandsTab = new Button(() => Open(true));
            BandsTab.text = StringConstants.TEXT_CODEX_TAB;
            BandsTab.AddToClassList("btn");
            tabs.Add(NewsTab);
            tabs.Add(BandsTab);

            List = new ScrollView();
            List.AddToClassList("notebook-list");

            var close = new Button(() => Book.style.display = DisplayStyle.None);
            close.text = StringConstants.TEXT_NOTEBOOK_CLOSE;
            close.AddToClassList("popup-button");
            close.AddToClassList("popup-button--main");

            panel.Add(title);
            panel.Add(tabs);
            panel.Add(List);
            panel.Add(close);
            Book.Add(panel);
            Root.Add(Book);
        }

        private VisualElement NewsCard(MarketSimulation sim, string stockId, string headline)
        {
            var card = new VisualElement();
            card.AddToClassList("info-card");
            card.AddToClassList("info-card--private");

            var held = sim == null ? 0 : sim.Wallet.QuantityOf(stockId);
            card.EnableInClassList("info-card--held", held > 0);

            var line = new VisualElement();
            line.AddToClassList("info-line");
            var name = new Label(NameOf(sim, stockId));
            name.AddToClassList("info-stock");
            line.Add(name);

            if (held > 0)
            {
                var badge = new Label(string.Format(StringConstants.TEXT_INFO_HELD, held));
                badge.AddToClassList("info-held");
                line.Add(badge);
            }

            var text = new Label(headline);
            text.AddToClassList("info-headline");
            card.Add(line);
            card.Add(text);
            return card;
        }
    }
}
