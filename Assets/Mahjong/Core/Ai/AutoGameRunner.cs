using System;
using System.Collections.Generic;
using System.Linq;

namespace WuXueMaJong.Core
{
    /// <summary>全 AI 自动对局，用于测试与观战模式。</summary>
    public static class AutoGameRunner
    {
        public static HandResult RunToEnd(GameEngine engine, BotAI[] bots, int maxSteps = 20000)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (bots == null || bots.Length != Seat.Count)
            {
                throw new ArgumentException("需要 4 个 AI。", nameof(bots));
            }

            int steps = 0;
            while (engine.Phase != GamePhase.HandEnded)
            {
                if (++steps > maxSteps)
                {
                    throw new InvalidOperationException("自动对局步数超限，可能出现死循环。");
                }

                switch (engine.Phase)
                {
                    case GamePhase.AwaitingDiscard:
                    {
                        int seat = engine.CurrentSeat;
                        BotObservation observation = BotObservation.From(engine, seat);
                        LegalActionSet legal = engine.GetLegalActions(seat);
                        CommandResult result = bots[seat].ActTurn(engine, observation, legal);
                        if (!result.Success)
                        {
                            throw new InvalidOperationException(
                                $"AI 动作失败（座位{seat}）：{result.Error}");
                        }

                        break;
                    }
                    case GamePhase.ClaimWindow:
                    {
                        List<int> pending = engine.PendingClaimSeats.ToList();
                        if (pending.Count == 0)
                        {
                            throw new InvalidOperationException("响应窗口没有待处理座位。");
                        }

                        foreach (int seat in pending)
                        {
                            ClaimOption option = engine.GetClaimOption(seat);
                            BotObservation observation = BotObservation.From(engine, seat);
                            ClaimDecision decision = bots[seat].DecideClaim(option, observation);
                            CommandResult result = engine.SubmitClaim(seat, decision);
                            if (!result.Success)
                            {
                                throw new InvalidOperationException(
                                    $"AI 响应失败（座位{seat}）：{result.Error}");
                            }
                        }

                        break;
                    }
                    case GamePhase.RobKongWindow:
                    {
                        List<int> pending = engine.PendingRobKongSeats.ToList();
                        if (pending.Count == 0)
                        {
                            throw new InvalidOperationException("抢杠窗口没有待处理座位。");
                        }

                        foreach (int seat in pending)
                        {
                            RobKongOption option = engine.GetRobKongOption(seat);
                            CommandResult result = engine.SubmitRobKongDecision(seat, option != null && option.CanHu);
                            if (!result.Success)
                            {
                                throw new InvalidOperationException(
                                    $"AI 抢杠响应失败（座位{seat}）：{result.Error}");
                            }
                        }

                        break;
                    }
                    case GamePhase.NotStarted:
                        throw new InvalidOperationException("对局尚未开始。");
                }
            }

            return engine.Result;
        }

        public static BotAI[] CreateBots(long seed)
        {
            var bots = new BotAI[Seat.Count];
            for (int seat = 0; seat < Seat.Count; seat++)
            {
                bots[seat] = new BotAI(seat, seed);
            }

            return bots;
        }
    }
}
