using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Market
{
    /// <summary>호가 한 단계. 가격과 그 가격에 쌓인 수량이다.</summary>
    public struct BookLevel
    {
        public float Price;
        public int Quantity;
    }

    /// <summary>
    /// 호가창. 실제 매칭 엔진을 두지 않고 현재가·유동성·틱에서 **파생**시킨다.
    ///
    /// 왜 이렇게 하는가.
    /// 이 게임의 가격은 <see cref="PriceModel"/>이 만드는 연속 값이다. 호가를 진짜로 쌓아
    /// 체결시키면 가격 모델을 두 벌 갖게 되고, 기획서 §7의 여섯 요인이 호가 수급에 묻힌다.
    /// 플레이어가 보는 것은 "지금 이 가격 근처에 얼마나 물량이 있는가"이므로, 그 모습만
    /// 가격과 유동성에서 만들어 내면 판단에 필요한 정보는 모두 전달된다.
    ///
    /// 같은 틱이면 같은 호가가 나온다. 매 프레임 숫자가 흔들리면 읽을 수 없다.
    /// </summary>
    public static class OrderBook
    {
        /// <summary>
        /// 호가 단위. 가격에 비례하되 하한을 둔다. 126.8이면 0.5, 42면 0.2 정도가 된다.
        /// </summary>
        public static float TickSize(float price)
        {
            var raw = price * Constants.BOOK_TICK_RATIO;

            if (raw < Constants.BOOK_TICK_MIN)
            {
                raw = Constants.BOOK_TICK_MIN;
            }

            // 0.1 단위로 떨어지게 다듬는다. 호가가 소수점 둘째 자리까지 가면 읽기 어렵다.
            var steps = (float)Math.Round(raw / Constants.BOOK_TICK_MIN);
            return steps * Constants.BOOK_TICK_MIN;
        }

        /// <summary>
        /// 매도 호가를 1단계부터 채운다. 현재가보다 비싸다. 가장 싼 매도가 먼저 온다.
        /// </summary>
        public static void FillAsks(AdventurerStock stock, int tick, List<BookLevel> target)
        {
            Fill(stock, tick, target, OrderSide.Side_Sell);
        }

        /// <summary>
        /// 매수 호가를 1단계부터 채운다. 현재가보다 싸다. 가장 비싼 매수가 먼저 온다.
        /// </summary>
        public static void FillBids(AdventurerStock stock, int tick, List<BookLevel> target)
        {
            Fill(stock, tick, target, OrderSide.Side_Buy);
        }

        private static void Fill(AdventurerStock stock, int tick, List<BookLevel> target, OrderSide side)
        {
            if (stock == null)
            {
                throw new ArgumentNullException(nameof(stock));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.Clear();

            var step = TickSize(stock.Price);
            var sign = side == OrderSide.Side_Sell ? 1f : -1f;

            for (var i = 1; i <= Constants.BOOK_LEVELS; i++)
            {
                var price = stock.Price + sign * step * i;

                if (price < Constants.PRICE_FLOOR)
                {
                    break;
                }

                target.Add(new BookLevel
                {
                    Price = price,
                    Quantity = DepthAt(stock, tick, i, side)
                });
            }
        }

        /// <summary>
        /// 한 단계에 쌓인 수량. 유동성이 클수록 두껍고, 멀수록 두껍다.
        /// 흔들림은 틱·단계·종목에서 만든 결정적 값이라 같은 틱이면 같은 숫자가 나온다.
        /// </summary>
        public static int DepthAt(AdventurerStock stock, int tick, int level, OrderSide side)
        {
            var baseDepth = stock.Liquidity * Constants.BOOK_DEPTH_BASE;
            var grown = baseDepth * (1f + (level - 1) * Constants.BOOK_DEPTH_GROWTH);

            // 결정적 흔들림. 0.75 ~ 1.25 사이로 섞는다.
            var sideSalt = side == OrderSide.Side_Sell ? 17 : 31;
            var hash = stock.Id.GetHashCode() ^ (tick * 73856093) ^ (level * 19349663) ^ sideSalt;
            var noise = 0.75f + (hash & 0xFF) / 255f * 0.5f;

            var quantity = (int)Math.Round(grown * noise);

            if (quantity < 1)
            {
                return 1;
            }

            return quantity;
        }

        /// <summary>
        /// 지정가를 호가 단위에 맞춰 다듬는다. 호가에 없는 가격으로는 주문할 수 없다.
        /// </summary>
        public static float SnapToTick(float price, float referencePrice)
        {
            var step = TickSize(referencePrice);

            if (step <= 0f)
            {
                return price;
            }

            var steps = (float)Math.Round(price / step);
            var snapped = steps * step;

            if (snapped < Constants.PRICE_FLOOR)
            {
                return Constants.PRICE_FLOOR;
            }

            return snapped;
        }
    }
}
