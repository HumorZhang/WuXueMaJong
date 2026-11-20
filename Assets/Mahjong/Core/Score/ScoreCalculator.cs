using System.Collections.Generic;

namespace WuXueMaJong.Core
{
    /// <summary>口口翻计分。</summary>
    public static class ScoreCalculator
    {
        public static ScoreBreakdown Calculate(
            int winnerSeat,
            WinResult win,
            WinContext context,
            IReadOnlyList<PlayerState> players,
            int dealerSeat,
            RulesConfig rules)
        {
            var breakdown = new ScoreBreakdown
            {
                WinnerSeat = winnerSeat,
                DealerSeat = dealerSeat,
                IsDraw = false
            };

            int bigCount = win?.BigHands?.Count ?? 0;
            breakdown.BasePoints = bigCount > 0 ? rules.BaseBigWin * bigCount : rules.BaseSmallWin;

            PlayerState winner = players[winnerSeat];
            long winnerMultiplier = PlayerMultiplier(winner, rules);
            if (win != null && win.IsHard)
            {
                winnerMultiplier *= rules.HardWinMultiplier;
            }

            if (context.IsSelfDraw)
            {
                winnerMultiplier *= rules.SelfDrawMultiplier;
            }

            breakdown.WinnerMultiplier = winnerMultiplier;

            for (int seat = 0; seat < Seat.Count; seat++)
            {
                if (seat == winnerSeat)
                {
                    continue;
                }

                long loserMultiplier = PlayerMultiplier(players[seat], rules);

                bool isDiscarder = !context.IsSelfDraw && seat == context.DiscarderSeat;
                if (isDiscarder)
                {
                    loserMultiplier *= rules.DiscarderMultiplier;
                }

                if (winnerSeat == dealerSeat)
                {
                    loserMultiplier *= rules.DealerMultiplier;
                }
                else if (seat == dealerSeat)
                {
                    loserMultiplier *= rules.DealerMultiplier;
                }

                long payment = breakdown.BasePoints * winnerMultiplier * loserMultiplier;
                breakdown.Payments[seat] = payment;

                string role = seat == dealerSeat ? "庄" : "闲";
                if (isDiscarder)
                {
                    role += "·放冲";
                }

                breakdown.DetailLines.Add(
                    $"座位{seat}（{role}）：基础 {breakdown.BasePoints} × 赢家 {winnerMultiplier} × 自己 {loserMultiplier} = {payment}");
            }

            if (context.IsRobKong && context.DiscarderSeat >= 0)
            {
                breakdown.IsRobKongContract = true;
                breakdown.ContractPayerSeat = context.DiscarderSeat;

                long total = 0;
                for (int seat = 0; seat < Seat.Count; seat++)
                {
                    total += breakdown.Payments[seat];
                }

                for (int seat = 0; seat < Seat.Count; seat++)
                {
                    breakdown.Payments[seat] = 0;
                }

                breakdown.Payments[context.DiscarderSeat] = total;
                breakdown.DetailLines.Add($"抢杠承包：座位{context.DiscarderSeat} 承担三家输分共 {total}");
            }

            long gain = 0;
            for (int seat = 0; seat < Seat.Count; seat++)
            {
                if (seat == winnerSeat)
                {
                    continue;
                }

                breakdown.Deltas[seat] = -breakdown.Payments[seat];
                gain += breakdown.Payments[seat];
            }

            breakdown.Deltas[winnerSeat] = gain;

            if (rules.ScoreCap != long.MaxValue && gain > rules.ScoreCap)
            {
                // 暂不启用封顶；保留参数位。
            }

            return breakdown;
        }

        public static ScoreBreakdown CreateDraw(int dealerSeat)
        {
            return new ScoreBreakdown
            {
                WinnerSeat = -1,
                DealerSeat = dealerSeat,
                IsDraw = true,
                BasePoints = 0,
                WinnerMultiplier = 0
            };
        }

        /// <summary>单个玩家的倍数：开口、明杠、暗杠、特殊杠。</summary>
        public static long PlayerMultiplier(PlayerState player, RulesConfig rules)
        {
            long multiplier = 1;

            for (int i = 0; i < player.OpenCount; i++)
            {
                multiplier *= rules.OpenMultiplier;
            }

            for (int i = 0; i < player.ExposedKongCount; i++)
            {
                multiplier *= rules.ExposedKongMultiplier;
            }

            for (int i = 0; i < player.AddedKongCount; i++)
            {
                multiplier *= rules.AddedKongMultiplier;
            }

            for (int i = 0; i < player.ConcealedKongCount; i++)
            {
                multiplier *= rules.ConcealedKongMultiplier;
            }

            int specialKongs = player.RedCenterGongCount + player.LaiziGongCount;
            for (int i = 0; i < specialKongs; i++)
            {
                multiplier *= rules.SpecialGongMultiplier;
            }

            return multiplier;
        }
    }
}
