using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WuXueMaJong.Core;

namespace WuXueMaJong.Tests.EditMode
{
    [TestFixture]
    public class EngineTests
    {
        private const int DiscardKind = 8; // 九万

        private static GameEngine DealerDiscardSetup(
            List<int> seat1,
            List<int> seat2 = null,
            List<int> seat3 = null,
            List<int> drawPile = null,
            int dealerSeat = 0)
        {
            var hands = new[]
            {
                T.Hand(DiscardKind, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22),
                seat1,
                seat2 ?? T.Filler(13, DiscardKind),
                seat3 ?? T.Filler(13, DiscardKind)
            };

            return T.NewEngine(hands, dealerSeat, drawPile ?? T.Hand(9, 10, 11));
        }

        private static List<int> WinningConcealedWithoutNineWan()
        {
            // 123万 456万 78万 + 2筒将，胡 9万
            return T.Hand(0, 1, 2, 3, 4, 5, 6, 7, 10, 10);
        }

        [Test]
        public void Discard_NoClaims_AdvancesAndDraws()
        {
            GameEngine engine = DealerDiscardSetup(T.Filler(13, DiscardKind), drawPile: T.Hand(9));

            CommandResult result = engine.Discard(0, T.TileId(DiscardKind, 0));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(GamePhase.AwaitingDiscard, engine.Phase);
            Assert.AreEqual(1, engine.CurrentSeat);
            Assert.AreEqual(14, engine.Player(1).Hand.Count);
            Assert.AreEqual(T.TileId(9, 0), engine.LastDrawnTileId);
        }

        [Test]
        public void Pong_Claim_Works()
        {
            List<int> seat1 = T.Hand(DiscardKind, DiscardKind,
                10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            GameEngine engine = DealerDiscardSetup(seat1);

            engine.Discard(0, T.TileId(DiscardKind, 0));

            Assert.AreEqual(GamePhase.ClaimWindow, engine.Phase);
            Assert.AreEqual(1, engine.ClaimOptions.Count);
            ClaimOption option = engine.GetClaimOption(1);
            Assert.IsTrue(option.CanPong);
            Assert.IsFalse(option.CanHu);

            CommandResult claim = engine.SubmitClaim(1, ClaimDecision.Pong());

            Assert.IsTrue(claim.Success);
            Assert.AreEqual(GamePhase.AwaitingDiscard, engine.Phase);
            Assert.AreEqual(1, engine.CurrentSeat);
            Assert.AreEqual(1, engine.Player(1).Melds.Count);
            Assert.AreEqual(MeldType.Pong, engine.Player(1).Melds[0].Type);
            Assert.AreEqual(11, engine.Player(1).Hand.Count);
            Assert.AreEqual(0, engine.Player(0).Discards.Count);
        }

        [Test]
        public void Chi_OnlyOfferedToNextSeat()
        {
            List<int> seat1 = T.Hand(6, 7, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            List<int> seat2 = T.Hand(6, 7, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            GameEngine engine = DealerDiscardSetup(seat1, seat2);

            engine.Discard(0, T.TileId(DiscardKind, 0));

            Assert.AreEqual(GamePhase.ClaimWindow, engine.Phase);
            ClaimOption next = engine.GetClaimOption(1);
            ClaimOption far = engine.GetClaimOption(2);

            Assert.IsTrue(next.ChiOptions.Count > 0);
            Assert.IsNull(far);

            CommandResult claim = engine.SubmitClaim(1, ClaimDecision.ChiWith(next.ChiOptions[0]));

            Assert.IsTrue(claim.Success);
            Assert.AreEqual(1, engine.CurrentSeat);
            Assert.AreEqual(MeldType.Chi, engine.Player(1).Melds[0].Type);
        }

        [Test]
        public void Hu_BeatsPong_WhenBothDeclared()
        {
            List<int> seat1 = WinningConcealedWithoutNineWan();
            List<int> seat2 = T.Hand(DiscardKind, DiscardKind,
                10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

            GameEngine engine = DealerDiscardSetup(seat1, seat2);
            engine.Player(1).Melds.Add(T.Pong(TileCatalog.East));

            engine.Discard(0, T.TileId(DiscardKind, 0));

            Assert.IsTrue(engine.GetClaimOption(1).CanHu);
            Assert.IsTrue(engine.GetClaimOption(2).CanPong);

            engine.SubmitClaim(2, ClaimDecision.Pong());
            engine.SubmitClaim(1, ClaimDecision.Win());

            Assert.AreEqual(GamePhase.HandEnded, engine.Phase);
            Assert.IsFalse(engine.Result.IsDraw);
            Assert.AreEqual(1, engine.Result.WinnerSeat);
            Assert.AreEqual(0, engine.Player(2).Melds.Count);
        }

        [Test]
        public void Hu_NearestSeatWins_WhenBothDeclare()
        {
            List<int> seat1 = WinningConcealedWithoutNineWan();
            List<int> seat2 = WinningConcealedWithoutNineWan();

            GameEngine engine = DealerDiscardSetup(seat1, seat2);
            engine.Player(1).Melds.Add(T.Pong(TileCatalog.East));
            engine.Player(2).Melds.Add(T.Pong(TileCatalog.South));

            engine.Discard(0, T.TileId(DiscardKind, 0));

            engine.SubmitClaim(1, ClaimDecision.Win());
            engine.SubmitClaim(2, ClaimDecision.Win());

            Assert.AreEqual(GamePhase.HandEnded, engine.Phase);
            Assert.AreEqual(1, engine.Result.WinnerSeat);
        }

        [Test]
        public void Hu_FartherSeatWins_WhenNearerPasses()
        {
            List<int> seat1 = WinningConcealedWithoutNineWan();
            List<int> seat2 = WinningConcealedWithoutNineWan();

            GameEngine engine = DealerDiscardSetup(seat1, seat2);
            engine.Player(1).Melds.Add(T.Pong(TileCatalog.East));
            engine.Player(2).Melds.Add(T.Pong(TileCatalog.South));

            engine.Discard(0, T.TileId(DiscardKind, 0));

            engine.SubmitClaim(1, ClaimDecision.Pass());
            engine.SubmitClaim(2, ClaimDecision.Win());

            Assert.AreEqual(GamePhase.HandEnded, engine.Phase);
            Assert.AreEqual(2, engine.Result.WinnerSeat);
        }

        [Test]
        public void ExposedKong_FromDiscard_DrawsReplacement()
        {
            List<int> seat1 = T.Hand(DiscardKind, DiscardKind, DiscardKind,
                10, 11, 12, 13, 14, 15, 16, 17, 18, 19);
            GameEngine engine = DealerDiscardSetup(seat1, drawPile: T.Hand(9, 10, 11));

            engine.Discard(0, T.TileId(DiscardKind, 0));

            Assert.IsTrue(engine.GetClaimOption(1).CanExposedKong);
            int wallBefore = engine.Wall.Remaining;

            CommandResult claim = engine.SubmitClaim(1, ClaimDecision.Kong());

            Assert.IsTrue(claim.Success);
            Assert.AreEqual(1, engine.CurrentSeat);
            Assert.AreEqual(MeldType.ExposedKong, engine.Player(1).Melds[0].Type);
            Assert.AreEqual(11, engine.Player(1).Hand.Count);
            Assert.AreEqual(wallBefore - 1, engine.Wall.Remaining);
        }

        [Test]
        public void AddedKong_CanBeRobbed_ContractPaysAll()
        {
            var hands = new[]
            {
                T.Filler(13),
                T.Hand(DiscardKind, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19),
                WinningConcealedWithoutNineWan(),
                T.Filler(13)
            };

            GameEngine engine = T.NewEngine(hands, dealerSeat: 1, drawPile: T.Hand(9, 10, 11, 12));
            engine.Player(1).Melds.Add(T.Pong(DiscardKind));
            engine.Player(2).Melds.Add(T.Pong(TileCatalog.East));

            CommandResult kong = engine.DeclareAddedKong(1, DiscardKind);

            Assert.IsTrue(kong.Success);
            Assert.AreEqual(GamePhase.RobKongWindow, engine.Phase);
            Assert.IsTrue(engine.GetRobKongOption(2).CanHu);

            CommandResult rob = engine.SubmitRobKongDecision(2, true);

            Assert.IsTrue(rob.Success);
            Assert.AreEqual(GamePhase.HandEnded, engine.Phase);
            Assert.AreEqual(2, engine.Result.WinnerSeat);
            Assert.IsTrue(engine.Result.IsRobKong);
            Assert.AreEqual(MeldType.Pong, engine.Player(1).Melds[0].Type);

            ScoreBreakdown score = engine.Result.Score;
            Assert.IsTrue(score.IsRobKongContract);
            Assert.AreEqual(1, score.ContractPayerSeat);
            Assert.Greater(score.Payments[1], 0);
            Assert.AreEqual(0, score.Payments[0]);
            Assert.AreEqual(0, score.Payments[2]);
            Assert.AreEqual(0, score.Payments[3]);
            Assert.AreEqual(score.Payments[1], score.Deltas[2]);
            Assert.AreEqual(0, score.Deltas.Sum());
        }

        [Test]
        public void AddedKong_NotRobbed_CompletesAndDraws()
        {
            var hands = new[]
            {
                T.Filler(13),
                T.Hand(DiscardKind, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19),
                T.Filler(13),
                T.Filler(13)
            };

            GameEngine engine = T.NewEngine(hands, dealerSeat: 1, drawPile: T.Hand(9, 10, 11, 12));
            engine.Player(1).Melds.Add(T.Pong(DiscardKind));

            int wallBefore = engine.Wall.Remaining;
            CommandResult kong = engine.DeclareAddedKong(1, DiscardKind);

            Assert.IsTrue(kong.Success);
            Assert.AreEqual(GamePhase.AwaitingDiscard, engine.Phase);
            Assert.AreEqual(1, engine.CurrentSeat);
            Assert.AreEqual(MeldType.AddedKong, engine.Player(1).Melds[0].Type);
            Assert.AreEqual(11, engine.Player(1).Hand.Count);
            Assert.AreEqual(wallBefore - 1, engine.Wall.Remaining);
        }

        [Test]
        public void RedCenterGong_RemovesTileAndDraws()
        {
            var hands = new[]
            {
                T.Hand(TileCatalog.RedCenter, DiscardKind, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21),
                T.Filler(13),
                T.Filler(13),
                T.Filler(13)
            };

            GameEngine engine = T.NewEngine(hands, drawPile: T.Hand(9, 10, 11));
            int redTile = T.TileId(TileCatalog.RedCenter, 0);

            Assert.IsFalse(engine.Discard(0, redTile).Success);

            CommandResult gong = engine.DeclareRedCenterGong(0, redTile);

            Assert.IsTrue(gong.Success);
            Assert.AreEqual(1, engine.Player(0).RedCenterGongCount);
            Assert.AreEqual(14, engine.Player(0).Hand.Count);
            Assert.IsTrue(engine.AfterKongReplacement);
        }

        [Test]
        public void LaiziGong_RemovesTileAndDraws()
        {
            var hands = new[]
            {
                T.Hand(T.JokerKind, DiscardKind, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21),
                T.Filler(13),
                T.Filler(13),
                T.Filler(13)
            };

            GameEngine engine = T.NewEngine(hands, drawPile: T.Hand(9, 10, 11));
            int jokerTile = T.TileId(T.JokerKind, 0);

            Assert.IsFalse(engine.Discard(0, jokerTile).Success);

            CommandResult gong = engine.DeclareLaiziGong(0, jokerTile);

            Assert.IsTrue(gong.Success);
            Assert.AreEqual(1, engine.Player(0).LaiziGongCount);
            Assert.AreEqual(14, engine.Player(0).Hand.Count);
        }

        [Test]
        public void EmptyWall_AfterDiscard_DrawGame()
        {
            GameEngine engine = DealerDiscardSetup(
                T.Filler(13, DiscardKind),
                drawPile: new List<int>());

            engine.Discard(0, T.TileId(DiscardKind, 0));

            Assert.AreEqual(GamePhase.HandEnded, engine.Phase);
            Assert.IsTrue(engine.Result.IsDraw);
            Assert.AreEqual(0, engine.Result.NextDealerSeat);
        }

        [Test]
        public void HandResult_DealerRotation()
        {
            var draw = new HandResult { IsDraw = true, DealerSeat = 2 };
            Assert.AreEqual(2, draw.NextDealerSeat);

            var dealerWin = new HandResult { IsDraw = false, DealerSeat = 2, WinnerSeat = 2 };
            Assert.AreEqual(2, dealerWin.NextDealerSeat);

            var otherWin = new HandResult { IsDraw = false, DealerSeat = 2, WinnerSeat = 3 };
            Assert.AreEqual(3, otherWin.NextDealerSeat);

            var otherWin2 = new HandResult { IsDraw = false, DealerSeat = 3, WinnerSeat = 0 };
            Assert.AreEqual(0, otherWin2.NextDealerSeat);
        }
    }
}
