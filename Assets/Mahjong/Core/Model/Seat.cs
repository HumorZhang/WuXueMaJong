namespace WuXueMaJong.Core
{
    /// <summary>座位工具。下家 = (seat + 1) % 4，按逆时针出牌。</summary>
    public static class Seat
    {
        public const int Count = 4;

        public static int Next(int seat) => (seat + 1) % Count;

        public static int Previous(int seat) => (seat + 3) % Count;

        /// <summary>从 from 的下家开始数，seat 的相对距离（1..3）。</summary>
        public static int DistanceFrom(int from, int seat) => (seat - from + Count) % Count;

        /// <summary>相对座位名：0=自己（不会用于其他家），1=下家，2=对家，3=上家。</summary>
        public static string RelativeName(int viewerSeat, int seat)
        {
            switch (DistanceFrom(viewerSeat, seat))
            {
                case 0: return "自己";
                case 1: return "下家";
                case 2: return "对家";
                default: return "上家";
            }
        }
    }
}
