using System;
using System.Collections.Generic;
using Underwrite.Enumeration;

namespace Underwrite.Save
{
    /// <summary>
    /// 저장 파일 한 벌. JsonUtility가 읽고 쓰므로 사전 대신 목록을 쓴다.
    ///
    /// 저장은 장 사이(영지에 있을 때)에만 한다. 장중 상태(주문 대기·정보 타임·틱 사이 흔들림)는 담지 않는다.
    /// 장중에 끄면 그 장에 들어가기 전의 영지에서 다시 시작한다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int Version;

        /// <summary>저장한 실제 시각(UTC, ISO 8601). 이어하기 단추에 보이지 않지만 문제를 찾을 때 쓴다.</summary>
        public string SavedAtUtc;

        public ClockState Clock = new ClockState();

        /// <summary>지하거래소를 찾기 전이면 시장이 아직 없다.</summary>
        public bool HasMarket;

        public MarketState Market = new MarketState();
        public GatherState Gather = new GatherState();
        public PatronageState Patronage = new PatronageState();
        public HeroesState Heroes = new HeroesState();
        public StoryState Story = new StoryState();
        public List<CodexState> Codex = new List<CodexState>();
        public int QuestIndex;
        public SeasonPlanState SeasonPlan = new SeasonPlanState();
    }

    [Serializable]
    public sealed class SeasonPlanState
    {
        public List<int> Departed = new List<int>();
        public int PendingGuild;
        public int PendingSmith;
        public int PendingTips;
        public int OmenClues;
    }

    [Serializable]
    public sealed class ClockState
    {
        public float Accumulated;
        public float BreakAccumulated;
        public int SessionsDone;
        public int Tick;
        public int Day;
        public int BreakMinuteOfDay;
        public bool IsBreak;
        public int SkippedSessions;
        public int Cycle;
        public MarketPhase Phase;
        public MarketSession Session;
    }

    [Serializable]
    public sealed class MarketState
    {
        public int Seed;
        public MarketSentiment Sentiment;
        public int NextEventIndex;
        public int InfoTimesOpened;
        public int NextSlot;
        public int BriefingOrder;
        public List<StockState> Stocks = new List<StockState>();
        public WalletState Wallet = new WalletState();
        public List<InfoState> Info = new List<InfoState>();

        /// <summary>주기마다 지은 새 단. 불러올 때 같은 단을 다시 세운다.</summary>
        public List<GeneratedBandState> Generated = new List<GeneratedBandState>();
        public int BandSerial;
    }

    /// <summary>새로 지은 용병단 하나의 명세. 표에 없는 단이라 이것만으로 다시 세울 수 있어야 한다.</summary>
    [Serializable]
    public sealed class GeneratedBandState
    {
        public string Id;
        public string Name;
        public int Profile;
        public StockPersonality Personality;
        public float TrueValue;
        public float Liquidity;
        public int ListCycle;
        public int DelistCycle;
        public DelistReason DelistReason;
    }

    [Serializable]
    public sealed class StockState
    {
        public string Id;
        public float TrueValue;
        public float MarketExpectation;
        public float Price;
        public bool IsTradable;
        public DelistReason LeftBecause;
        public int NoticedDelistCycle;
        public DelistReason PlannedReason;
        public float SessionOpenPrice;
        public float Momentum;
        public List<float> History = new List<float>();
    }

    [Serializable]
    public sealed class WalletState
    {
        public float Cash;
        public float RealizedProfit;
        public float PatronageSpent;
        public List<HoldingState> Holdings = new List<HoldingState>();
    }

    [Serializable]
    public sealed class HoldingState
    {
        public string StockId;
        public int Quantity;
        public float AverageCost;
    }

    [Serializable]
    public sealed class InfoState
    {
        public int Tick;
        public string StockId;
        public string Headline;
        public InfoSource Source;
        public EventStage Stage;
        public bool IsPublic;
        public int Day;
        public MarketSession Session;
    }

    [Serializable]
    public sealed class GatherState
    {
        public float Spent;
        public float Earned;
        public float Budget;
        public int NextSlot;
        public List<NoteState> Notes = new List<NoteState>();
    }

    [Serializable]
    public sealed class NoteState
    {
        public string StockId;
        public string Headline;
        public InfoSource Source;
    }

    [Serializable]
    public sealed class PatronageState
    {
        public bool Collapsed;
        public int Clues;
        public int RequestsPosted;
        public List<NeedSaveState> Needs = new List<NeedSaveState>();
        public List<string> Reactions = new List<string>();
        public List<int> ReactionDays = new List<int>();
    }

    [Serializable]
    public sealed class NeedSaveState
    {
        public string Id;
        public NeedState State;
        public bool Heard;
    }

    [Serializable]
    public sealed class HeroesState
    {
        public List<HeroState> People = new List<HeroState>();
        public List<int> DayOffSeen = new List<int>();
        public List<int> ReturnSeen = new List<int>();
    }

    [Serializable]
    public sealed class HeroState
    {
        public string Id;
        public int Affinity;
        public int LastTalkDay;
        public List<string> TopicsSeen = new List<string>();
    }

    [Serializable]
    public sealed class StoryState
    {
        public bool CellarFound;
        public bool MarketTutorialDone;
        public bool VillageTutorialDone;
        public List<string> SleepSeen = new List<string>();
    }

    [Serializable]
    public sealed class CodexState
    {
        public string StockId;
        public List<int> LearnedCycle = new List<int>();
    }
}
