using System.Linq;
using NUnit.Framework;
using WuXueMaJong.Core;

namespace WuXueMaJong.Tests.EditMode
{
    [TestFixture]
    public class AutoGameTests
    {
        [Test]
        public void AutoGames_CompleteWithoutErrors()
        {
            for (long seed = 1; seed <= 100; seed++)
            {
                var session = new MahjongSession((int)(seed % 4));
                GameEngine engine = session.StartHand(seed);
                BotAI[] bots = AutoGameRunner.CreateBots(seed);

                HandResult result = AutoGameRunner.RunToEnd(engine, bots);

                Assert.IsNotNull(result, $"seed {seed}");
                Assert.AreEqual(GamePhase.HandEnded, engine.Phase, $"seed {seed}");
                Assert.AreEqual(0, result.Score.Deltas.Sum(), $"seed {seed} 分数不守恒");

                if (!result.IsDraw)
                {
                    Assert.GreaterOrEqual(result.WinnerSeat, 0, $"seed {seed}");
                    Assert.Greater(result.Score.Deltas[result.WinnerSeat], 0, $"seed {seed}");
                }

                session.ApplyResult();
                Assert.AreEqual(result.NextDealerSeat, session.DealerSeat, $"seed {seed}");
                Assert.AreEqual(0, session.Scores.Sum(), $"seed {seed} 累计分不守恒");
            }
        }

        [Test]
        public void SameSeed_ProducesSameResult()
        {
            HandResult Run(long seed)
            {
                var engine = new GameEngine(WallFactory.CreateShuffled(seed, 0), RulesConfig.WuhanDefault());
                BotAI[] bots = AutoGameRunner.CreateBots(seed);
                return AutoGameRunner.RunToEnd(engine, bots);
            }

            HandResult a = Run(2026);
            HandResult b = Run(2026);

            Assert.AreEqual(a.IsDraw, b.IsDraw);
            Assert.AreEqual(a.WinnerSeat, b.WinnerSeat);
            Assert.AreEqual(a.DiscarderSeat, b.DiscarderSeat);
            CollectionAssert.AreEqual(a.Score.Deltas, b.Score.Deltas);
        }

        [Test]
        public void Session_RotatesDealerAcrossHands()
        {
            var session = new MahjongSession(0);
            GameEngine first = session.StartHand(11);
            AutoGameRunner.RunToEnd(first, AutoGameRunner.CreateBots(11));
            HandResult firstResult = first.Result;

            GameEngine second = session.StartNextHand(12);
            Assert.AreEqual(firstResult.NextDealerSeat, session.DealerSeat);
            Assert.AreEqual(2, session.HandNumber);
            Assert.AreSame(second, session.Engine);
        }
    }
}
