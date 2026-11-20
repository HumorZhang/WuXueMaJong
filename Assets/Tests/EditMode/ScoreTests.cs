using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WuXueMaJong.Core;

namespace WuXueMaJong.Tests.EditMode
{
    [TestFixture]
    public class ScoreTests
    {
        private readonly RulesConfig _rules = RulesConfig.WuhanDefault();

        private static PlayerState[] NewPlayers()
        {
            var players = new PlayerState[4];
            for (int seat = 0; seat < 4; seat++)
            {
                players[seat] = new PlayerState(seat);
            }

            return players;
        }

        private static WinResult SmallHardWin()
        {
            return new WinResult
            {
                CanWin = true,
                IsHard = true,
                WildcardUsed = 0,
                PairKind = 10,
                BigHands = new List<BigHandType>()
            };
        }

        [Test]
        public void SelfDraw_Hard_OneOpen_CalculatesMultipliers()
        {
            PlayerState[] players = NewPlayers();
            players[1].Melds.Add(T.Pong(TileCatalog.East));

            ScoreBreakdown score = ScoreCalculator.Calculate(
                1, SmallHardWin(), new WinContext { IsSelfDraw = true }, players, 0, _rules);

            Assert.AreEqual(1, score.BasePoints);
            Assert.AreEqual(8, score.WinnerMultiplier); // 开口2 × 硬胡2 × 自摸2
            Assert.AreEqual(16, score.Payments[0]);      // 庄家再 ×2
            Assert.AreEqual(8, score.Payments[2]);
            Assert.AreEqual(8, score.Payments[3]);
            Assert.AreEqual(32, score.Deltas[1]);
            Assert.AreEqual(-16, score.Deltas[0]);
            Assert.AreEqual(0, score.Deltas.Sum());
        }

        [Test]
        public void DiscardWin_DiscarderPaysDouble()
        {
            PlayerState[] players = NewPlayers();
            players[1].Melds.Add(T.Pong(TileCatalog.East));

            var context = new WinContext { IsSelfDraw = false, DiscarderSeat = 2 };
            ScoreBreakdown score = ScoreCalculator.Calculate(1, SmallHardWin(), context, players, 0, _rules);

            Assert.AreEqual(4, score.WinnerMultiplier); // 开口2 × 硬胡2
            Assert.AreEqual(8, score.Payments[0]);      // 庄家 ×2
            Assert.AreEqual(8, score.Payments[2]);      // 放冲 ×2
            Assert.AreEqual(4, score.Payments[3]);
            Assert.AreEqual(20, score.Deltas[1]);
            Assert.AreEqual(0, score.Deltas.Sum());
        }

        [Test]
        public void MultipleBigHands_IncreaseBasePoints()
        {
            PlayerState[] players = NewPlayers();
            WinResult win = SmallHardWin();
            win.BigHands = new List<BigHandType> { BigHandType.PengPengHu, BigHandType.QingYiSe };

            ScoreBreakdown score = ScoreCalculator.Calculate(
                1, win, new WinContext { IsSelfDraw = false, DiscarderSeat = 2 }, players, 0, _rules);

            Assert.AreEqual(20, score.BasePoints);
        }

        [Test]
        public void PlayerMultiplier_CountsOpensKongsAndSpecialGongs()
        {
            var player = new PlayerState(0);
            player.Melds.Add(T.Pong(TileCatalog.East));
            player.Melds.Add(T.ExposedKong(TileCatalog.South));
            player.Melds.Add(T.ConcealedKong(TileCatalog.West));
            player.SpecialGongs.Add(new SpecialGong(
                SpecialGongType.RedCenter, T.TileId(TileCatalog.RedCenter, 0)));

            long multiplier = ScoreCalculator.PlayerMultiplier(player, _rules);

            // 开口2次 2^2=4 × 明杠2 × 暗杠4 × 红中杠2 = 64
            Assert.AreEqual(64, multiplier);
        }

        [Test]
        public void Draw_HasNoScoreChange()
        {
            ScoreBreakdown score = ScoreCalculator.CreateDraw(2);

            Assert.IsTrue(score.IsDraw);
            Assert.AreEqual(0, score.Deltas.Sum());
            Assert.IsTrue(score.Deltas.All(d => d == 0));
        }
    }
}
