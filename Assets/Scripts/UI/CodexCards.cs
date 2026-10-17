using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 모험단 도감 카드. 거래소 정보 기록의 「모험단」 탭과 영지 수첩이 같은 카드를 쓴다.
    /// 아는 줄은 그대로, 모르는 줄은 어디서 알 수 있는지 흐리게 보인다.
    /// </summary>
    public static class CodexCards
    {
        public static VisualElement Build(MarketSimulation sim, string stockId, string displayName)
        {
            var card = new VisualElement();
            card.AddToClassList("info-card");

            var held = sim == null ? 0 : sim.Wallet.QuantityOf(stockId);
            card.EnableInClassList("info-card--held", held > 0);

            var line = new VisualElement();
            line.AddToClassList("info-line");

            var name = new Label(displayName);
            name.AddToClassList("info-stock");
            line.Add(name);

            if (held > 0)
            {
                var badge = new Label(string.Format(StringConstants.TEXT_INFO_HELD, held));
                badge.AddToClassList("info-held");
                line.Add(badge);
            }

            var known = BandCodex.KnownCount(stockId);
            var meta = new Label(StageText(sim, stockId) + " · "
                + string.Format(StringConstants.TEXT_CODEX_KNOWN, known, StringConstants.CODEX_HINTS.Length));
            meta.AddToClassList("info-when");
            line.Add(meta);
            card.Add(line);

            for (var i = 0; i < StringConstants.CODEX_HINTS.Length; i++)
            {
                var source = (CodexSource)i;
                var fact = new Label(BandCodex.LineOf(stockId, source));
                fact.AddToClassList("codex-fact");
                fact.EnableInClassList("codex-fact--unknown", BandCodex.IsKnown(stockId, source) == false);
                card.Add(fact);
            }

            return card;
        }

        private static string StageText(MarketSimulation sim, string stockId)
        {
            if (sim == null)
            {
                return string.Empty;
            }

            switch (sim.StageOf(stockId))
            {
                case BandStage.Band_Arrived:
                {
                    return StringConstants.TEXT_BAND_ARRIVED;
                }
                case BandStage.Band_Listed:
                {
                    return StringConstants.TEXT_BAND_LISTED;
                }
                case BandStage.Band_Left:
                {
                    return StringConstants.TEXT_CODEX_STAGE_LEFT;
                }
                default:
                {
                    return StringConstants.TEXT_CODEX_STAGE_NOT_ARRIVED;
                }
            }
        }
    }
}
