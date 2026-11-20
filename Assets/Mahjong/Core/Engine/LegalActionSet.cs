using System.Collections.Generic;
using System.Linq;

namespace WuXueMaJong.Core
{
    public sealed class ChiOption
    {
        public int StartKind;
        public int TileIdA;
        public int TileIdB;

        public int[] SequenceKinds()
        {
            return new[] { StartKind, StartKind + 1, StartKind + 2 };
        }

        public string DisplayName()
        {
            return "吃 " + string.Join("", SequenceKinds().Select(TileCatalog.DisplayName));
        }
    }

    public sealed class ClaimOption
    {
        public int Seat;
        public int DiscarderSeat;
        public int TileKind;
        public bool CanHu;
        public WinResult HuPreview;
        public bool CanPong;
        public bool CanExposedKong;
        public List<ChiOption> ChiOptions = new List<ChiOption>();

        public bool HasAny => CanHu || CanPong || CanExposedKong || ChiOptions.Count > 0;
    }

    public sealed class RobKongOption
    {
        public int Seat;
        public bool CanHu;
        public WinResult HuPreview;
    }

    public enum ClaimAction
    {
        Pass = 0,
        Win = 1,
        Pong = 2,
        ExposedKong = 3,
        Chi = 4
    }

    public sealed class ClaimDecision
    {
        public ClaimAction Action;
        public ChiOption Chi;

        public static ClaimDecision Pass()
        {
            return new ClaimDecision { Action = ClaimAction.Pass };
        }

        public static ClaimDecision Win()
        {
            return new ClaimDecision { Action = ClaimAction.Win };
        }

        public static ClaimDecision Pong()
        {
            return new ClaimDecision { Action = ClaimAction.Pong };
        }

        public static ClaimDecision Kong()
        {
            return new ClaimDecision { Action = ClaimAction.ExposedKong };
        }

        public static ClaimDecision ChiWith(ChiOption option)
        {
            return new ClaimDecision { Action = ClaimAction.Chi, Chi = option };
        }
    }

    /// <summary>当前玩家可执行的全部合法动作。</summary>
    public sealed class LegalActionSet
    {
        public bool CanWin;
        public WinResult WinPreview;

        /// <summary>可以普通打出的手牌（不含红中、癞子）。</summary>
        public List<int> DiscardableTileIds = new List<int>();

        public List<int> ConcealedKongKinds = new List<int>();
        public List<int> AddedKongKinds = new List<int>();
        public List<int> RedCenterGongTileIds = new List<int>();
        public List<int> LaiziGongTileIds = new List<int>();

        public bool HasNormalDiscard => DiscardableTileIds.Count > 0;

        public bool HasAnyAction =>
            CanWin ||
            DiscardableTileIds.Count > 0 ||
            ConcealedKongKinds.Count > 0 ||
            AddedKongKinds.Count > 0 ||
            RedCenterGongTileIds.Count > 0 ||
            LaiziGongTileIds.Count > 0;
    }
}
