using System;
using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Market
{
    /// <summary>
    /// 표에서 시장 한 판을 짠다. 거래소 화면이 처음 열릴 때와 저장을 불러올 때가 같은 길을 탄다.
    /// 장은 열지 않는다 — 언제 어느 장을 열지는 부르는 쪽이 정한다.
    /// </summary>
    public static class MarketFactory
    {
        public static MarketSimulation Create(GameDataSet data, int seed, MarketSentiment sentiment)
        {
            var sim = new MarketSimulation(seed, Constants.STARTING_CASH);
            var stocks = BuildStocks(sim, data);

            for (var i = 0; i < stocks.Count; i++)
            {
                sim.AddStock(stocks[i]);
            }

            // 용사단 종목. 용병단과 달리 표에서 오지 않는다 — 이야기와 묶인 하나뿐인 종목이다.
            sim.AddStock(new AdventurerStock(Constants.HERO_STOCK_ID, StringConstants.TEXT_HERO_STOCK_NAME,
                StockPersonality.Personality_Index, Constants.HERO_INDEX_VALUE, Constants.HERO_INDEX_LIQUIDITY));

            RegisterDepartures(sim, data);
            sim.AddEvents(BuildEvents(data));
            sim.SetSentiment(sentiment);
            return sim;
        }

        /// <summary>
        /// 거래 대상을 표에서 만든다. 표가 없으면 코드에 박힌 프로토타입을 쓴다.
        /// 표를 물리지 않았다고 화면이 비면 무엇이 잘못됐는지 알기 어렵다.
        /// </summary>
        private static List<AdventurerStock> BuildStocks(MarketSimulation sim, GameDataSet data)
        {
            if (data == null || data.StockRows == null || data.StockRows.Rows.Count == 0)
            {
                return PrototypeScenario.CreateStocks();
            }

            var rows = data.StockRows.Rows;
            var list = new List<AdventurerStock>(rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var stock = new AdventurerStock(row.Id, row.DisplayName, row.Personality,
                    row.TrueValue, row.Liquidity);
                stock.SetCodexProfile(Array.IndexOf(StringConstants.CODEX_STOCK_IDS, row.Id));

                // 나중에 올라올 종목은 대기열로 간다. 첫 주기에 전부 상장하면 자리가 넘친다.
                if (row.ListCycle > 1)
                {
                    sim.AddWaiting(stock, row.ListCycle);
                    sim.PlanDeparture(row.Id, row.DelistCycle, row.DelistReason);
                    continue;
                }

                list.Add(stock);
            }

            return list;
        }

        /// <summary>떠날 주기와 사유를 표에서 읽어 등록한다. 청산율은 상장 시점부터 보인다.</summary>
        private static void RegisterDepartures(MarketSimulation sim, GameDataSet data)
        {
            if (data == null || data.StockRows == null)
            {
                return;
            }

            var rows = data.StockRows.Rows;

            for (var i = 0; i < rows.Count; i++)
            {
                sim.PlanDeparture(rows[i].Id, rows[i].DelistCycle, rows[i].DelistReason);
            }
        }

        private static List<MarketEventStep> BuildEvents(GameDataSet data)
        {
            if (data == null || data.EventRows == null || data.EventRows.Rows.Count == 0)
            {
                return PrototypeScenario.CreateEvents();
            }

            var rows = data.EventRows.Rows;
            var list = new List<MarketEventStep>(rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                list.Add(new MarketEventStep(row.Slot, row.StockId, row.Stage, row.Headline,
                    row.Source, row.Visibility, row.ExpectationDelta, row.ValueDelta));
            }

            return list;
        }
    }
}
