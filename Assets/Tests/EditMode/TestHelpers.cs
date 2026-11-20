using System.Collections.Generic;
using WuXueMaJong.Core;

namespace WuXueMaJong.Tests.EditMode
{
    internal static class T
    {
        // 指示牌为发财 → 癞子为白板
        public const int JokerKind = TileCatalog.WhiteDragon;
        public const int IndicatorKind = TileCatalog.GreenDragon;

        public static int TileId(int kind, int copy = 0) => kind * 4 + copy;

        public static List<int> Hand(params int[] kinds)
        {
            var used = new int[TileCatalog.KindCount];
            var list = new List<int>(kinds.Length);
            foreach (int kind in kinds)
            {
                list.Add(TileId(kind, used[kind]++));
            }

            return list;
        }

        public static Meld Pong(int kind, int fromSeat = 0)
        {
            return new Meld(
                MeldType.Pong,
                new[] { TileId(kind, 0), TileId(kind, 1), TileId(kind, 2) },
                fromSeat,
                TileId(kind, 2));
        }

        public static Meld Chi(int startKind, int fromSeat = 0)
        {
            return new Meld(
                MeldType.Chi,
                new[] { TileId(startKind, 0), TileId(startKind + 1, 0), TileId(startKind + 2, 0) },
                fromSeat,
                TileId(startKind + 2, 0));
        }

        public static Meld ExposedKong(int kind, int fromSeat = 0)
        {
            return new Meld(
                MeldType.ExposedKong,
                new[] { TileId(kind, 0), TileId(kind, 1), TileId(kind, 2), TileId(kind, 3) },
                fromSeat,
                TileId(kind, 3));
        }

        public static Meld ConcealedKong(int kind)
        {
            return new Meld(
                MeldType.ConcealedKong,
                new[] { TileId(kind, 0), TileId(kind, 1), TileId(kind, 2), TileId(kind, 3) });
        }

        public static WinContext SelfDraw(bool afterKong = false, bool lastWall = false)
        {
            return new WinContext
            {
                IsSelfDraw = true,
                IsAfterKongReplacement = afterKong,
                IsLastWallDraw = lastWall
            };
        }

        public static WinContext Claim(int discarderSeat = 2, bool robKong = false)
        {
            return new WinContext
            {
                IsSelfDraw = false,
                DiscarderSeat = discarderSeat,
                IsRobKong = robKong
            };
        }

        /// <summary>生成凑数手牌，避开 31/32/33（红中/发财/白板）。</summary>
        public static List<int> Filler(int count, params int[] excluded)
        {
            var exclude = new HashSet<int>(excluded);
            var kinds = new List<int>(count);

            for (int kind = 10; kind <= 30 && kinds.Count < count; kind++)
            {
                if (!exclude.Contains(kind))
                {
                    kinds.Add(kind);
                }
            }

            for (int kind = 0; kind <= 9 && kinds.Count < count; kind++)
            {
                if (!exclude.Contains(kind))
                {
                    kinds.Add(kind);
                }
            }

            return Hand(kinds.ToArray());
        }

        public static GameEngine NewEngine(
            List<int>[] hands,
            int dealerSeat = 0,
            List<int> drawPile = null,
            RulesConfig rules = null)
        {
            var setup = new HandSetup(
                hands,
                drawPile ?? Hand(9, 10, 11, 12),
                IndicatorKind * 4 + 3,
                dealerSeat);
            return new GameEngine(setup, rules);
        }

        public static List<int>[] FourHands(
            List<int> seat0,
            List<int> seat1,
            List<int> seat2,
            List<int> seat3)
        {
            return new[] { seat0, seat1, seat2, seat3 };
        }
    }
}
