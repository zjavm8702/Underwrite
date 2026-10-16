using System;
using Underwrite.Enumeration;
using Underwrite.Global;

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

        /// <summary>장이 열린 뒤 지난 틱 수. 사건 일정의 기준이다.</summary>
        public int Tick { get; private set; }

        /// <summary>며칠째인가. 오후장이 끝나면 하루가 넘어간다.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>장을 넘길 때 틱을 하나 강제로 진행시킨다. 실시간 경과와 무관하다.</summary>
        public void ForceTick()
        {
            PhaseDuringAdvance = Phase;

            if (Tick < TicksPerSession)
            {
                Tick++;
            }
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
