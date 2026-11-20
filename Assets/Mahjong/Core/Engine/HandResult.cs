namespace WuXueMaJong.Core
{
    public sealed class HandResult
    {
        public bool IsDraw;
        public int WinnerSeat = -1;
        public int DiscarderSeat = -1;
        public bool IsRobKong;
        public int DealerSeat;
        public WinContext Context;
        public WinResult Win;
        public ScoreBreakdown Score;

        public bool DealerContinues => IsDraw || WinnerSeat == DealerSeat;

        public int NextDealerSeat => DealerContinues ? DealerSeat : Seat.Next(DealerSeat);
    }
}
