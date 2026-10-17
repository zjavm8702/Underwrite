using System.Collections.Generic;
using Underwrite.Enumeration;

namespace Underwrite.Market
{
    /// <summary>
    /// 첫 시제품의 종목 3개와 사건 일정. 기획서 §14의 "거래 대상 3개"를 코드로 고정했다.
    ///
    /// 사건 일정은 기획서 §7의 예시를 그대로 옮긴 것이다.
    /// 무명 모험가의 고난도 의뢰 수락 → 유명 기사 동행 소문으로 급등 → 동행 취소 공개로 급락
    /// → 원정 중 연락 두절로 공포 확산 → 희귀 유물과 함께 귀환하며 재평가.
    ///
    /// 종목 데이터를 ScriptableObject로 옮기는 것은 시제품 검증 이후다. 지금은 코드가 단일 출처다.
    /// </summary>
    public static class PrototypeScenario
    {
        public const string STOCK_ROOKIE = "rookie_sella";
        public const string STOCK_VETERAN = "veteran_brann";
        public const string STOCK_HIGH_RISK = "band_blackmoor";

        /// <summary>시제품 종목 3개. 성격이 서로 달라 같은 장세에서 다르게 움직인다.</summary>
        public static List<AdventurerStock> CreateStocks()
        {
            return new List<AdventurerStock>
            {
                // 신입: 정보와 거래가 적어 소문에 크게 흔들린다. 유동성이 낮다.
                new AdventurerStock(STOCK_ROOKIE, "셀라",
                    StockPersonality.Personality_Rookie, 42f, 60f),

                // 베테랑: 실적이 알려져 있어 변동이 작다. 유동성이 높다.
                new AdventurerStock(STOCK_VETERAN, "브란",
                    StockPersonality.Personality_Veteran, 120f, 400f),

                // 고위험 모험단: 성공 보상과 실패 위험이 모두 크다.
                new AdventurerStock(STOCK_HIGH_RISK, "블랙무어 단",
                    StockPersonality.Personality_HighRisk, 75f, 150f)
            };
        }

        /// <summary>
        /// 사건 일정. 한 장중(72틱) 안에서 소문 → 보고 → 결과가 모두 돌아간다.
        ///
        /// 셀라의 연쇄가 검증 질문 2번(정보로 가격 변동을 추론할 수 있는가)의 핵심이다.
        /// 15틱의 동행 소문으로 기대가 크게 오르고, 28틱에 취소가 공개되며 그 기대가 꺾인다.
        /// 54틱의 결과는 실제 가치를 올리지만, 그 시점에 기대가 이미 식어 있어 재평가 폭이 커진다.
        /// </summary>
        public static List<MarketEventStep> CreateEvents()
        {
            return new List<MarketEventStep>
            {
                // ── 셀라: 고난도 의뢰 연쇄 ─────────────────────────
                new MarketEventStep(0, STOCK_ROOKIE, EventStage.Stage_Rumor,
                    "셀라가 고난도 유적 의뢰를 수락했다는 이야기가 돈다.",
                    InfoSource.Source_TavernTalk, InfoVisibility.Visibility_Public,
                    expectationDelta: 0.10f),

                new MarketEventStep(0, STOCK_ROOKIE, EventStage.Stage_Rumor,
                    "유명 기사 브란이 동행한다는 소문이 퍼졌다. 매수가 몰린다.",
                    InfoSource.Source_MarketBoard, InfoVisibility.Visibility_Public,
                    expectationDelta: 0.45f),

                // 플레이어만 아는 정보. 시장은 아직 모르므로 가격이 움직이지 않는다.
                new MarketEventStep(1, STOCK_ROOKIE, EventStage.Stage_Report,
                    "브란이 다른 의뢰를 먼저 받았다고 사적으로 전해 왔다. 동행은 어려워 보인다.",
                    InfoSource.Source_PrivateLetter, InfoVisibility.Visibility_PlayerOnly),

                new MarketEventStep(1, STOCK_ROOKIE, EventStage.Stage_Report,
                    "동행 취소가 공개됐다. 기대가 꺾이고 매도가 쏟아진다.",
                    InfoSource.Source_MarketBoard, InfoVisibility.Visibility_Public,
                    expectationDelta: -0.40f),

                new MarketEventStep(2, STOCK_ROOKIE, EventStage.Stage_Report,
                    "원정 중 연락이 끊겼다. 공포가 번진다.",
                    InfoSource.Source_GuildRecord, InfoVisibility.Visibility_Public,
                    expectationDelta: -0.25f),

                new MarketEventStep(2, STOCK_ROOKIE, EventStage.Stage_Outcome,
                    "셀라가 희귀 유물과 함께 귀환했다. 실력이 재평가된다.",
                    InfoSource.Source_GuildRecord, InfoVisibility.Visibility_Public,
                    valueDelta: 0.55f),

                // ── 브란: 베테랑의 담담한 흐름 ───────────────────
                new MarketEventStep(0, STOCK_VETERAN, EventStage.Stage_Rumor,
                    "브란이 고수익 호위 의뢰를 맡았다.",
                    InfoSource.Source_GuildRecord, InfoVisibility.Visibility_Public,
                    expectationDelta: 0.08f),

                new MarketEventStep(1, STOCK_VETERAN, EventStage.Stage_Outcome,
                    "브란이 1차 호위를 무사히 마쳤다. 보수가 확정됐다.",
                    InfoSource.Source_GuildRecord, InfoVisibility.Visibility_Public,
                    valueDelta: 0.07f),

                new MarketEventStep(2, STOCK_VETERAN, EventStage.Stage_Outcome,
                    "브란이 2차 호위도 예정대로 마쳤다. 놀랄 일은 없었다.",
                    InfoSource.Source_GuildRecord, InfoVisibility.Visibility_Public,
                    valueDelta: 0.06f),

                new MarketEventStep(3, STOCK_VETERAN, EventStage.Stage_Outcome,
                    "브란이 장기 계약을 따냈다. 실적이 쌓인다.",
                    InfoSource.Source_GuildRecord, InfoVisibility.Visibility_Public,
                    valueDelta: 0.08f),

                // ── 블랙무어 단: 큰 성공과 큰 실패 ───────────────
                new MarketEventStep(0, STOCK_HIGH_RISK, EventStage.Stage_Rumor,
                    "블랙무어 단이 미지의 심층 구역에 들어간다는 소문이다.",
                    InfoSource.Source_TavernTalk, InfoVisibility.Visibility_Public,
                    expectationDelta: 0.30f),

                new MarketEventStep(1, STOCK_HIGH_RISK, EventStage.Stage_Report,
                    "중개인이 단장의 장비가 파손됐다고 흘렸다. 신뢰도는 낮다.",
                    InfoSource.Source_BrokerLeak, InfoVisibility.Visibility_Public,
                    expectationDelta: -0.12f),

                new MarketEventStep(2, STOCK_HIGH_RISK, EventStage.Stage_Outcome,
                    "단원 둘을 잃고 빈손으로 돌아왔다.",
                    InfoSource.Source_GuildRecord, InfoVisibility.Visibility_Public,
                    valueDelta: -0.35f)
            };
        }
    }
}
