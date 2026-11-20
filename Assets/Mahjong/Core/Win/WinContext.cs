namespace WuXueMaJong.Core
{
    /// <summary>胡牌时的上下文（怎么胡的）。</summary>
    public sealed class WinContext
    {
        public bool IsSelfDraw;
        public bool IsAfterKongReplacement;
        public bool IsLastWallDraw;
        public bool IsRobKong;
        public int WinningTileId = -1;
        public int DiscarderSeat = -1;
    }
}
