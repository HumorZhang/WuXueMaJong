using System;
using System.Linq;

namespace WuXueMaJong.Core
{
    public enum MeldType
    {
        Chi = 0,
        Pong = 1,
        ExposedKong = 2,
        AddedKong = 3,
        ConcealedKong = 4
    }

    /// <summary>一组已亮出（或暗杠）的面子。</summary>
    public sealed class Meld
    {
        public MeldType Type { get; }
        public int[] TileIds { get; }
        public int FromSeat { get; }
        public int ClaimedTileId { get; }

        public bool IsKong => Type == MeldType.ExposedKong || Type == MeldType.AddedKong || Type == MeldType.ConcealedKong;
        public bool IsOpen => Type != MeldType.ConcealedKong;

        public Meld(MeldType type, int[] tileIds, int fromSeat = -1, int claimedTileId = -1)
        {
            Type = type;
            TileIds = tileIds ?? throw new ArgumentNullException(nameof(tileIds));
            FromSeat = fromSeat;
            ClaimedTileId = claimedTileId;
        }

        public int KindAt(int index) => TileCatalog.KindOf(TileIds[index]);

        public int[] Kinds() => TileIds.Select(TileCatalog.KindOf).ToArray();

        public bool ContainsKind(int kind) => TileIds.Any(t => TileCatalog.KindOf(t) == kind);

        /// <summary>把碰升级为蓄杠（补上第四张）。</summary>
        public Meld UpgradeToAddedKong(int tileId)
        {
            if (Type != MeldType.Pong)
            {
                throw new InvalidOperationException("只有碰才能升级为蓄杠。");
            }

            var tiles = new int[TileIds.Length + 1];
            Array.Copy(TileIds, tiles, TileIds.Length);
            tiles[TileIds.Length] = tileId;
            return new Meld(MeldType.AddedKong, tiles, FromSeat, ClaimedTileId);
        }

        public string DisplayName()
        {
            switch (Type)
            {
                case MeldType.Chi:
                    return "吃 " + string.Join("", TileIds.Select(t => TileCatalog.DisplayName(TileCatalog.KindOf(t))));
                case MeldType.Pong:
                    return "碰 " + TileCatalog.DisplayName(KindAt(0));
                case MeldType.ExposedKong:
                    return "明杠 " + TileCatalog.DisplayName(KindAt(0));
                case MeldType.AddedKong:
                    return "补杠 " + TileCatalog.DisplayName(KindAt(0));
                default:
                    return "暗杠 " + TileCatalog.DisplayName(KindAt(0));
            }
        }
    }
}
