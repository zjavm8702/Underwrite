using System;
using System.Collections.Generic;
using Underwrite.Global;

namespace Underwrite.Market
{
    /// <summary>한 종목의 보유 지분과 평균 매입가.</summary>
    public sealed class Holding
    {
        public string StockId { get; private set; }
        public int Quantity { get; private set; }

        /// <summary>평균 매입가. 매수할 때마다 가중 평균으로 갱신한다.</summary>
        public float AverageCost { get; private set; }

        public Holding(string stockId)
        {
            StockId = stockId;
        }

        internal void Add(int quantity, float price)
        {
            var total = AverageCost * Quantity + price * quantity;
            Quantity += quantity;

            if (Quantity > 0)
            {
                AverageCost = total / Quantity;
            }
        }

        /// <summary>매도한다. 실현 손익을 돌려준다.</summary>
        internal float Remove(int quantity, float price)
        {
            if (quantity > Quantity)
            {
                throw new InvalidOperationException("Cannot sell more shares than held.");
            }

            var realized = (price - AverageCost) * quantity;
            Quantity -= quantity;

            if (Quantity == 0)
            {
                AverageCost = 0f;
            }

            return realized;
        }
    }

    /// <summary>
    /// 플레이어의 현금과 보유 지분. 기획서 §9에 따라 시장 거래와 후원 지출을 분리해 기록한다.
    /// 시제품에서는 배당과 청산을 다루지 않고 시세 차익만 계산한다.
    /// </summary>
    public sealed class Portfolio
    {
        private readonly Dictionary<string, Holding> Holdings = new Dictionary<string, Holding>();

        /// <summary>현금.</summary>
        public float Cash { get; private set; }

        /// <summary>실현 손익 누계. 매도할 때만 쌓인다.</summary>
        public float RealizedProfit { get; private set; }

        /// <summary>후원으로 지출한 누계. 시장 거래와 분리해 기록한다.</summary>
        public float PatronageSpent { get; private set; }

        public Portfolio(float startingCash)
        {
            if (startingCash < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(startingCash), "Starting cash must not be negative.");
            }

            Cash = startingCash;
        }

        public IReadOnlyDictionary<string, Holding> AllHoldings
        {
            get
            {
                return Holdings;
            }
        }

        public int QuantityOf(string stockId)
        {
            Holding holding;

            if (Holdings.TryGetValue(stockId, out holding) == false)
            {
                return 0;
            }

            return holding.Quantity;
        }

        public float AverageCostOf(string stockId)
        {
            Holding holding;

            if (Holdings.TryGetValue(stockId, out holding) == false)
            {
                return 0f;
            }

            return holding.AverageCost;
        }

        /// <summary>
        /// 보유 지분의 현재 평가액. 종목 수가 적으므로 매 호출 계산해도 되지만,
        /// 매 틱 호출하는 경로에서는 결과를 캐싱해 쓴다.
        /// </summary>
        public float HoldingsValue(IReadOnlyDictionary<string, AdventurerStock> stocks)
        {
            if (stocks == null)
            {
                throw new ArgumentNullException(nameof(stocks));
            }

            var total = 0f;

            foreach (var pair in Holdings)
            {
                AdventurerStock stock;

                if (stocks.TryGetValue(pair.Key, out stock) == false)
                {
                    continue;
                }

                total += stock.Price * pair.Value.Quantity;
            }

            return total;
        }

        /// <summary>현금과 평가액의 합.</summary>
        public float TotalValue(IReadOnlyDictionary<string, AdventurerStock> stocks)
        {
            return Cash + HoldingsValue(stocks);
        }

        internal bool CanAfford(float amount)
        {
            return Cash >= amount;
        }

        internal void Buy(string stockId, int quantity, float price)
        {
            var cost = price * quantity;

            if (CanAfford(cost) == false)
            {
                throw new InvalidOperationException("Not enough cash to complete the purchase.");
            }

            Cash -= cost;

            Holding holding;

            if (Holdings.TryGetValue(stockId, out holding) == false)
            {
                holding = new Holding(stockId);
                Holdings.Add(stockId, holding);
            }

            holding.Add(quantity, price);
        }

        internal void Sell(string stockId, int quantity, float price)
        {
            Holding holding;

            if (Holdings.TryGetValue(stockId, out holding) == false)
            {
                throw new InvalidOperationException("No holding exists for the requested stock.");
            }

            RealizedProfit += holding.Remove(quantity, price);
            Cash += price * quantity;

            if (holding.Quantity == 0)
            {
                Holdings.Remove(stockId);
            }
        }

        /// <summary>
        /// 후원 지출. 시장 거래가 아니므로 보유 지분을 바꾸지 않고 현금만 줄인다. 기획서 §9.
        /// </summary>
        public bool SpendPatronage(float amount)
        {
            if (amount <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Patronage amount must be positive.");
            }

            if (CanAfford(amount) == false)
            {
                return false;
            }

            Cash -= amount;
            PatronageSpent += amount;
            return true;
        }
    }
}
