namespace Underwrite.Data
{
    /// <summary>주문서 읽기의 주문서 한 장. 대장간 꽂이에 날짜순으로 꽂혀 있다.</summary>
    public sealed class SmithSlip
    {
        /// <summary>시장의 종목 아이디. 꽂이를 채우려고 넣은 이름 없는 무리는 비어 있다.</summary>
        public string BandId;
        public string BandName;
        public string Item;

        /// <summary>이번 계절 며칠째의 주문인가. 0이면 지난 계절 주문이다.</summary>
        public int Day;

        /// <summary><see cref="Global.StringConstants.TEXT_SMITH_STATUS"/>의 순번. 마지막 칸이 「취소」다.</summary>
        public int Status;
    }
}
