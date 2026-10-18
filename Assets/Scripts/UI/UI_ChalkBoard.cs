using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Market;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 지하 방 벽의 주식판(칠판)에 쓴 분필 글씨(2026-10-18 교차 검수 — 빈 격자로는 거래판으로 읽히지 않는다).
    /// 칠판 그림의 글자 칸(월드 사각형)에 맞춰 화면 위에 종목·현재가·등락을 띄운다. 값은 몇 초마다만 다시 쓴다.
    /// </summary>
    public sealed class UI_ChalkBoard
    {
        private readonly VisualElement Board;
        private readonly List<VisualElement> Rows = new List<VisualElement>();
        private readonly List<Label> Names = new List<Label>();
        private readonly List<Label> Prices = new List<Label>();
        private readonly List<Label> Changes = new List<Label>();
        private float RefreshLeft;

        // 이보다 작게 움직이면 0.0%로 보이므로 화살표·색을 붙이지 않는다.
        private const float CHALK_FLAT_RATIO = 0.0005f;

        public UI_ChalkBoard(VisualElement root)
        {
            Board = new VisualElement();
            Board.AddToClassList("chalk-board");
            Board.pickingMode = PickingMode.Ignore;
            root.Add(Board);
            Board.SendToBack();

            // 칠판 격자는 다섯 칸이다 — 이름이 앞 세 칸, 현재가와 등락이 한 칸씩.
            for (var i = 0; i < Constants.CHALK_ROWS; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("chalk-row");
                row.pickingMode = PickingMode.Ignore;
                Board.Add(row);
                Rows.Add(row);
                Names.Add(AddCell(row, "chalk-cell--name"));
                Prices.Add(AddCell(row, "chalk-cell--price"));
                Changes.Add(AddCell(row, "chalk-cell--change"));
            }
        }

        private static Label AddCell(VisualElement row, string className)
        {
            var cell = new Label();
            cell.AddToClassList("chalk-cell");
            cell.AddToClassList(className);
            cell.pickingMode = PickingMode.Ignore;
            row.Add(cell);
            return cell;
        }

        /// <summary>칠판 글자 칸에 맞춰 자리를 잡고, 때가 되면 값을 다시 쓴다.</summary>
        /// <param name="center">글자 칸 가운데(월드).</param>
        /// <param name="size">글자 칸 크기(월드 유닛).</param>
        public void Tick(MarketSimulation sim, Vector3 center, Vector2 size, bool hidden, float delta)
        {
            var camera = Camera.main;
            Board.style.display = hidden == true || camera == null || sim == null ? DisplayStyle.None : DisplayStyle.Flex;

            if (hidden == true || camera == null || sim == null || Board.panel == null)
            {
                return;
            }

            var topLeft = ToPanel(camera, center + new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f));
            var bottomRight = ToPanel(camera, center + new Vector3(size.x * 0.5f, -size.y * 0.5f, 0f));
            Board.style.left = topLeft.x;
            Board.style.top = topLeft.y;
            Board.style.width = bottomRight.x - topLeft.x;
            Board.style.height = bottomRight.y - topLeft.y;

            RefreshLeft -= delta;

            if (RefreshLeft > 0f)
            {
                return;
            }

            RefreshLeft = Constants.CHALK_REFRESH_SECONDS;
            var shown = 0;
            var stocks = sim.Stocks;

            for (var i = 0; i < stocks.Count && shown < Rows.Count; i++)
            {
                var stock = stocks[i];

                if (stock.IsTradable == false)
                {
                    continue;
                }

                var change = stock.SessionChangeRatio;
                var rising = change > CHALK_FLAT_RATIO;
                var falling = change < -CHALK_FLAT_RATIO;
                var arrow = StringConstants.TEXT_CHALK_FLAT;

                if (rising == true)
                {
                    arrow = StringConstants.TEXT_CHALK_UP;
                }
                else if (falling == true)
                {
                    arrow = StringConstants.TEXT_CHALK_DOWN;
                }

                var row = Rows[shown];
                Names[shown].text = stock.DisplayName;
                Prices[shown].text = string.Format(StringConstants.TEXT_CHALK_PRICE, stock.Price);
                Changes[shown].text = string.Format(StringConstants.TEXT_CHALK_CHANGE, arrow, Mathf.Abs(change) * 100f);
                row.EnableInClassList("chalk-row--up", rising);
                row.EnableInClassList("chalk-row--down", falling);
                row.style.display = DisplayStyle.Flex;
                shown++;
            }

            for (var i = shown; i < Rows.Count; i++)
            {
                Rows[i].style.display = DisplayStyle.None;
            }
        }

        private Vector2 ToPanel(Camera camera, Vector3 world)
        {
            var screen = camera.WorldToScreenPoint(world);
            return RuntimePanelUtils.ScreenToPanel(Board.panel, new Vector2(screen.x, Screen.height - screen.y));
        }
    }
}
