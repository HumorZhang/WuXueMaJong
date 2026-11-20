using System.Collections.Generic;
using System.Linq;

namespace WuXueMaJong.Core
{
    /// <summary>一个玩家在一局中的全部状态。</summary>
    public sealed class PlayerState
    {
        public int Seat { get; }
        public List<int> Hand { get; } = new List<int>();
        public List<Meld> Melds { get; } = new List<Meld>();
        public List<SpecialGong> SpecialGongs { get; } = new List<SpecialGong>();
        public List<int> Discards { get; } = new List<int>();

        public PlayerState(int seat)
        {
            Seat = seat;
        }

        /// <summary>开口次数：吃、碰、直杠、蓄杠各算一口，暗杠不算。</summary>
        public int OpenCount => Melds.Count(m => m.IsOpen);

        public int ExposedKongCount => Melds.Count(m => m.Type == MeldType.ExposedKong);
        public int AddedKongCount => Melds.Count(m => m.Type == MeldType.AddedKong);
        public int ConcealedKongCount => Melds.Count(m => m.Type == MeldType.ConcealedKong);
        public int RedCenterGongCount => SpecialGongs.Count(g => g.Type == SpecialGongType.RedCenter);
        public int LaiziGongCount => SpecialGongs.Count(g => g.Type == SpecialGongType.Laizi);

        public bool HasOpened => OpenCount > 0;

        public bool HasRedCenterInHand => Hand.Any(t => TileCatalog.KindOf(t) == TileCatalog.RedCenter);

        public int CountInHand(int kind) => Hand.Count(t => TileCatalog.KindOf(t) == kind);

        public int[] HandKinds() => Hand.Select(TileCatalog.KindOf).ToArray();

        public int KongTotalCount => ExposedKongCount + AddedKongCount + ConcealedKongCount +
                                     RedCenterGongCount + LaiziGongCount;
    }
}
