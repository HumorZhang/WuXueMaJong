using System.Collections.Generic;
using System.Linq;

namespace WuXueMaJong.Core
{
    public enum BigHandType
    {
        PengPengHu = 0,   // 碰碰胡
        QingYiSe = 1,     // 清一色
        JiangYiSe = 2,    // 将一色
        FengYiSe = 3,     // 风一色
        QuanQiuRen = 4,   // 全求人
        GangShangHua = 5, // 杠上花
        HaiDiLao = 6,     // 海底捞
        QiangGang = 7     // 抢杠
    }

    /// <summary>分解出的一个面子。</summary>
    public sealed class DecomposedSet
    {
        public bool IsTriplet;
        public bool IsSequence;
        public bool IsFlexible;   // 全部由万能癞子组成，可指定为任意面子
        public int Kind;          // 刻子牌种，或顺子的最小牌种
        public int WildUsed;
        public int[] FixedKinds;  // 实际使用的自然牌牌种（不含万能癞子）

        public static DecomposedSet Triplet(int kind, int wildUsed, int[] fixedKinds)
        {
            return new DecomposedSet { IsTriplet = true, Kind = kind, WildUsed = wildUsed, FixedKinds = fixedKinds };
        }

        public static DecomposedSet Sequence(int startKind, int wildUsed, int[] fixedKinds)
        {
            return new DecomposedSet { IsSequence = true, Kind = startKind, WildUsed = wildUsed, FixedKinds = fixedKinds };
        }

        public static DecomposedSet Flexible()
        {
            return new DecomposedSet
            {
                IsTriplet = true,
                IsFlexible = true,
                Kind = -1,
                WildUsed = 3,
                FixedKinds = new int[0]
            };
        }
    }

    public sealed class HandDecomposition
    {
        public int PairKind;
        public bool PairFlexible;
        public int PairWildUsed;
        public List<DecomposedSet> Sets = new List<DecomposedSet>();
        public int WildUsed;

        public string Signature()
        {
            string pair = PairFlexible ? "*" : PairKind.ToString();
            var parts = Sets
                .Select(s => s.IsFlexible
                    ? "F"
                    : (s.IsSequence ? "S" + s.Kind : "T" + s.Kind) + "w" + s.WildUsed)
                .OrderBy(x => x)
                .ToList();
            return pair + "|" + WildUsed + "|" + string.Join(",", parts);
        }
    }

    /// <summary>胡牌判定结果。</summary>
    public sealed class WinResult
    {
        public bool CanWin;
        public bool IsHard;
        public int WildcardUsed;
        public int PairKind;
        public bool PairFlexible;
        public List<BigHandType> BigHands = new List<BigHandType>();
        public HandDecomposition Decomposition;

        public bool IsBig => CanWin && BigHands.Count > 0;

        public static WinResult Fail()
        {
            return new WinResult { CanWin = false };
        }

        public static string BigHandName(BigHandType type)
        {
            switch (type)
            {
                case BigHandType.PengPengHu: return "碰碰胡";
                case BigHandType.QingYiSe: return "清一色";
                case BigHandType.JiangYiSe: return "将一色";
                case BigHandType.FengYiSe: return "风一色";
                case BigHandType.QuanQiuRen: return "全求人";
                case BigHandType.GangShangHua: return "杠上花";
                case BigHandType.HaiDiLao: return "海底捞";
                case BigHandType.QiangGang: return "抢杠";
                default: return type.ToString();
            }
        }

        public string Describe()
        {
            if (!CanWin)
            {
                return "未成胡";
            }

            var parts = new List<string>();
            parts.Add(IsHard ? "硬胡" : "软胡");
            parts.Add(BigHands.Count > 0
                ? string.Join("、", BigHands.Select(BigHandName))
                : "小胡");
            if (PairKind >= 0)
            {
                parts.Add("将 " + (PairFlexible ? "癞子" : TileCatalog.DisplayName(PairKind)));
            }

            return string.Join(" · ", parts);
        }
    }
}
