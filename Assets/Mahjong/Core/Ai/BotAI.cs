using System;
using System.Collections.Generic;
using System.Linq;

namespace WuXueMaJong.Core
{
    /// <summary>简单启发式 AI：能胡则胡，优先开口/杠，打出最没用的牌。</summary>
    public sealed class BotAI
    {
        private readonly int _seat;
        private readonly DeterministicRandom _rng;

        public BotAI(int seat, long seed)
        {
            _seat = seat;
            _rng = new DeterministicRandom(seed + 7919L * (seat + 1));
        }

        public CommandResult ActTurn(GameEngine engine, BotObservation observation, LegalActionSet legal)
        {
            if (legal.CanWin)
            {
                CommandResult win = engine.DeclareWin(_seat);
                if (win.Success)
                {
                    return win;
                }
            }

            if (legal.RedCenterGongTileIds.Count > 0)
            {
                CommandResult gong = engine.DeclareRedCenterGong(_seat, legal.RedCenterGongTileIds[0]);
                if (gong.Success)
                {
                    return gong;
                }
            }

            if (legal.LaiziGongTileIds.Count > 0 && observation.JokerCount >= 3)
            {
                CommandResult gong = engine.DeclareLaiziGong(_seat, legal.LaiziGongTileIds[0]);
                if (gong.Success)
                {
                    return gong;
                }
            }

            if (legal.ConcealedKongKinds.Count > 0)
            {
                CommandResult kong = engine.DeclareConcealedKong(_seat, legal.ConcealedKongKinds[0]);
                if (kong.Success)
                {
                    return kong;
                }
            }

            if (legal.AddedKongKinds.Count > 0)
            {
                CommandResult kong = engine.DeclareAddedKong(_seat, legal.AddedKongKinds[0]);
                if (kong.Success)
                {
                    return kong;
                }
            }

            List<int> candidates = legal.DiscardableTileIds;
            if (candidates.Count == 0)
            {
                return CommandResult.Fail("AI 没有可打出的牌。");
            }

            int best = candidates[0];
            int bestScore = int.MaxValue;
            foreach (int tileId in candidates)
            {
                int kind = TileCatalog.KindOf(tileId);
                int score = HandEvaluator.KeepScore(observation.Hand, observation.JokerKind, kind);
                score += _rng.Next(5); // 小幅随机打破平局
                if (score < bestScore)
                {
                    bestScore = score;
                    best = tileId;
                }
            }

            return engine.Discard(_seat, best);
        }

        public ClaimDecision DecideClaim(ClaimOption option, BotObservation observation)
        {
            if (option == null)
            {
                return ClaimDecision.Pass();
            }

            if (option.CanHu)
            {
                return ClaimDecision.Win();
            }

            if (option.CanExposedKong)
            {
                return ClaimDecision.Kong();
            }

            if (option.CanPong)
            {
                bool keepPair = observation.HasOpened &&
                                TileCatalog.IsPairRank(option.TileKind) &&
                                observation.CountInHand(option.TileKind) == 2 &&
                                HandEvaluator.CountPairRankPairs(observation.Hand) <= 1;
                if (!keepPair)
                {
                    return ClaimDecision.Pong();
                }
            }

            if (option.ChiOptions.Count > 0)
            {
                if (!observation.HasOpened)
                {
                    return ClaimDecision.ChiWith(option.ChiOptions[0]);
                }

                // 已开口：只在风险较低时吃（这里简单选择不影响将牌的组合）
                ChiOption choice = option.ChiOptions.FirstOrDefault(c => !ContainsPairRank(c.TileIdA, c.TileIdB));
                if (choice != null && _rng.Next(100) < 40)
                {
                    return ClaimDecision.ChiWith(choice);
                }
            }

            return ClaimDecision.Pass();
        }

        private static bool ContainsPairRank(int tileIdA, int tileIdB)
        {
            return TileCatalog.IsPairRank(TileCatalog.KindOf(tileIdA)) ||
                   TileCatalog.IsPairRank(TileCatalog.KindOf(tileIdB));
        }
    }
}
