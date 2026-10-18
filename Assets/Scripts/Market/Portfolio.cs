using System;
using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Save;

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
        internal void Restore(int quantity, float averageCost)
        {
            Quantity = quantity;
            AverageCost = averageCost;
        }

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

        /// <summary>
        /// 지정가 매수 주문이 묶어 둔 현금. 아직 쓰이지는 않았지만 다른 주문에 쓸 수도 없다.
        /// 이걸 두지 않으면 같은 돈으로 주문을 무한히 걸 수 있다.
        /// </summary>
        public float ReservedCash { get; private set; }

        /// <summary>지정가 매도 주문이 묶어 둔 지분. 종목별 수량이다.</summary>
        private readonly Dictionary<string, int> ReservedShares = new Dictionary<string, int>();

        /// <summary>실제로 쓸 수 있는 현금. 묶인 돈을 뺀 값이다.</summary>
        public float AvailableCash
        {
            get
            {
                return Cash - ReservedCash;
            }
        }

        /// <summary>실제로 팔 수 있는 지분. 묶인 수량을 뺀 값이다.</summary>
        public int AvailableQuantityOf(string stockId)
        {
            int reserved;

            if (ReservedShares.TryGetValue(stockId, out reserved) == false)
            {
                reserved = 0;
            }

            return QuantityOf(stockId) - reserved;
        }

        internal bool ReserveCash(float amount)
        {
            if (AvailableCash < amount)
            {
                return false;
            }

            ReservedCash += amount;
            return true;
        }

        internal void ReleaseCash(float amount)
        {
            ReservedCash -= amount;

            if (ReservedCash < 0f)
            {
                ReservedCash = 0f;
            }
        }

        internal bool ReserveShares(string stockId, int quantity)
        {
            if (AvailableQuantityOf(stockId) < quantity)
            {
                return false;
            }

            int current;
            ReservedShares.TryGetValue(stockId, out current);
            ReservedShares[stockId] = current + quantity;
            return true;
        }

        internal void ReleaseShares(string stockId, int quantity)
        {
            int current;

            if (ReservedShares.TryGetValue(stockId, out current) == false)
            {
                return;
            }

            var next = current - quantity;

            if (next <= 0)
            {
                ReservedShares.Remove(stockId);
                return;
            }

            ReservedShares[stockId] = next;
        }

        public Portfolio(float startingCash)
        {
            if (startingCash < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(startingCash), "Starting cash must not be negative.");
            }

            Cash = startingCash;
        }

        /// <summary>저장할 값. 장 사이에만 저장하므로 묶인 돈과 주식(지정가 대기)은 없다.</summary>
        internal WalletState Capture()
        {
            var state = new WalletState
            {
                Cash = Cash,
                RealizedProfit = RealizedProfit,
                PatronageSpent = PatronageSpent
            };

            foreach (var pair in Holdings)
            {
                if (pair.Value.Quantity <= 0)
                {
                    continue;
                }

                state.Holdings.Add(new HoldingState
                {
                    StockId = pair.Key,
                    Quantity = pair.Value.Quantity,
                    AverageCost = pair.Value.AverageCost
                });
            }

            return state;
        }

        internal void Restore(WalletState state)
        {
            Cash = state.Cash;
            RealizedProfit = state.RealizedProfit;
            PatronageSpent = state.PatronageSpent;
            ReservedCash = 0f;
            ReservedShares.Clear();
            Holdings.Clear();

            for (var i = 0; i < state.Holdings.Count; i++)
            {
                var row = state.Holdings[i];
                var holding = new Holding(row.StockId);
                holding.Restore(row.Quantity, row.AverageCost);
                Holdings.Add(row.StockId, holding);
            }
        }

        /// <summary>한 주라도 가진 첫 종목. 없으면 null. 구체 사전을 돌아 매 프레임 불러도 할당이 없다.</summary>
        public string FirstHeldStock()
        {
            foreach (var pair in Holdings)
            {
                if (pair.Value.Quantity > 0)
                {
                    return pair.Key;
                }
            }

            return null;
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

        /// <summary>묶이지 않은 현금으로 살 수 있는지. 예약분은 쓸 수 없다.</summary>
        internal bool CanAfford(float amount)
        {
            return AvailableCash >= amount;
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
        /// 첫 지하 방문의 연습 매수·매도 뒤(2026-10-18). 현금을 들어왔을 때 금액으로 되돌리고 실현 손익을 지운다 —
        /// 연습에서 생긴 몇 푼의 차이로 첫 판이 기울지 않게. 가진 주식이 없을 때만 부른다.
        /// </summary>
        public void ResetAfterPractice(float cash)
        {
            Cash = cash;
            RealizedProfit = 0f;
        }

        /// <summary>
        /// 거래 밖에서 들어온 돈. 영지의 퀘스트로 번 종잣돈이 여기로 들어온다. 기획서 §10.
        /// 매매 손익과 섞지 않는다. 실현손익은 거래로 번 것만 세야 한다.
        /// </summary>
        public void Earn(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            Cash += amount;
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
