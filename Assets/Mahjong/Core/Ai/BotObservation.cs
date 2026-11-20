using System.Linq;

namespace WuXueMaJong.Core
{
    /// <summary>AI 可见的信息：自己的手牌 + 公开信息。不暴露其他家手牌与牌墙顺序。</summary>
    public sealed class BotObservation
    {
        public int SeatIndex;
        public int JokerKind;
        public int WallRemaining;
        public int[] Hand;
        public Meld[] Melds;
        public int OpenCount;
        public bool HasOpened;
        public int[] OwnDiscards;
        public int[] OpponentHandCounts;

        public static BotObservation From(GameEngine engine, int seat)
        {
            PlayerState player = engine.Player(seat);
            var opponentCounts = new int[Seat.Count];
            for (int s = 0; s < Seat.Count; s++)
            {
                opponentCounts[s] = s == seat ? 0 : engine.Player(s).Hand.Count;
            }

            return new BotObservation
            {
                SeatIndex = seat,
                JokerKind = engine.JokerKind,
                WallRemaining = engine.Wall.Remaining,
                Hand = player.Hand.ToArray(),
                Melds = player.Melds.ToArray(),
                OpenCount = player.OpenCount,
                HasOpened = player.HasOpened,
                OwnDiscards = player.Discards.ToArray(),
                OpponentHandCounts = opponentCounts
            };
        }

        public int CountInHand(int kind) => Hand.Count(t => TileCatalog.KindOf(t) == kind);

        public int JokerCount => Hand.Count(t => TileCatalog.KindOf(t) == JokerKind);
    }
}
