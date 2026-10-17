using Underwrite.Global;

namespace Underwrite.Utility
{
    /// <summary>
    /// 게임 시각을 「PM 1:05:30」처럼 적는다(2026-10-17 사용자 요청 — 24시 표기 대신 AM/PM).
    /// 화면마다 따로 적으면 같은 시각이 다르게 보인다. 한곳에서 만든다.
    /// </summary>
    public static class ClockText
    {
        /// <summary>하루 기준 초(24시를 넘어도 된다)를 적는다.</summary>
        public static string FromSeconds(int totalSeconds, bool withSeconds)
        {
            var day = ((totalSeconds % 86400) + 86400) % 86400;
            var hour = day / 3600;
            var minute = day / 60 % 60;
            var second = day % 60;
            var mark = hour < 12 ? StringConstants.TEXT_CLOCK_AM : StringConstants.TEXT_CLOCK_PM;
            var shown = hour % 12 == 0 ? 12 : hour % 12;

            return withSeconds == true
                ? $"{mark} {shown}:{minute:00}:{second:00}"
                : $"{mark} {shown}:{minute:00}";
        }

        /// <summary>하루 기준 분(24시를 넘어도 된다)을 적는다.</summary>
        public static string FromMinutes(int totalMinutes)
        {
            return FromSeconds(totalMinutes * 60, false);
        }
    }
}
