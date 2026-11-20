using System.Collections.Generic;
using System.Linq;

namespace WuXueMaJong.Core
{
    /// <summary>轻量手牌评分，用于 AI 出牌与吃碰决策（不做完整胡牌搜索）。</summary>
    public static class HandEvaluator
    {
        /// <summary>保留这张牌的价值，越低越应该打出。</summary>
        public static int KeepScore(IReadOnlyList<int> hand, int jokerKind, int kind)
        {
            if (kind == jokerKind)
            {
                return 100000;
            }

            if (kind == TileCatalog.RedCenter)
            {
                return 90000;
            }

            int count = hand.Count(t => TileCatalog.KindOf(t) == kind);
            int score = 0;

            if (count >= 3)
            {
                score += 1000;
            }
            else if (count == 2)
            {
                score += 300;
            }

            if (TileCatalog.IsSuited(kind))
            {
                int rank = TileCatalog.RankOf(kind);
                if (HasKind(hand, kind - 1))
                {
                    score += 90;
                }

                if (HasKind(hand, kind + 1))
                {
                    score += 90;
                }

                if (rank - 2 >= 1 && HasKind(hand, kind - 2))
                {
                    score += 45;
                }

                if (rank + 2 <= 9 && HasKind(hand, kind + 2))
                {
                    score += 45;
                }

                if (rank >= 3 && rank <= 7)
                {
                    score += 15;
                }

                if (rank == 1 || rank == 9)
                {
                    score -= 15;
                }

                if (TileCatalog.IsPairRank(kind))
                {
                    score += 40;
                }
            }
            else if (count == 1)
            {
                score -= 120;
            }

            return score;
        }

        public static int CountPairRankPairs(IReadOnlyList<int> hand)
        {
            return hand
                .GroupBy(TileCatalog.KindOf)
                .Count(g => TileCatalog.IsPairRank(g.Key) && g.Count() >= 2);
        }

        private static bool HasKind(IReadOnlyList<int> hand, int kind)
        {
            return hand.Any(t => TileCatalog.KindOf(t) == kind);
        }
    }
}
