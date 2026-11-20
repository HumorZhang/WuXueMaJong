using System.Collections.Generic;

namespace WuXueMaJong.Core
{
    /// <summary>一局结算明细。Payments 为正数表示该家应付的分数。</summary>
    public sealed class ScoreBreakdown
    {
        public int WinnerSeat = -1;
        public int DealerSeat;
        public bool IsDraw;
        public long BasePoints;
        public long WinnerMultiplier = 1;
        public long[] Payments = new long[Seat.Count];
        public long[] Deltas = new long[Seat.Count];
        public List<string> DetailLines = new List<string>();
        public bool IsRobKongContract;
        public int ContractPayerSeat = -1;

        public long TotalGain
        {
            get
            {
                long sum = 0;
                foreach (long delta in Deltas)
                {
                    if (delta > 0)
                    {
                        sum += delta;
                    }
                }

                return sum;
            }
        }
    }
}
