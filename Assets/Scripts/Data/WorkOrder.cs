namespace Underwrite.Data
{
    /// <summary>주문 나르기의 주문표 한 장. 어디로, 무엇을 몇 개.</summary>
    public sealed class WorkOrder
    {
        public string Destination;

        /// <summary>물건마다 몇 개인가. 순서는 그 일의 물건 목록과 같다.</summary>
        public int[] Counts;
    }
}
