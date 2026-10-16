using System;

namespace Underwrite.Characters
{
    /// <summary>
    /// 영지의 자리에 들어가고 나간 것을 알린다.
    ///
    /// 자리(VillageSpot)가 화면(UI)을 직접 알면 둘이 묶여 버린다.
    /// 자리는 알리기만 하고, 무엇을 띄울지는 화면이 정한다.
    /// </summary>
    public static class VillageSpotEvents
    {
        public static event Action<VillageSpot> Entered;
        public static event Action<VillageSpot> Exited;

        internal static void RaiseEnter(VillageSpot spot)
        {
            var handler = Entered;

            if (handler != null)
            {
                handler(spot);
            }
        }

        internal static void RaiseExit(VillageSpot spot)
        {
            var handler = Exited;

            if (handler != null)
            {
                handler(spot);
            }
        }
    }
}
