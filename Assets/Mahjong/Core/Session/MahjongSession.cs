using System;

namespace WuXueMaJong.Core
{
    /// <summary>多局会话：累计分数与庄家轮转。</summary>
    public sealed class MahjongSession
    {
        public long[] Scores { get; } = new long[Seat.Count];
        public int DealerSeat { get; private set; }
        public int HandNumber { get; private set; }
        public HandResult LastResult { get; private set; }
        public GameEngine Engine { get; private set; }

        private bool _resultApplied;

        public MahjongSession(int dealerSeat = 0)
        {
            if (dealerSeat < 0 || dealerSeat >= Seat.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(dealerSeat));
            }

            DealerSeat = dealerSeat;
        }

        public GameEngine StartHand(long seed, RulesConfig rules = null)
        {
            Engine = new GameEngine(WallFactory.CreateShuffled(seed, DealerSeat), rules);
            HandNumber++;
            _resultApplied = false;
            return Engine;
        }

        public GameEngine StartNextHand(long seed, RulesConfig rules = null)
        {
            ApplyResult();
            return StartHand(seed, rules);
        }

        public void ApplyResult()
        {
            if (Engine == null || Engine.Result == null || _resultApplied)
            {
                return;
            }

            HandResult result = Engine.Result;
            for (int seat = 0; seat < Seat.Count; seat++)
            {
                Scores[seat] += result.Score.Deltas[seat];
            }

            DealerSeat = result.NextDealerSeat;
            LastResult = result;
            _resultApplied = true;
        }
    }
}
