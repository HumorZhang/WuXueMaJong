using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WuXueMaJong.Core;

namespace WuXueMaJong.Tests.EditMode
{
    [TestFixture]
    public class WinCheckerTests
    {
        private readonly RulesConfig _rules = RulesConfig.WuhanDefault();

        private static List<Meld> OpenPong()
        {
            return new List<Meld> { T.Pong(TileCatalog.East) };
        }

        [Test]
        public void StandardHand_With258Pair_Wins()
        {
            // 碰东 + 123万 456万 789万 + 2筒将
            List<int> hand = T.Hand(0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 10);
            List<Meld> melds = OpenPong();

            WinResult win = WinChecker.Evaluate(hand, melds, T.JokerKind, T.SelfDraw(), _rules);

            Assert.IsTrue(win.CanWin);
            Assert.IsTrue(win.IsHard);
            Assert.AreEqual(0, win.WildcardUsed);
            Assert.AreEqual(10, win.PairKind); // 二筒
            Assert.IsEmpty(win.BigHands);
        }

        [Test]
        public void StandardHand_WithNon258Pair_Loses()
        {
            List<int> hand = T.Hand(0, 1, 2, 3, 4, 5, 6, 7, 8, 11, 11); // 3筒将
            WinResult win = WinChecker.Evaluate(hand, OpenPong(), T.JokerKind, T.SelfDraw(), _rules);

            Assert.IsFalse(win.CanWin);
        }

        [Test]
        public void PengPengHu_AllowsAnyPair()
        {
            // 碰东 + 111万 222筒 333条 + 33筒将
            List<int> hand = T.Hand(0, 0, 0, 10, 10, 10, 20, 20, 20, 11, 11);
            WinResult win = WinChecker.Evaluate(hand, OpenPong(), T.JokerKind, T.SelfDraw(), _rules);

            Assert.IsTrue(win.CanWin);
            Assert.Contains(BigHandType.PengPengHu, win.BigHands);
        }

        [Test]
        public void QingYiSe_AllowsAnyPair()
        {
            // 吃123万 + 456万 789万 123万 + 55万将
            var melds = new List<Meld> { T.Chi(0) };
            List<int> hand = T.Hand(3, 4, 5, 6, 7, 8, 0, 1, 2, 4, 4);

            WinResult win = WinChecker.Evaluate(hand, melds, T.JokerKind, T.SelfDraw(), _rules);

            Assert.IsTrue(win.CanWin);
            Assert.Contains(BigHandType.QingYiSe, win.BigHands);
        }

        [Test]
        public void JiangYiSe_AllTilesAre258()
        {
            // 碰2万 + 555万 888筒 222条 + 88万将
            var melds = new List<Meld> { T.Pong(1) };
            List<int> hand = T.Hand(4, 4, 4, 16, 16, 16, 19, 19, 19, 7, 7);

            WinResult win = WinChecker.Evaluate(hand, melds, T.JokerKind, T.SelfDraw(), _rules);

            Assert.IsTrue(win.CanWin);
            Assert.Contains(BigHandType.JiangYiSe, win.BigHands);
        }

        [Test]
        public void FengYiSe_AllWindsAndDragonsAllowed()
        {
            // 碰东 + 南南南 西西西 北北北 + 发发将
            List<int> hand = T.Hand(28, 28, 28, 29, 29, 29, 30, 30, 30, 32, 32);
            WinResult win = WinChecker.Evaluate(hand, OpenPong(), T.JokerKind, T.SelfDraw(), _rules);

            Assert.IsTrue(win.CanWin);
            Assert.Contains(BigHandType.FengYiSe, win.BigHands);
        }

        [Test]
        public void WithoutOpening_CannotWin()
        {
            List<int> hand = T.Hand(0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 10, 10);

            WinResult win = WinChecker.Evaluate(hand, new List<Meld>(), T.JokerKind, T.SelfDraw(), _rules);

            Assert.IsFalse(win.CanWin);
        }

        [Test]
        public void RedCenterInHand_CannotWin()
        {
            List<int> hand = T.Hand(0, 1, 2, 3, 4, 5, 6, 7, 8, TileCatalog.RedCenter, 10);

            WinResult win = WinChecker.Evaluate(hand, OpenPong(), T.JokerKind, T.SelfDraw(), _rules);

            Assert.IsFalse(win.CanWin);
        }

        [Test]
        public void QuanQiuRen_OnlyOnDiscard()
        {
            var melds = new List<Meld>
            {
                T.Pong(TileCatalog.East),
                T.Pong(TileCatalog.South),
                T.Pong(TileCatalog.West),
                T.Pong(TileCatalog.North)
            };

            List<int> handWithoutWinTile = T.Hand(1); // 一张2万，胡另一张2万
            WinResult discardWin = WinChecker.EvaluateWithWinningTile(
                handWithoutWinTile, T.TileId(1, 1), melds, T.JokerKind, T.Claim(2), _rules);

            Assert.IsTrue(discardWin.CanWin);
            Assert.Contains(BigHandType.QuanQiuRen, discardWin.BigHands);

            List<int> pairHand = T.Hand(1, 1);
            WinResult selfDraw = WinChecker.Evaluate(pairHand, melds, T.JokerKind, T.SelfDraw(), _rules);
            Assert.IsTrue(selfDraw.CanWin);
            Assert.IsFalse(selfDraw.BigHands.Contains(BigHandType.QuanQiuRen));
        }

        [Test]
        public void ContextFlags_AddBigHands()
        {
            List<int> hand = T.Hand(0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 10);
            List<Meld> melds = OpenPong();

            Assert.Contains(
                BigHandType.GangShangHua,
                WinChecker.Evaluate(hand, melds, T.JokerKind, T.SelfDraw(afterKong: true), _rules).BigHands);

            Assert.Contains(
                BigHandType.HaiDiLao,
                WinChecker.Evaluate(hand, melds, T.JokerKind, T.SelfDraw(lastWall: true), _rules).BigHands);

            Assert.Contains(
                BigHandType.QiangGang,
                WinChecker.Evaluate(hand, melds, T.JokerKind, T.Claim(2, robKong: true), _rules).BigHands);
        }

        [Test]
        public void JokerUsedAsItself_IsHardWin()
        {
            // 癞子为六万（kind 5），手牌自然组成 456万
            List<int> hand = T.Hand(0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 10);
            WinResult win = WinChecker.Evaluate(hand, OpenPong(), 5, T.SelfDraw(), _rules);

            Assert.IsTrue(win.CanWin);
            Assert.IsTrue(win.IsHard);
            Assert.AreEqual(0, win.WildcardUsed);
        }

        [Test]
        public void JokerSubstitutesOtherSuit_IsSoftWin()
        {
            // 癞子为六万，但用来补 6筒，形成 4-5-6筒 与 7-8-9筒
            List<int> hand = T.Hand(0, 1, 2, 12, 13, 15, 16, 17, 10, 10, 5);
            WinResult win = WinChecker.Evaluate(hand, OpenPong(), 5, T.SelfDraw(), _rules);

            Assert.IsTrue(win.CanWin);
            Assert.IsFalse(win.IsHard);
            Assert.AreEqual(1, win.WildcardUsed);
        }

        [Test]
        public void TwoWildcards_SmallHandRejected()
        {
            // 两张癞子分别补 6筒、7筒，形成小胡 → 不允许
            List<int> hand = T.Hand(0, 1, 2, 12, 13, 16, 17, 19, 19, 5, 5);
            WinResult win = WinChecker.Evaluate(hand, OpenPong(), 5, T.SelfDraw(), _rules);

            Assert.IsFalse(win.CanWin);
        }

        [Test]
        public void TwoWildcards_BigHandAllowed()
        {
            // 碰东 + 111万 222筒 + (5筒+两张癞子) + 33条将 → 碰碰胡
            List<int> hand = T.Hand(0, 0, 0, 10, 10, 10, 20, 20, 13, 5, 5);
            WinResult win = WinChecker.Evaluate(hand, OpenPong(), 5, T.SelfDraw(), _rules);

            Assert.IsTrue(win.CanWin);
            Assert.Contains(BigHandType.PengPengHu, win.BigHands);
            Assert.AreEqual(2, win.WildcardUsed);
        }

        [Test]
        public void MeldsContainingJoker_Rejected()
        {
            var melds = new List<Meld> { T.Pong(5) }; // 碰癞子牌不合法
            List<int> hand = T.Hand(0, 1, 2, 3, 4, 6, 7, 8, 9, 10, 10);

            WinResult win = WinChecker.Evaluate(hand, melds, 5, T.SelfDraw(), _rules);

            Assert.IsFalse(win.CanWin);
        }
    }
}
