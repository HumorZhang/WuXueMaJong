namespace WuXueMaJong.Core
{
    /// <summary>武汉麻将规则参数。所有版本差异集中在这里。</summary>
    public sealed class RulesConfig
    {
        // 胡牌条件
        public int BaseSmallWin = 1;
        public int BaseBigWin = 10;
        public int MinOpenGroups = 1;
        public int MaxWildForSmallWin = 1;
        public bool Require258Pair = true;
        public bool AllowSevenPairs = false;
        public bool RequireOpenToWin = true;

        // 计分倍率（口口翻）
        public int OpenMultiplier = 2;
        public int ExposedKongMultiplier = 2;
        public int AddedKongMultiplier = 2;
        public int ConcealedKongMultiplier = 4;
        public int SpecialGongMultiplier = 2;
        public int HardWinMultiplier = 2;
        public int SelfDrawMultiplier = 2;
        public int DiscarderMultiplier = 2;
        public int DealerMultiplier = 2;
        public long ScoreCap = long.MaxValue;

        // 响应限制
        public bool AllowRonOnLastDiscard = true;
        public bool NonHuClaimsOnEmptyWall = false;
        public int ClaimTimeoutSeconds = 8;

        public static RulesConfig WuhanDefault() => new RulesConfig();
    }
}
