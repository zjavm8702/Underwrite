using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Story
{
    /// <summary>
    /// 영지의 행동력(2026-10-19 사장님 — 영지에서는 시간 대신 턴제 행동력, 시간은 거래할 때만 쓴다).
    ///
    /// 휴장 하나(오전 · 오후 · 새벽)마다 정해진 만큼 받는다. 일·미니게임·돈 내고 묻기·모험단 말 걸기에 쓰고,
    /// 걷기·용사단 대화·이야기·익명 의뢰는 공짜다. 다 쓰면 「장으로 간다」(오전·오후) 또는 잠(새벽)으로 넘긴다.
    /// 영지 시계는 휴장 동안 흐르지 않으므로 휴장마다 시각이 하나로 정해져 있다 — 그 날짜·시각을 열쇠로 한 번만 채운다.
    /// </summary>
    public static class ActionPoints
    {
        public static int Current { get; private set; }

        /// <summary>마지막으로 채운 휴장의 열쇠(날 × 10000 + 휴장 시각). 저장한다.</summary>
        public static int FilledKey { get; private set; } = -1;

        public static void Reset()
        {
            Current = 0;
            FilledKey = -1;
        }

        public static void Restore(int current, int filledKey)
        {
            Current = current;
            FilledKey = filledKey;
        }

        /// <summary>그 시각이 어느 휴장인가. 영지의 낮·밤도 이 셋으로 바뀐다.</summary>
        public static VillagePhase PhaseOf(int breakMinuteOfDay)
        {
            var minute = breakMinuteOfDay % (24 * 60);

            if (minute >= Constants.MARKET_MORNING_START_HOUR * 60 + Constants.MARKET_SESSION_MINUTES
                && minute < Constants.MARKET_EVENING_START_HOUR * 60)
            {
                return VillagePhase.Phase_Afternoon;
            }

            if (minute >= Constants.VILLAGE_WAKE_HOUR * 60 && minute < Constants.MARKET_MORNING_START_HOUR * 60)
            {
                return VillagePhase.Phase_Morning;
            }

            return VillagePhase.Phase_Night;
        }

        public static int MaxOf(VillagePhase phase)
        {
            switch (phase)
            {
                case VillagePhase.Phase_Morning:
                {
                    return Constants.AP_MORNING;
                }
                case VillagePhase.Phase_Afternoon:
                {
                    return Constants.AP_AFTERNOON;
                }
                default:
                {
                    return Constants.AP_NIGHT;
                }
            }
        }

        /// <summary>
        /// 새 휴장이면 채운다. 채웠으면 참. <paramref name="locked"/>면 0으로 채운다(행동력 안내를 보기 전 — 첫날 밤).
        /// </summary>
        public static bool FillIfNew(int day, int breakMinuteOfDay, bool locked)
        {
            var key = day * 10000 + breakMinuteOfDay;

            if (key == FilledKey)
            {
                return false;
            }

            FilledKey = key;
            Current = locked == true ? 0 : MaxOf(PhaseOf(breakMinuteOfDay));
            return true;
        }

        /// <summary>지금 휴장의 몫으로 다시 채운다(행동력 안내를 마친 뒤).</summary>
        public static void Refill(int breakMinuteOfDay)
        {
            Current = MaxOf(PhaseOf(breakMinuteOfDay));
        }

        public static bool Has(int cost)
        {
            return Current >= cost;
        }

        /// <summary>쓴다. 모자라면 쓰지 않고 거짓.</summary>
        public static bool Spend(int cost)
        {
            if (Current < cost)
            {
                return false;
            }

            Current -= cost;
            return true;
        }
    }
}
