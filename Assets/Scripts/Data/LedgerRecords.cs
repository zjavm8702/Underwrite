namespace Underwrite.Data
{
    /// <summary>장부 맞추기의 왼쪽 장. 길드가 단에 맡긴 의뢰와 약정한 보수.</summary>
    public sealed class LedgerRequest
    {
        /// <summary>시장의 종목 아이디. 장부를 채우려고 넣은 이름 없는 무리는 비어 있다.</summary>
        public string BandId;
        public string BandName;
        public string Job;
        public int Reward;
    }

    /// <summary>장부 맞추기의 오른쪽 장. 실제로 나간 돈 한 줄. 한 의뢰의 보수가 여러 줄로 쪼개져 나간다.</summary>
    public sealed class LedgerPayment
    {
        public int Day;
        public string BandName;
        public int Amount;

        /// <summary>선금·중도금·잔금·완수.</summary>
        public string Note;
    }
}
