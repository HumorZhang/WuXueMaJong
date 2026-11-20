using System;

namespace WuXueMaJong.Core
{
    /// <summary>
    /// 牌种编码与基础工具。
    /// 物理牌 TileId：0..135，每 4 张同牌种连续。
    /// 牌种 Kind：0..33 = TileId / 4。
    /// 0-8 一万..九万；9-17 一筒..九筒；18-26 一条..九条；
    /// 27-30 东南西北；31 红中；32 发财；33 白板。
    /// </summary>
    public static class TileCatalog
    {
        public const int KindCount = 34;
        public const int TileCount = 136;

        public const int WanStart = 0;
        public const int TongStart = 9;
        public const int TiaoStart = 18;
        public const int East = 27;
        public const int South = 28;
        public const int West = 29;
        public const int North = 30;
        public const int RedCenter = 31;
        public const int GreenDragon = 32;
        public const int WhiteDragon = 33;

        private static readonly string[] NumberNames = { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        private static readonly string[] SuitNames = { "万", "筒", "条" };
        private static readonly string[] HonorNames = { "东", "南", "西", "北", "红中", "发财", "白板" };

        public static int KindOf(int tileId) => tileId / 4;
        public static int CopyOf(int tileId) => tileId % 4;
        public static int MakeTileId(int kind, int copy) => kind * 4 + copy;

        public static bool IsValidKind(int kind) => kind >= 0 && kind < KindCount;
        public static bool IsValidTile(int tileId) => tileId >= 0 && tileId < TileCount;
        public static bool IsSuited(int kind) => kind >= 0 && kind < 27;
        public static bool IsHonor(int kind) => kind >= 27 && kind < KindCount;
        public static bool IsWind(int kind) => kind >= East && kind <= North;
        public static bool IsDragon(int kind) => kind >= RedCenter && kind <= WhiteDragon;
        public static bool IsRedCenter(int kind) => kind == RedCenter;

        /// <summary>0=万、1=筒、2=条；字牌返回 -1。</summary>
        public static int SuitOf(int kind) => IsSuited(kind) ? kind / 9 : -1;

        /// <summary>数牌返回 1..9；字牌返回 0。</summary>
        public static int RankOf(int kind) => IsSuited(kind) ? kind % 9 + 1 : 0;

        public static bool IsTerminal(int kind) => IsSuited(kind) && (RankOf(kind) == 1 || RankOf(kind) == 9);

        /// <summary>是否为二、五、八（将牌候选）。</summary>
        public static bool IsPairRank(int kind) => IsSuited(kind) && (RankOf(kind) == 2 || RankOf(kind) == 5 || RankOf(kind) == 8);

        /// <summary>
        /// 翻牌定癞子：指示牌的「下一张」。
        /// 数牌同花色 1→2→…→9→1；字牌 东→南→西→北→发财→白板→东（跳过红中）。
        /// </summary>
        public static int NextJokerKind(int indicatorKind)
        {
            if (!IsValidKind(indicatorKind))
            {
                throw new ArgumentOutOfRangeException(nameof(indicatorKind));
            }

            if (IsSuited(indicatorKind))
            {
                int suitBase = indicatorKind - indicatorKind % 9;
                return suitBase + (indicatorKind % 9 + 1) % 9;
            }

            switch (indicatorKind)
            {
                case East: return South;
                case South: return West;
                case West: return North;
                case North: return GreenDragon; // 跳过红中
                case RedCenter: return GreenDragon;
                case GreenDragon: return WhiteDragon;
                case WhiteDragon: return East;
                default: throw new ArgumentOutOfRangeException(nameof(indicatorKind));
            }
        }

        public static string DisplayName(int kind)
        {
            if (!IsValidKind(kind))
            {
                return "?";
            }

            if (IsSuited(kind))
            {
                return NumberNames[RankOf(kind) - 1] + SuitNames[SuitOf(kind)];
            }

            return HonorNames[kind - East];
        }

        /// <summary>用于排序与副露展示的简短名称。</summary>
        public static string ShortName(int kind) => DisplayName(kind);
    }
}
