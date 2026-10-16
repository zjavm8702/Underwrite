using Underwrite.Enumeration;

namespace Underwrite.Market
{
    /// <summary>
    /// 대기 중인 지정가 주문.
    ///
    /// 매수 지정가는 가격이 지정가 **이하**로 내려올 때, 매도 지정가는 **이상**으로 올라갈 때 체결된다.
    /// 체결가는 지정가 그대로다. 시장가와 달리 유리한 가격이 보장되는 대신 체결 자체가 보장되지 않는다.
    ///
    /// 이 구조가 생기면 기획서 §7의 급락 구간이 기회가 된다. 소문이 꺾여 가격이 무너질 때
    /// 아래에 걸어 둔 매수가 체결된다. 그러려면 폭락을 **미리 예상해야** 한다.
    /// </summary>
    public sealed class PendingOrder
    {
        private static int NextId = 1;

        public int Id { get; private set; }
        public string StockId { get; private set; }
        public OrderSide Side { get; private set; }

        /// <summary>지정가. 호가 단위에 맞춰 다듬어진 값이다.</summary>
        public float LimitPrice { get; private set; }

        public int Quantity { get; private set; }
        public PendingState State { get; private set; } = PendingState.Pending_Waiting;

        /// <summary>주문을 건 틱. 목록에서 순서를 보여줄 때 쓴다.</summary>
        public int PlacedTick { get; private set; }

        /// <summary>매수 주문이 묶어 둔 현금. 취소하거나 체결하면 풀린다.</summary>
        public float ReservedCash { get; private set; }

        public PendingOrder(string stockId, OrderSide side, float limitPrice, int quantity,
            int placedTick, float reservedCash)
        {
            Id = NextId++;
            StockId = stockId;
            Side = side;
            LimitPrice = limitPrice;
            Quantity = quantity;
            PlacedTick = placedTick;
            ReservedCash = reservedCash;
        }

        /// <summary>현재가가 이 주문을 체결시키는지.</summary>
        public bool ShouldFill(float marketPrice)
        {
            if (State != PendingState.Pending_Waiting)
            {
                return false;
            }

            if (Side == OrderSide.Side_Buy)
            {
                return marketPrice <= LimitPrice;
            }

            return marketPrice >= LimitPrice;
        }

        internal void MarkFilled()
        {
            State = PendingState.Pending_Filled;
            ReservedCash = 0f;
        }

        internal void MarkCancelled()
        {
            State = PendingState.Pending_Cancelled;
            ReservedCash = 0f;
        }

        /// <summary>폐장 시 남은 주문은 사라진다. 기획서 §11의 장 단위 진행에 맞춘다.</summary>
        internal void MarkExpired()
        {
            State = PendingState.Pending_ExpiredAtClose;
            ReservedCash = 0f;
        }
    }
}
