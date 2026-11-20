namespace WuXueMaJong.Core
{
    public enum SpecialGongType
    {
        RedCenter = 0,
        Laizi = 1
    }

    /// <summary>红中杠或癞子杠（单张成杠，不属于面子）。</summary>
    public sealed class SpecialGong
    {
        public SpecialGongType Type { get; }
        public int TileId { get; }

        public SpecialGong(SpecialGongType type, int tileId)
        {
            Type = type;
            TileId = tileId;
        }

        public string DisplayName()
        {
            string prefix = Type == SpecialGongType.RedCenter ? "红中杠" : "癞子杠";
            return prefix + " " + TileCatalog.DisplayName(TileCatalog.KindOf(TileId));
        }
    }
}
