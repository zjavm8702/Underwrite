using NUnit.Framework;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;

namespace Underwrite.Editor.Tests
{
    /// <summary>계절 달력과 장 순서(시간구조 설계안 §2, 2026-10-18 계절 4일·겨울 2일).</summary>
    public sealed class SeasonCalendarTests
    {
        [Test]
        public void DaysMapToSeasonsWithoutGapsOrOverlaps()
        {
            var expectedSeason = 1;
            var expectedDay = 1;

            for (var day = 1; day <= 400; day++)
            {
                Assert.AreEqual(expectedSeason, MarketClock.SeasonOf(day), $"day {day}");
                Assert.AreEqual(expectedDay, MarketClock.DayInSeason(day), $"day {day}");
                Assert.AreEqual(MarketClock.SeasonStartDay(expectedSeason) + expectedDay - 1, day, $"day {day}");

                expectedDay++;

                if (expectedDay > MarketClock.DaysInSeason(expectedSeason))
                {
                    expectedSeason++;
                    expectedDay = 1;
                }
            }
        }

        [Test]
        public void WinterIsShortAndHasNoInspection()
        {
            Assert.AreEqual(Constants.WINTER_SEASON_DAYS, MarketClock.DaysInSeason(4));
            Assert.AreEqual(Constants.SEASON_DAYS, MarketClock.DaysInSeason(5));

            var winterStart = MarketClock.SeasonStartDay(4);

            for (var day = winterStart; day < winterStart + Constants.WINTER_SEASON_DAYS; day++)
            {
                Assert.IsFalse(MarketClock.IsInspectionDay(day), $"winter day {day}");
                Assert.IsFalse(MarketClock.IsHeroDayOff(day), $"winter day {day} — 겨울에는 쉬는 날이 없다");
            }

            Assert.IsTrue(MarketClock.IsHeroDayOff(MarketClock.SeasonStartDay(5) + Constants.HERO_DAY_OFF_SEASON_DAY - 1));
        }

        [Test]
        public void SeasonFirstDayHasNoMorningSession()
        {
            int nextDay;

            Assert.AreEqual(MarketSession.Session_Evening,
                MarketClock.NextSessionAfter(MarketSession.Session_Morning, 2, out nextDay));
            Assert.AreEqual(2, nextDay);

            Assert.AreEqual(MarketSession.Session_Morning,
                MarketClock.NextSessionAfter(MarketSession.Session_Evening, 2, out nextDay));
            Assert.AreEqual(3, nextDay);

            // 4일째 오후장 다음은 다음 계절 1일째 — 오전장 없이 그날 오후장(재개장).
            Assert.AreEqual(MarketSession.Session_Evening,
                MarketClock.NextSessionAfter(MarketSession.Session_Evening, 4, out nextDay));
            Assert.AreEqual(5, nextDay);
        }

        [Test]
        public void ReopeningCannotBeSkipped()
        {
            var clock = new MarketClock();
            clock.BeginBreak();

            // 새 판은 1일째 낮에 시작한다. 다음 장이 첫 계절의 오후장(재개장 자리)이다.
            Assert.IsTrue(clock.IsReopeningAt(0));
            Assert.IsFalse(clock.SkipNextSession());
            Assert.AreEqual(0, clock.SkippedSessions);
        }

        [Test]
        public void SessionGapsFollowEightAndEighteen()
        {
            var clock = new MarketClock();
            clock.OpenSession(false, MarketSession.Session_Morning);
            clock.CloseSession();
            clock.BeginBreak();

            Assert.AreEqual(14 * 60, clock.BreakMinuteOfDay);
            Assert.AreEqual(18 * 60, clock.NextSessionMinute);
        }
    }
}
