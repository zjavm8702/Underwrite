using Underwrite.Enumeration;

namespace Underwrite.Data
{
    /// <summary>
    /// 용사단의 사정 하나. 금액이 아니라 소문으로 드러난다(상장흐름 설계안 §1-5).
    ///
    /// 플레이어는 여관에서 엿들은 말로 무엇이 필요한지 짐작하고 길드에 익명 의뢰를 낸다.
    /// 기한은 보여 주고, 무엇이 필요한지는 말로만 전한다.
    /// </summary>
    public sealed class HeroNeed
    {
        public string Id;

        /// <summary>실제로 필요한 것. 플레이어에게는 직접 보여 주지 않는다.</summary>
        public RequestPurpose Purpose;

        /// <summary>며칠째에 일어나는가.</summary>
        public int RevealDay;

        /// <summary>일어난 날부터 며칠 안에 해결해야 하는가.</summary>
        public int DeadlineDays;

        /// <summary>여관에서 엿듣는 말.</summary>
        public string Rumor;

        /// <summary>맞는 의뢰를 받은 다음 날 여관에서 하는 말.</summary>
        public string ReactionMet;

        public NeedState State = NeedState.State_Hidden;

        /// <summary>플레이어가 이 사정을 들었는가.</summary>
        public bool Heard;

        /// <summary>이 날이 끝날 때까지 해결하지 못하면 실패다.</summary>
        public int LastDay
        {
            get
            {
                return RevealDay + DeadlineDays - 1;
            }
        }
    }
}
