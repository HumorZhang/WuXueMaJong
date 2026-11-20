using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WuXueMaJong.Core;

namespace WuXueMaJong.Tests.EditMode
{
    [TestFixture]
    public class TileAndWallTests
    {
        [Test]
        public void NextJokerKind_NumbersCycleWithinSuit()
        {
            Assert.AreEqual(5, TileCatalog.NextJokerKind(4));   // 五万 → 六万
            Assert.AreEqual(0, TileCatalog.NextJokerKind(8));   // 九万 → 一万
            Assert.AreEqual(9, TileCatalog.NextJokerKind(17));  // 九筒 → 一筒
            Assert.AreEqual(18, TileCatalog.NextJokerKind(26)); // 九条 → 一条
            Assert.AreEqual(4, TileCatalog.NextJokerKind(3));   // 四万 → 五万
        }

        [Test]
        public void NextJokerKind_HonorsSkipRedCenter()
        {
            Assert.AreEqual(TileCatalog.South, TileCatalog.NextJokerKind(TileCatalog.East));
            Assert.AreEqual(TileCatalog.West, TileCatalog.NextJokerKind(TileCatalog.South));
            Assert.AreEqual(TileCatalog.North, TileCatalog.NextJokerKind(TileCatalog.West));
            Assert.AreEqual(TileCatalog.GreenDragon, TileCatalog.NextJokerKind(TileCatalog.North));
            Assert.AreEqual(TileCatalog.WhiteDragon, TileCatalog.NextJokerKind(TileCatalog.GreenDragon));
            Assert.AreEqual(TileCatalog.East, TileCatalog.NextJokerKind(TileCatalog.WhiteDragon));
            Assert.AreEqual(TileCatalog.GreenDragon, TileCatalog.NextJokerKind(TileCatalog.RedCenter));
        }

        [Test]
        public void PairRank_OnlyNumbersTwoFiveEight()
        {
            Assert.IsTrue(TileCatalog.IsPairRank(1));  // 二万
            Assert.IsTrue(TileCatalog.IsPairRank(4));  // 五万
            Assert.IsTrue(TileCatalog.IsPairRank(7));  // 八万
            Assert.IsFalse(TileCatalog.IsPairRank(0)); // 一万
            Assert.IsFalse(TileCatalog.IsPairRank(2)); // 三万
            Assert.IsFalse(TileCatalog.IsPairRank(TileCatalog.East));
        }

        [Test]
        public void WallFactory_DealsCorrectlyAndUsesAllTiles()
        {
            HandSetup setup = WallFactory.CreateShuffled(12345, 2);

            Assert.AreEqual(13, setup.Hands[0].Count);
            Assert.AreEqual(13, setup.Hands[1].Count);
            Assert.AreEqual(14, setup.Hands[2].Count); // 庄家
            Assert.AreEqual(13, setup.Hands[3].Count);
            Assert.AreEqual(82, setup.DrawPile.Count);

            var all = new List<int>(setup.DrawPile);
            all.Add(setup.IndicatorTileId);
            foreach (List<int> hand in setup.Hands)
            {
                all.AddRange(hand);
            }

            Assert.AreEqual(136, all.Count);
            CollectionAssert.AreEquivalent(
                Enumerable.Range(0, 136).ToList(),
                all);

            Assert.AreEqual(
                TileCatalog.NextJokerKind(TileCatalog.KindOf(setup.IndicatorTileId)),
                setup.JokerKind);
        }

        [Test]
        public void WallFactory_SameSeedSameDeal()
        {
            HandSetup a = WallFactory.CreateShuffled(777, 0);
            HandSetup b = WallFactory.CreateShuffled(777, 0);

            CollectionAssert.AreEqual(a.DrawPile, b.DrawPile);
            for (int seat = 0; seat < 4; seat++)
            {
                CollectionAssert.AreEqual(a.Hands[seat], b.Hands[seat]);
            }

            Assert.AreEqual(a.IndicatorTileId, b.IndicatorTileId);
        }

        [Test]
        public void WallFactory_DifferentSeedDifferentDeal()
        {
            HandSetup a = WallFactory.CreateShuffled(1, 0);
            HandSetup b = WallFactory.CreateShuffled(2, 0);

            bool different = !a.DrawPile.SequenceEqual(b.DrawPile) ||
                             a.IndicatorTileId != b.IndicatorTileId;
            Assert.IsTrue(different);
        }

        [Test]
        public void Wall_DrawsUntilEmpty()
        {
            HandSetup setup = WallFactory.CreateShuffled(99, 0);
            var wall = new Wall(setup);

            Assert.AreEqual(82, wall.Remaining);
            for (int i = 0; i < 82; i++)
            {
                Assert.IsTrue(wall.TryDraw(out _));
            }

            Assert.IsTrue(wall.IsEmpty);
            Assert.IsFalse(wall.TryDraw(out _));
        }
    }
}
