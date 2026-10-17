using System;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Save;

namespace Underwrite.Market
{
    /// <summary>
    /// 장중 시간을 관리한다. 기획서 §11의 시간 제어와 영주 조사일 휴장을 담당한다.
    ///
    /// 이동·조사·거래가 같은 시간을 공유하므로 시계는 하나만 둔다.
    /// 일시정지와 배속을 제공하고, 주요 사건에서 자동 정지할 수 있다.
    /// 휴장일에는 주문 체결이 멈추지만 원정과 세계의 사건은 계속 진행된다.
    /// </summary>
    public sealed class MarketClock
    {
        private float Accumulated;
        private float BreakAccumulated;
        private int SessionsDone;

        /// <summary>장이 열린 뒤 지난 틱 수. 사건 일정의 기준이다.</summary>
        public int Tick { get; private set; }

        /// <summary>며칠째인가. 오후장이 끝나면 하루가 넘어간다.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>
        /// 장과 장 사이, 지금 시각(분). 자정 기준이며 0~1439 사이다.
        /// 장중 시각은 Tick이 만들지만 영지의 시각은 이 값이 든다.
        /// </summary>
        public int BreakMinuteOfDay { get; private set; }

        /// <summary>영지 시간이 흐르는 중인가.</summary>
        public bool IsBreak { get; private set; }

        public int BreakHour
        {
            get
            {
                return BreakMinuteOfDay / 60 % 24;
            }
        }

        /// <summary>영지 시각의 초. 영지는 실시간 1:1이라 초가 흘러야 시간이 가는 것이 보인다.</summary>
        public int BreakSecond
        {
            get
            {
                return Math.Min(59, (int)(BreakAccumulated * 60f));
            }
        }

        public int BreakMinute
        {
            get
            {
                return BreakMinuteOfDay % 60;
            }
        }

        /// <summary>
        /// 플레이어가 쉬기로 한 장의 수. 기획서 §11 — 장에 나가는 것은 선택이다.
        ///
        /// 쉰다고 해서 시간이 사라지지 않는다. 그 장이 열리고 닫히는 동안
        /// 플레이어는 영지에 있을 뿐이다. 그래서 **다음 장이 그만큼 뒤로 밀린다.**
        /// 거래 화면으로 돌아가면 쉰 장을 실제로 돌려 보고 0으로 되돌린다.
        /// </summary>
        public int SkippedSessions { get; private set; }

        /// <summary>
        /// 다음 장이 열리는 시각(분). 오전장 다음은 오후장, 오후장 다음은 이튿날 오전장이다.
        ///
        /// 장은 9시와 21시에 열린다. 간격이 12시간으로 고른 덕에, 쉰 장은
        /// 횟수에 12시간을 곱해 더하는 것으로 끝난다. 장의 상태를 건드릴 필요가 없다.
        /// </summary>
        public int NextSessionMinute
        {
            get
            {
                var baseMinute = Session == MarketSession.Session_Morning
                    ? Constants.MARKET_EVENING_START_HOUR * 60
                    : (Constants.MARKET_MORNING_START_HOUR + 24) * 60;

                return baseMinute + SkippedSessions * SESSION_GAP_MINUTES;
            }
        }

        /// <summary>
        /// 영지에서 보는 날짜. 하루는 오전장이 열리는 9시에 시작한다.
        ///
        /// <see cref="Day"/>는 거래소가 오전장을 열 때만 넘어간다. 그래서 장을 쉬고 영지에서 다음 날
        /// 낮까지 머물면 날짜가 그대로 남았다(2026-10-17 사용자 보고: "2일차에서 안 넘어간다").
        /// 영지의 시계가 다음 날 9시를 지나면 그만큼 더해 보여 준다. 거래소가 장을 열면 Day가 따라온다.
        /// </summary>
        public int CalendarDay
        {
            get
            {
                if (IsBreak == false)
                {
                    return Day;
                }

                var sinceMorning = BreakMinuteOfDay - Constants.MARKET_MORNING_START_HOUR * 60;
                return Day + Math.Max(0, sinceMorning / MINUTES_PER_DAY);
            }
        }


        /// <summary>
        /// 다음에 열릴 장이 오전장인가. 그렇다면 지금은 밤이고, 하루를 닫고 잠자리에 들 때다.
        /// 쉰 장이 있으면 한 장마다 오전·오후가 뒤바뀐다.
        /// </summary>
        public bool NextSessionIsMorning
        {
            get
            {
                var lastWasEvening = Session == MarketSession.Session_Evening;
                var flips = SkippedSessions % 2 == 1;
                return lastWasEvening != flips;
            }
        }

        /// <summary>
        /// 다음 장을 쉰다. 영지의 시간이 한 장만큼 길어진다.
        ///
        /// 쉬는 동안에도 시장은 돈다. 가격이 움직이고 소식이 돌고 상장과 이탈이 일어난다.
        /// 플레이어만 그 자리에 없다. 거래로 벌 수 있었던 시간을 정보와 종잣돈으로 맞바꾸는 것이다.
        /// </summary>
        public void SkipNextSession()
        {
            if (IsBreak == false)
            {
                return;
            }

            SkippedSessions++;
        }

        /// <summary>
        /// 쉬기로 한 것을 무른다. **아직 그 시간을 쓰지 않았을 때만** 된다.
        ///
        /// 쉬기로 하고 여섯 시간을 더 쓴 뒤에 무르면 시각이 다음 장을 넘어서 버린다.
        /// 시간을 빚질 수는 없으므로, 되돌려도 남는 시간이 있을 때만 허락한다.
        /// </summary>
        public bool UndoSkippedSession()
        {
            if (SkippedSessions <= 0)
            {
                return false;
            }

            if (BreakMinuteOfDay > NextSessionMinute - SESSION_GAP_MINUTES)
            {
                return false;
            }

            SkippedSessions--;
            return true;
        }

        /// <summary>쉰 장을 모두 치렀다. 거래 화면이 실제로 돌린 뒤에 부른다.</summary>
        public void ClearSkippedSessions()
        {
            SkippedSessions = 0;
        }

        /// <summary>다음 장까지 남은 시간(분). 영지에서 쓸 수 있는 전부다.</summary>
        public int BreakMinutesLeft
        {
            get
            {
                var left = NextSessionMinute - BreakMinuteOfDay;

                if (left < 0)
                {
                    return 0;
                }

                return left;
            }
        }

        /// <summary>
        /// 장이 끝나고 영지의 시간이 시작된다. 폐장 시각부터 흐른다.
        /// </summary>
        public void BeginBreak()
        {
            IsBreak = true;
            BreakMinuteOfDay = StartHour * 60 + Constants.MARKET_SESSION_MINUTES;
        }

        /// <summary>영지의 시간을 실시간 속도로 흘린다. 매 프레임 불린다.</summary>
        public void AdvanceBreak(float deltaSeconds)
        {
            if (IsBreak == false)
            {
                return;
            }

            BreakAccumulated += deltaSeconds * Constants.BREAK_MINUTES_PER_REAL_SECOND;

            while (BreakAccumulated >= 1f)
            {
                BreakAccumulated -= 1f;
                SpendBreakMinutes(1);
            }
        }

        /// <summary>
        /// 영지에서 시간을 쓴다. 한 가지 일을 하면 그만큼 시간이 간다.
        /// 다음 장을 넘어서면 거기서 멈춘다. 시간을 빚질 수는 없다.
        /// </summary>
        public void SpendBreakMinutes(int minutes)
        {
            if (IsBreak == false || minutes <= 0)
            {
                return;
            }

            BreakMinuteOfDay += minutes;

            if (BreakMinuteOfDay > NextSessionMinute)
            {
                BreakMinuteOfDay = NextSessionMinute;
            }
        }

        /// <summary>시간 넘기기로 갈 수 있는 가장 늦은 시각(하루 기준 분). 다음 장 직전이다.</summary>
        public int LatestBreakMinute
        {
            get
            {
                return NextSessionMinute - Constants.BREAK_SKIP_MARGIN_MINUTES;
            }
        }

        /// <summary>
        /// 남은 시간을 건너뛰어 다음 장 직전으로 간다.
        /// 볼일을 다 봤는데 실시간으로 기다리게 할 이유가 없다.
        /// </summary>
        public void SkipBreak()
        {
            if (IsBreak == false)
            {
                return;
            }

            var target = NextSessionMinute - Constants.BREAK_SKIP_MARGIN_MINUTES;

            if (target > BreakMinuteOfDay)
            {
                BreakMinuteOfDay = target;
            }
        }

        /// <summary>장이 열린다. 영지의 시간은 멈춘다.</summary>
        public void EndBreak()
        {
            IsBreak = false;
            BreakAccumulated = 0f;
        }

        // 장과 장 사이의 간격. 9시와 21시에 열리므로 12시간으로 고르다.
        private const int SESSION_GAP_MINUTES = 12 * 60;
        private const int MINUTES_PER_DAY = 24 * 60;

        /// <summary>
        /// 몇 주기째인가. 주기는 원정 한 번이고 40주기가 10년이다. 설계안 §2.
        /// 장을 셀 뿐 가격 계산에는 쓰지 않는다. 상장과 이탈이 이 숫자로 일어난다.
        /// </summary>
        public int Cycle { get; private set; } = 1;

        /// <summary>이번 주기에서 몇 번째 장인가. 1부터 센다.</summary>
        public int SessionInCycle
        {
            get
            {
                return SessionsDone % Constants.SESSIONS_PER_CYCLE + 1;
            }
        }

        /// <summary>이번 장이 주기의 마지막 장인가. 끝나면 주기가 넘어간다.</summary>
        public bool IsLastSessionOfCycle
        {
            get
            {
                return SessionInCycle >= Constants.SESSIONS_PER_CYCLE;
            }
        }

        /// <summary>장을 넘길 때 틱을 하나 강제로 진행시킨다. 실시간 경과와 무관하다.</summary>
        public void ForceTick()
        {
            PhaseDuringAdvance = Phase;

            if (Tick < TicksPerSession)
            {
                Tick++;
            }
        }

        /// <summary>
        /// 그날이 영주 시찰일인가. 정기 시찰이라 달력으로 미리 알 수 있다(기획서 §11).
        /// 시찰일에는 지하시장을 숨기고 주문을 받지 않는다. 오전장·오후장이 모두 닫힌다.
        /// </summary>
        public static bool IsInspectionDay(int day)
        {
            if (day < Constants.INSPECTION_FIRST_DAY || Constants.INSPECTION_INTERVAL_DAYS <= 0)
            {
                return false;
            }

            return (day - Constants.INSPECTION_FIRST_DAY) % Constants.INSPECTION_INTERVAL_DAYS == 0;
        }

        /// <summary>그날 또는 그 뒤로 가장 가까운 시찰일.</summary>
        public static int NextInspectionDay(int fromDay)
        {
            if (fromDay <= Constants.INSPECTION_FIRST_DAY || Constants.INSPECTION_INTERVAL_DAYS <= 0)
            {
                return Constants.INSPECTION_FIRST_DAY;
            }

            var passed = fromDay - Constants.INSPECTION_FIRST_DAY;
            var steps = (passed + Constants.INSPECTION_INTERVAL_DAYS - 1) / Constants.INSPECTION_INTERVAL_DAYS;
            return Constants.INSPECTION_FIRST_DAY + steps * Constants.INSPECTION_INTERVAL_DAYS;
        }

        public void AdvanceDay()
        {
            Day++;
        }

        public MarketPhase Phase { get; private set; } = MarketPhase.Phase_Closed;

        /// <summary>지금 열려 있는 장. 하루에 오전·오후 두 번 열린다.</summary>
        public MarketSession Session { get; private set; } = MarketSession.Session_Morning;

        /// <summary>장이 시작하는 시각(시). 오전장 09시, 오후장 21시.</summary>
        public int StartHour
        {
            get
            {
                if (Session == MarketSession.Session_Evening)
                {
                    return Constants.MARKET_EVENING_START_HOUR;
                }

                return Constants.MARKET_MORNING_START_HOUR;
            }
        }

        /// <summary>현재 게임 내 시각의 시 부분. 오후장은 자정을 넘어 03시까지 간다.</summary>
        public int ClockHour
        {
            get
            {
                var total = StartHour * 60 + ElapsedMinutes;
                return total / 60 % 24;
            }
        }

        /// <summary>
        /// 장중 게임 시각의 초까지(하루 기준 초). 틱 사이도 흐르게 보여 배속이 눈에 보인다(2026-10-17 사용자 요청).
        /// 1배에서 게임 1분이 실시간 3초다.
        /// </summary>
        public int ClockTotalSeconds
        {
            get
            {
                var minutes = StartHour * 60 + ElapsedMinutes + Math.Min(Accumulated, Constants.MARKET_MINUTES_PER_TICK);
                return (int)(minutes * 60f) % (24 * 60 * 60);
            }
        }

        /// <summary>현재 게임 내 시각의 분 부분.</summary>
        public int ClockMinute
        {
            get
            {
                return ElapsedMinutes % 60;
            }
        }

        /// <summary>
        /// 마지막 <see cref="Advance"/>를 시작할 때의 상태. 한 번의 Advance가 장 마감을 넘어설 수 있으므로,
        /// 그 배치에서 넘어온 틱이 장중 틱이었는지는 이 값으로 판단한다.
        /// 이 값을 쓰지 않고 <see cref="Phase"/>를 읽으면 마감을 넘은 배치의 가격 갱신이 통째로 사라진다.
        /// </summary>
        public MarketPhase PhaseDuringAdvance { get; private set; } = MarketPhase.Phase_Closed;

        /// <summary>일시정지 여부. 중요한 대화 중에는 시간을 정지한다.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>배속. 1.0이 기본이다.</summary>
        public float SpeedScale { get; private set; } = 1f;

        /// <summary>장중 길이를 틱으로 환산한 값.</summary>
        public int TicksPerSession
        {
            get
            {
                return Constants.MARKET_SESSION_MINUTES / Constants.MARKET_MINUTES_PER_TICK;
            }
        }

        /// <summary>장중 경과 비율. 0이 개장, 1이 폐장이다.</summary>
        /// <summary>
        /// 이번 틱이 얼마나 지났는가. 0에서 1 사이다.
        /// 차트의 마지막 점을 틱과 틱 사이에서 매끄럽게 밀기 위해 쓴다.
        /// </summary>
        public float TickFraction
        {
            get
            {
                var fraction = Accumulated / Constants.MARKET_MINUTES_PER_TICK;

                if (fraction < 0f)
                {
                    return 0f;
                }

                if (fraction > 1f)
                {
                    return 1f;
                }

                return fraction;
            }
        }

        public float SessionProgress
        {
            get
            {
                if (TicksPerSession <= 0)
                {
                    return 0f;
                }

                var ratio = (float)Tick / TicksPerSession;

                if (ratio > 1f)
                {
                    return 1f;
                }

                return ratio;
            }
        }

        /// <summary>게임 내 경과 분.</summary>
        public int ElapsedMinutes
        {
            get
            {
                return Tick * Constants.MARKET_MINUTES_PER_TICK;
            }
        }

        /// <summary>시간이 실제로 흐르고 있는지. 정지 중이거나 폐장이면 흐르지 않는다.</summary>
        public bool IsRunning
        {
            get
            {
                return IsPaused == false && Phase != MarketPhase.Phase_Closed;
            }
        }

        /// <summary>
        /// 주문을 받을 수 있는 상태인지. 휴장일과 일시정지 중에는 받지 않는다.
        ///
        /// 기획서 §11은 "정지 중 주문 입력과 체결 허용 여부는 상세 규칙으로 확정해야 한다"고
        /// 미결정으로 두었다. 사용자 결정(2026-10-16): **정지 중에는 주문할 수 없다.**
        /// 정지해 두고 천천히 고르는 것이 유리해지면 실시간 시장의 긴장이 사라진다.
        /// 대화 중 자동 정지(§11)는 읽는 속도가 손실을 결정하지 않게 하려는 장치이지,
        /// 생각할 시간을 공짜로 주려는 장치가 아니다.
        /// </summary>
        public bool AcceptsOrders
        {
            get
            {
                return Phase == MarketPhase.Phase_Open && IsPaused == false;
            }
        }

        /// <summary>장을 연다. 휴장일이면 Halted로 연다 — 가격은 흐르지 않고 마지막 가격만 보인다.</summary>
        public void OpenSession(bool isInspectionDay, MarketSession session = MarketSession.Session_Morning)
        {
            Tick = 0;
            Accumulated = 0f;
            Session = session;

            if (isInspectionDay == true)
            {
                Phase = MarketPhase.Phase_Halted;
                return;
            }

            Phase = MarketPhase.Phase_Open;
        }

        /// <summary>장 하나를 끝낸 것으로 센다. 주기가 넘어가면 참을 돌려준다.</summary>
        public bool CountSessionAndAdvanceCycle()
        {
            SessionsDone++;

            if (SessionsDone % Constants.SESSIONS_PER_CYCLE != 0)
            {
                return false;
            }

            Cycle++;
            return true;
        }

        public void CloseSession()
        {
            Phase = MarketPhase.Phase_Closed;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }

        public void SetSpeedScale(float scale)
        {
            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), "Speed scale must be positive.");
            }

            SpeedScale = scale;
        }

        /// <summary>
        /// 실시간 경과를 받아 넘어간 틱 수를 돌려준다. 호출하는 쪽이 그만큼 시장을 진행시킨다.
        /// 정지 중이거나 흐르지 않는 상태면 0을 돌려준다.
        /// </summary>
        public int Advance(float deltaSeconds)
        {
            PhaseDuringAdvance = Phase;

            if (IsRunning == false)
            {
                return 0;
            }

            if (Phase == MarketPhase.Phase_Halted)
            {
                // 휴장일에도 시간은 흐르지만 가격은 움직이지 않는다. 틱만 세어 사건을 진행시킨다.
                return AccumulateTicks(deltaSeconds);
            }

            return AccumulateTicks(deltaSeconds);
        }

        /// <summary>저장할 값. 시계는 영지와 거래소가 함께 보므로 장 밖에서도 저장된다.</summary>
        internal ClockState Capture()
        {
            return new ClockState
            {
                Accumulated = Accumulated,
                BreakAccumulated = BreakAccumulated,
                SessionsDone = SessionsDone,
                Tick = Tick,
                Day = Day,
                BreakMinuteOfDay = BreakMinuteOfDay,
                IsBreak = IsBreak,
                SkippedSessions = SkippedSessions,
                Cycle = Cycle,
                Phase = Phase,
                Session = Session
            };
        }

        /// <summary>저장한 시계를 되살린다. 배속과 일시정지는 되살리지 않는다 — 다시 켜면 1배로 시작한다.</summary>
        internal static MarketClock Restore(ClockState state)
        {
            return new MarketClock
            {
                Accumulated = state.Accumulated,
                BreakAccumulated = state.BreakAccumulated,
                SessionsDone = state.SessionsDone,
                Tick = state.Tick,
                Day = state.Day,
                BreakMinuteOfDay = state.BreakMinuteOfDay,
                IsBreak = state.IsBreak,
                SkippedSessions = state.SkippedSessions,
                Cycle = state.Cycle,
                Phase = state.Phase,
                Session = state.Session
            };
        }

        private int AccumulateTicks(float deltaSeconds)
        {
            var minutes = deltaSeconds * Constants.MARKET_MINUTES_PER_REAL_SECOND * SpeedScale;
            Accumulated += minutes;

            var stepped = 0;

            while (Accumulated >= Constants.MARKET_MINUTES_PER_TICK)
            {
                Accumulated -= Constants.MARKET_MINUTES_PER_TICK;
                Tick++;
                stepped++;

                if (Tick >= TicksPerSession)
                {
                    CloseSession();
                    break;
                }
            }

            return stepped;
        }
    }
}
