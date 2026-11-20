using System;
using System.Collections.Generic;
using System.Linq;

namespace WuXueMaJong.Core
{
    /// <summary>
    /// 武汉麻将胡牌判定：普通牌分解 + 癞子万能牌补位。
    /// 输入的手牌必须已包含胡牌张。
    /// </summary>
    public static class WinChecker
    {
        /// <summary>手牌 + 别人打出的胡牌张。</summary>
        public static WinResult EvaluateWithWinningTile(
            IReadOnlyList<int> handWithoutWinningTile,
            int winningTileId,
            IReadOnlyList<Meld> melds,
            int jokerKind,
            WinContext context,
            RulesConfig rules)
        {
            if (handWithoutWinningTile == null)
            {
                return WinResult.Fail();
            }

            var tiles = new List<int>(handWithoutWinningTile.Count + 1);
            tiles.AddRange(handWithoutWinningTile);
            tiles.Add(winningTileId);
            return Evaluate(tiles, melds, jokerKind, context, rules);
        }

        public static WinResult Evaluate(
            IReadOnlyList<int> concealedTileIds,
            IReadOnlyList<Meld> melds,
            int jokerKind,
            WinContext context,
            RulesConfig rules)
        {
            if (concealedTileIds == null || melds == null || context == null || rules == null)
            {
                return WinResult.Fail();
            }

            if (!TileCatalog.IsValidKind(jokerKind))
            {
                return WinResult.Fail();
            }

            int meldCount = melds.Count;
            if (meldCount > 4)
            {
                return WinResult.Fail();
            }

            int setsNeeded = 4 - meldCount;
            if (concealedTileIds.Count != setsNeeded * 3 + 2)
            {
                return WinResult.Fail();
            }

            if (rules.RequireOpenToWin && !melds.Any(m => m.IsOpen))
            {
                return WinResult.Fail();
            }

            foreach (Meld meld in melds)
            {
                if (meld == null)
                {
                    return WinResult.Fail();
                }

                foreach (int tileId in meld.TileIds)
                {
                    if (!TileCatalog.IsValidTile(tileId))
                    {
                        return WinResult.Fail();
                    }

                    int kind = TileCatalog.KindOf(tileId);
                    if (kind == jokerKind || kind == TileCatalog.RedCenter)
                    {
                        return WinResult.Fail();
                    }
                }
            }

            var baseCounts = new int[TileCatalog.KindCount];
            int jokerCount = 0;
            foreach (int tileId in concealedTileIds)
            {
                if (!TileCatalog.IsValidTile(tileId))
                {
                    return WinResult.Fail();
                }

                int kind = TileCatalog.KindOf(tileId);
                if (kind == TileCatalog.RedCenter)
                {
                    return WinResult.Fail();
                }

                if (kind == jokerKind)
                {
                    jokerCount++;
                }
                else
                {
                    baseCounts[kind]++;
                }
            }

            WinResult best = null;
            long bestScore = long.MinValue;
            var seenDecompositions = new HashSet<string>();

            // 癞子既可以当自己，也可以当万能牌；枚举当万能牌的张数。
            for (int wildUsed = 0; wildUsed <= jokerCount; wildUsed++)
            {
                int naturalJokers = jokerCount - wildUsed;
                var counts = (int[])baseCounts.Clone();
                counts[jokerKind] = naturalJokers;

                foreach (PairOption pair in EnumeratePairOptions(counts, wildUsed))
                {
                    var setCounts = (int[])counts.Clone();
                    setCounts[pair.Kind] -= pair.NaturalUsed;

                    var output = new List<List<DecomposedSet>>();
                    CollectSetDecompositions(setCounts, wildUsed - pair.WildUsed, setsNeeded,
                        new List<DecomposedSet>(), output);

                    foreach (List<DecomposedSet> sets in output)
                    {
                        var decomposition = new HandDecomposition
                        {
                            PairKind = pair.Kind,
                            PairFlexible = pair.Flexible,
                            PairWildUsed = pair.WildUsed,
                            Sets = sets,
                            WildUsed = wildUsed
                        };

                        string signature = decomposition.Signature();
                        if (!seenDecompositions.Add(signature))
                        {
                            continue;
                        }

                        WinResult candidate = BuildResult(decomposition, melds, context, rules);
                        if (candidate == null)
                        {
                            continue;
                        }

                        long score = candidate.BigHands.Count * 100000L +
                                     (candidate.IsHard ? 1000L : 0L) -
                                     candidate.WildcardUsed;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = candidate;
                        }
                    }
                }
            }

            return best ?? WinResult.Fail();
        }

        private sealed class PairOption
        {
            public int Kind;
            public bool Flexible;
            public int NaturalUsed;
            public int WildUsed;
        }

        private static IEnumerable<PairOption> EnumeratePairOptions(int[] counts, int wild)
        {
            for (int kind = 0; kind < TileCatalog.KindCount; kind++)
            {
                if (kind == TileCatalog.RedCenter)
                {
                    continue;
                }

                if (counts[kind] >= 2)
                {
                    yield return new PairOption { Kind = kind, NaturalUsed = 2, WildUsed = 0 };
                }

                if (counts[kind] >= 1 && wild >= 1)
                {
                    yield return new PairOption { Kind = kind, NaturalUsed = 1, WildUsed = 1 };
                }
            }

            if (wild >= 2)
            {
                for (int kind = 0; kind < TileCatalog.KindCount; kind++)
                {
                    if (kind == TileCatalog.RedCenter)
                    {
                        continue;
                    }

                    yield return new PairOption { Kind = kind, Flexible = true, NaturalUsed = 0, WildUsed = 2 };
                }
            }
        }

        /// <summary>枚举把剩余牌拆成恰好 setsLeft 个面子的所有方式。</summary>
        private static void CollectSetDecompositions(
            int[] counts,
            int wild,
            int setsLeft,
            List<DecomposedSet> acc,
            List<List<DecomposedSet>> output)
        {
            if (setsLeft == 0)
            {
                if (wild == 0 && IsAllZero(counts))
                {
                    output.Add(new List<DecomposedSet>(acc));
                }

                return;
            }

            if (wild < 0)
            {
                return;
            }

            // 纯癞子组成的面子（可出现在任何位置，用于覆盖自然牌不足的情形）
            if (wild >= 3)
            {
                acc.Add(DecomposedSet.Flexible());
                CollectSetDecompositions(counts, wild - 3, setsLeft - 1, acc, output);
                acc.RemoveAt(acc.Count - 1);
            }

            int smallest = -1;
            for (int k = 0; k < counts.Length; k++)
            {
                if (counts[k] > 0)
                {
                    smallest = k;
                    break;
                }
            }

            if (smallest < 0)
            {
                return;
            }

            // 刻子
            {
                int natural = Math.Min(counts[smallest], 3);
                int deficit = 3 - natural;
                if (deficit <= wild)
                {
                    var fixedKinds = new int[natural];
                    for (int i = 0; i < natural; i++)
                    {
                        fixedKinds[i] = smallest;
                    }

                    counts[smallest] -= natural;
                    acc.Add(DecomposedSet.Triplet(smallest, deficit, fixedKinds));
                    CollectSetDecompositions(counts, wild - deficit, setsLeft - 1, acc, output);
                    acc.RemoveAt(acc.Count - 1);
                    counts[smallest] += natural;
                }
            }

            // 顺子：最小自然牌可能位于顺子的任意位置
            if (TileCatalog.IsSuited(smallest))
            {
                int rank = TileCatalog.RankOf(smallest);
                int suitBase = smallest - (rank - 1);
                for (int startRank = rank - 2; startRank <= rank; startRank++)
                {
                    if (startRank < 1 || startRank > 7)
                    {
                        continue;
                    }

                    int startKind = suitBase + (startRank - 1);
                    int[] need = { startKind, startKind + 1, startKind + 2 };
                    var used = new bool[3];
                    var fixedKinds = new List<int>(3);
                    int deficit = 0;

                    for (int i = 0; i < 3; i++)
                    {
                        if (counts[need[i]] > 0)
                        {
                            used[i] = true;
                            fixedKinds.Add(need[i]);
                            counts[need[i]]--;
                        }
                        else
                        {
                            deficit++;
                        }
                    }

                    if (deficit <= wild)
                    {
                        acc.Add(DecomposedSet.Sequence(startKind, deficit, fixedKinds.ToArray()));
                        CollectSetDecompositions(counts, wild - deficit, setsLeft - 1, acc, output);
                        acc.RemoveAt(acc.Count - 1);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        if (used[i])
                        {
                            counts[need[i]]++;
                        }
                    }
                }
            }
        }

        private static bool IsAllZero(int[] counts)
        {
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static WinResult BuildResult(
            HandDecomposition decomposition,
            IReadOnlyList<Meld> melds,
            WinContext context,
            RulesConfig rules)
        {
            var fixedKinds = new List<int>();
            bool hasChi = false;

            foreach (Meld meld in melds)
            {
                if (meld.Type == MeldType.Chi)
                {
                    hasChi = true;
                }

                foreach (int tileId in meld.TileIds)
                {
                    fixedKinds.Add(TileCatalog.KindOf(tileId));
                }
            }

            bool hasFixedSequence = false;
            foreach (DecomposedSet set in decomposition.Sets)
            {
                fixedKinds.AddRange(set.FixedKinds);
                if (set.IsSequence)
                {
                    hasFixedSequence = true;
                }
            }

            if (!decomposition.PairFlexible)
            {
                fixedKinds.Add(decomposition.PairKind);
            }

            var bigHands = new List<BigHandType>();

            bool pengPengHu = !hasChi && !hasFixedSequence;
            if (pengPengHu)
            {
                bigHands.Add(BigHandType.PengPengHu);
            }

            bool qingYiSe = false;
            if (fixedKinds.Count > 0)
            {
                int suit = TileCatalog.SuitOf(fixedKinds[0]);
                if (suit >= 0 && fixedKinds.All(k => TileCatalog.SuitOf(k) == suit))
                {
                    qingYiSe = true;
                }
            }

            if (qingYiSe)
            {
                bigHands.Add(BigHandType.QingYiSe);
            }

            bool jiangYiSe = fixedKinds.Count > 0 && fixedKinds.All(TileCatalog.IsPairRank);
            if (jiangYiSe)
            {
                bigHands.Add(BigHandType.JiangYiSe);
            }

            bool fengYiSe = fixedKinds.Count > 0 &&
                            fixedKinds.All(k => TileCatalog.IsWind(k) ||
                                                k == TileCatalog.GreenDragon ||
                                                k == TileCatalog.WhiteDragon);
            if (fengYiSe)
            {
                bigHands.Add(BigHandType.FengYiSe);
            }

            bool quanQiuRen = !context.IsSelfDraw &&
                              melds.Count == 4 &&
                              melds.All(m => m.IsOpen);
            if (quanQiuRen)
            {
                bigHands.Add(BigHandType.QuanQiuRen);
            }

            if (context.IsSelfDraw && context.IsAfterKongReplacement)
            {
                bigHands.Add(BigHandType.GangShangHua);
            }

            if (context.IsSelfDraw && context.IsLastWallDraw)
            {
                bigHands.Add(BigHandType.HaiDiLao);
            }

            if (context.IsRobKong)
            {
                bigHands.Add(BigHandType.QiangGang);
            }

            bool pairExempt = pengPengHu || qingYiSe || jiangYiSe || fengYiSe;
            if (rules.Require258Pair && !pairExempt && !TileCatalog.IsPairRank(decomposition.PairKind))
            {
                return null;
            }

            // 两个及以上癞子当万能牌时，只能胡大胡
            if (decomposition.WildUsed >= 2 && bigHands.Count == 0)
            {
                return null;
            }

            return new WinResult
            {
                CanWin = true,
                IsHard = decomposition.WildUsed == 0,
                WildcardUsed = decomposition.WildUsed,
                PairKind = decomposition.PairKind,
                PairFlexible = decomposition.PairFlexible,
                BigHands = bigHands,
                Decomposition = decomposition
            };
        }
    }
}
