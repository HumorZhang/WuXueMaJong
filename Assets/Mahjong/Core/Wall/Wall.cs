using System;
using System.Collections.Generic;

namespace WuXueMaJong.Core
{
    /// <summary>洗牌与发牌。庄家 14 张、闲家 13 张，发牌后翻指示牌。</summary>
    public static class WallFactory
    {
        public static HandSetup CreateShuffled(long seed, int dealerSeat)
        {
            if (dealerSeat < 0 || dealerSeat >= Seat.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(dealerSeat));
            }

            var rng = new DeterministicRandom(seed);
            var tiles = new List<int>(TileCatalog.TileCount);
            for (int i = 0; i < TileCatalog.TileCount; i++)
            {
                tiles.Add(i);
            }

            rng.Shuffle(tiles);

            var hands = new List<int>[Seat.Count];
            for (int i = 0; i < Seat.Count; i++)
            {
                hands[i] = new List<int>(14);
            }

            int index = 0;
            for (int round = 0; round < 13; round++)
            {
                for (int offset = 0; offset < Seat.Count; offset++)
                {
                    int seat = (dealerSeat + offset) % Seat.Count;
                    hands[seat].Add(tiles[index++]);
                }
            }

            hands[dealerSeat].Add(tiles[index++]);

            int indicator = tiles[index++];
            var drawPile = tiles.GetRange(index, tiles.Count - index);

            return new HandSetup(hands, drawPile, indicator, dealerSeat);
        }
    }

    /// <summary>对局中消耗的牌墙（不含已发出的手牌与指示牌）。</summary>
    public sealed class Wall
    {
        private readonly List<int> _drawPile;
        private int _nextIndex;

        public int IndicatorTileId { get; }
        public int JokerKind { get; }
        public int Remaining => _drawPile.Count - _nextIndex;
        public bool IsEmpty => Remaining == 0;

        public Wall(HandSetup setup)
        {
            _drawPile = setup.DrawPile;
            IndicatorTileId = setup.IndicatorTileId;
            JokerKind = setup.JokerKind;
        }

        public bool TryDraw(out int tileId)
        {
            if (_nextIndex >= _drawPile.Count)
            {
                tileId = -1;
                return false;
            }

            tileId = _drawPile[_nextIndex++];
            return true;
        }

        public int Draw()
        {
            if (!TryDraw(out int tileId))
            {
                throw new InvalidOperationException("牌墙已空。");
            }

            return tileId;
        }
    }
}
