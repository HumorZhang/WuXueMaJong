using System;
using System.Collections.Generic;

namespace WuXueMaJong.Core
{
    /// <summary>一局开始时的手牌与牌墙配置，可随机生成，也可在测试中固定。</summary>
    public sealed class HandSetup
    {
        public List<int>[] Hands { get; }
        public List<int> DrawPile { get; }
        public int IndicatorTileId { get; }
        public int DealerSeat { get; }
        public int JokerKind => TileCatalog.NextJokerKind(TileCatalog.KindOf(IndicatorTileId));

        public HandSetup(List<int>[] hands, List<int> drawPile, int indicatorTileId, int dealerSeat)
        {
            if (hands == null || hands.Length != Seat.Count)
            {
                throw new ArgumentException("必须提供 4 家手牌。", nameof(hands));
            }

            if (!TileCatalog.IsValidTile(indicatorTileId))
            {
                throw new ArgumentOutOfRangeException(nameof(indicatorTileId));
            }

            if (dealerSeat < 0 || dealerSeat >= Seat.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(dealerSeat));
            }

            Hands = hands;
            DrawPile = drawPile ?? new List<int>();
            IndicatorTileId = indicatorTileId;
            DealerSeat = dealerSeat;
        }
    }
}
