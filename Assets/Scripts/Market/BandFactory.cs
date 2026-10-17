using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Save;

namespace Underwrite.Market
{
    /// <summary>
    /// 주기마다 새로 오는 용병단을 짓는다(2026-10-17 사용자 결정 — 상장 조건 없이 자동 상장).
    ///
    /// 단마다 초상을 그리지 않는다. 유형(신생·노련·고위험·외지)만 정하고, 도감 문장은 같은 유형의 묶음을 돌려 쓴다.
    /// 이름은 조각을 이어 짓는다. 지은 것은 <see cref="GeneratedBandState"/>로 남겨 저장·불러오기에 쓴다.
    /// </summary>
    public static class BandFactory
    {
        private const string ID_PREFIX = "STOCK_BAND_";

        /// <summary>새 단 하나의 명세. <paramref name="taken"/>에 있는 이름은 피한다.</summary>
        public static GeneratedBandState Roll(Random rng, int serial, int listCycle, ICollection<string> taken)
        {
            var profile = rng.Next(Constants.BAND_PROFILE_PERSONALITY.Length);
            var stay = rng.Next(Constants.BAND_STAY_MIN, Constants.BAND_STAY_MAX + 1);
            var reason = Constants.BAND_PROFILE_DEPARTURE[profile];

            if (reason == DelistReason.Delist_Wiped && rng.NextDouble() >= Constants.BAND_WIPE_CHANCE)
            {
                reason = DelistReason.Delist_Moved;
            }

            return new GeneratedBandState
            {
                Id = ID_PREFIX + serial,
                Name = PickName(rng, taken),
                Profile = profile,
                Personality = Constants.BAND_PROFILE_PERSONALITY[profile],
                TrueValue = Between(rng, Constants.BAND_PROFILE_VALUE[profile]),
                Liquidity = Between(rng, Constants.BAND_PROFILE_LIQUIDITY[profile]),
                ListCycle = listCycle,
                DelistCycle = listCycle + stay,
                DelistReason = reason
            };
        }

        public static AdventurerStock Build(GeneratedBandState spec)
        {
            var stock = new AdventurerStock(spec.Id, spec.Name, spec.Personality, spec.TrueValue, spec.Liquidity);
            stock.SetCodexProfile(spec.Profile);
            return stock;
        }

        private static string PickName(Random rng, ICollection<string> taken)
        {
            var name = string.Empty;

            // 겹치지 않을 때까지 몇 번 굴린다. 조각 수가 넉넉해 거의 한 번에 된다.
            for (var attempt = 0; attempt < 20; attempt++)
            {
                name = StringConstants.BAND_NAME_FIRST[rng.Next(StringConstants.BAND_NAME_FIRST.Length)] + " "
                    + StringConstants.BAND_NAME_SECOND[rng.Next(StringConstants.BAND_NAME_SECOND.Length)]
                    + StringConstants.BAND_NAME_TAIL[rng.Next(StringConstants.BAND_NAME_TAIL.Length)];

                if (taken.Contains(name) == false)
                {
                    return name;
                }
            }

            return name + " " + (taken.Count + 1);
        }

        private static float Between(Random rng, float[] range)
        {
            return (float)(range[0] + (range[1] - range[0]) * rng.NextDouble());
        }
    }
}
