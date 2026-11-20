using System;
using System.Collections.Generic;
using System.Linq;

namespace WuXueMaJong.Core
{
    /// <summary>
    /// 武汉麻将单局状态机。引擎只接受合法命令，不替玩家做决策。
    /// </summary>
    public sealed class GameEngine
    {
        private readonly RulesConfig _rules;
        private readonly Wall _wall;
        private readonly PlayerState[] _players = new PlayerState[Seat.Count];
        private readonly List<GameEvent> _events = new List<GameEvent>();

        private readonly Dictionary<int, ClaimOption> _claimOptions = new Dictionary<int, ClaimOption>();
        private readonly Dictionary<int, ClaimDecision> _claimDecisions = new Dictionary<int, ClaimDecision>();
        private readonly Dictionary<int, RobKongOption> _robKongOptions = new Dictionary<int, RobKongOption>();
        private readonly Dictionary<int, bool> _robKongDecisions = new Dictionary<int, bool>();

        private int _robKongSeat = -1;
        private int _robKongTileId = -1;
        private int _robKongPongMeldIndex = -1;
        private Meld _robKongOriginalPong;

        private bool _canSelfDraw;
        private bool _afterKongReplacement;
        private bool _lastDrawWasLastWallTile;
        private int _lastDrawnTileId = -1;
        private bool _discardWasLastTile;

        public GamePhase Phase { get; private set; }
        public int CurrentSeat { get; private set; }
        public int DealerSeat { get; }
        public int LastDiscardSeat { get; private set; } = -1;
        public int LastDiscardTileId { get; private set; } = -1;
        public HandResult Result { get; private set; }

        public RulesConfig Rules => _rules;
        public Wall Wall => _wall;
        public IReadOnlyList<PlayerState> Players => _players;
        public IReadOnlyList<GameEvent> Events => _events;
        public int JokerKind => _wall.JokerKind;
        public int IndicatorTileId => _wall.IndicatorTileId;
        public bool AfterKongReplacement => _afterKongReplacement;
        public int LastDrawnTileId => _lastDrawnTileId;

        public IReadOnlyDictionary<int, ClaimOption> ClaimOptions => _claimOptions;
        public IReadOnlyDictionary<int, RobKongOption> RobKongOptions => _robKongOptions;
        public IEnumerable<int> PendingClaimSeats => _claimOptions.Keys.Where(seat => !_claimDecisions.ContainsKey(seat));
        public IEnumerable<int> PendingRobKongSeats => _robKongOptions.Keys.Where(seat => !_robKongDecisions.ContainsKey(seat));

        public GameEngine(HandSetup setup, RulesConfig rules = null)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            _rules = rules ?? RulesConfig.WuhanDefault();
            _wall = new Wall(setup);

            for (int seat = 0; seat < Seat.Count; seat++)
            {
                _players[seat] = new PlayerState(seat);
                _players[seat].Hand.AddRange(setup.Hands[seat]);
            }

            DealerSeat = setup.DealerSeat;
            CurrentSeat = DealerSeat;
            Phase = GamePhase.AwaitingDiscard;
            _canSelfDraw = false;

            AddEvent("start", DealerSeat,
                $"开局：座位{DealerSeat}坐庄，指示牌 {TileCatalog.DisplayName(TileCatalog.KindOf(IndicatorTileId))}，" +
                $"癞子 {TileCatalog.DisplayName(JokerKind)}。");

            EnsureProgress();
        }

        public PlayerState Player(int seat) => _players[seat];

        public ClaimOption GetClaimOption(int seat)
        {
            return _claimOptions.TryGetValue(seat, out ClaimOption option) ? option : null;
        }

        public RobKongOption GetRobKongOption(int seat)
        {
            return _robKongOptions.TryGetValue(seat, out RobKongOption option) ? option : null;
        }

        // ---------------------------------------------------------------- 合法动作

        public LegalActionSet GetLegalActions(int seat)
        {
            var actions = new LegalActionSet();
            if (Phase != GamePhase.AwaitingDiscard || seat != CurrentSeat)
            {
                return actions;
            }

            PlayerState player = _players[seat];

            if (_canSelfDraw)
            {
                var context = new WinContext
                {
                    IsSelfDraw = true,
                    IsAfterKongReplacement = _afterKongReplacement,
                    IsLastWallDraw = _lastDrawWasLastWallTile,
                    IsRobKong = false,
                    WinningTileId = _lastDrawnTileId
                };

                WinResult win = WinChecker.Evaluate(player.Hand, player.Melds, JokerKind, context, _rules);
                if (win.CanWin)
                {
                    actions.CanWin = true;
                    actions.WinPreview = win;
                }
            }

            foreach (int tileId in player.Hand)
            {
                int kind = TileCatalog.KindOf(tileId);
                if (kind != TileCatalog.RedCenter && kind != JokerKind)
                {
                    actions.DiscardableTileIds.Add(tileId);
                }
            }

            for (int kind = 0; kind < TileCatalog.KindCount; kind++)
            {
                if (kind == TileCatalog.RedCenter || kind == JokerKind)
                {
                    continue;
                }

                int count = player.CountInHand(kind);
                if (count >= 4 && !_wall.IsEmpty)
                {
                    actions.ConcealedKongKinds.Add(kind);
                }

                if (count >= 1 && !_wall.IsEmpty &&
                    player.Melds.Any(m => m.Type == MeldType.Pong && m.ContainsKind(kind)))
                {
                    actions.AddedKongKinds.Add(kind);
                }
            }

            if (!_wall.IsEmpty)
            {
                foreach (int tileId in player.Hand)
                {
                    int kind = TileCatalog.KindOf(tileId);
                    if (kind == TileCatalog.RedCenter)
                    {
                        actions.RedCenterGongTileIds.Add(tileId);
                    }
                    else if (kind == JokerKind)
                    {
                        actions.LaiziGongTileIds.Add(tileId);
                    }
                }
            }

            return actions;
        }

        // ---------------------------------------------------------------- 出牌与响应

        public CommandResult Discard(int seat, int tileId)
        {
            if (Phase != GamePhase.AwaitingDiscard)
            {
                return CommandResult.Fail("当前不是出牌阶段。");
            }

            if (seat != CurrentSeat)
            {
                return CommandResult.Fail("还没轮到你出牌。");
            }

            PlayerState player = _players[seat];
            if (!player.Hand.Contains(tileId))
            {
                return CommandResult.Fail("手牌中没有这张牌。");
            }

            int kind = TileCatalog.KindOf(tileId);
            if (kind == TileCatalog.RedCenter)
            {
                return CommandResult.Fail("红中不能普通打出，请使用红中杠。");
            }

            if (kind == JokerKind)
            {
                return CommandResult.Fail("癞子不能普通打出，请使用癞子杠。");
            }

            if (player.Hand.Count != 14 - 3 * player.Melds.Count)
            {
                return CommandResult.Fail("手牌数量异常。");
            }

            player.Hand.Remove(tileId);
            player.Discards.Add(tileId);
            LastDiscardSeat = seat;
            LastDiscardTileId = tileId;
            _discardWasLastTile = _wall.IsEmpty;
            _lastDrawnTileId = -1;
            _afterKongReplacement = false;
            _lastDrawWasLastWallTile = false;
            _canSelfDraw = false;

            AddEvent("discard", seat, $"座位{seat} 打出 {TileCatalog.DisplayName(kind)}。");

            BuildClaimWindow(seat, tileId);
            return CommandResult.Ok();
        }

        public CommandResult SubmitClaim(int seat, ClaimDecision decision)
        {
            if (Phase != GamePhase.ClaimWindow)
            {
                return CommandResult.Fail("当前没有响应窗口。");
            }

            if (!_claimOptions.TryGetValue(seat, out ClaimOption option))
            {
                return CommandResult.Fail("你没有可响应的操作。");
            }

            if (_claimDecisions.ContainsKey(seat))
            {
                return CommandResult.Fail("已经响应过了。");
            }

            decision = decision ?? ClaimDecision.Pass();
            switch (decision.Action)
            {
                case ClaimAction.Pass:
                    break;
                case ClaimAction.Win:
                    if (!option.CanHu)
                    {
                        return CommandResult.Fail("不能胡这张牌。");
                    }

                    break;
                case ClaimAction.Pong:
                    if (!option.CanPong)
                    {
                        return CommandResult.Fail("不能碰。");
                    }

                    break;
                case ClaimAction.ExposedKong:
                    if (!option.CanExposedKong)
                    {
                        return CommandResult.Fail("不能杠。");
                    }

                    break;
                case ClaimAction.Chi:
                    if (decision.Chi == null)
                    {
                        return CommandResult.Fail("缺少吃牌选择。");
                    }

                    ChiOption chosen = option.ChiOptions.FirstOrDefault(c => c.StartKind == decision.Chi.StartKind);
                    if (chosen == null)
                    {
                        return CommandResult.Fail("无效的吃牌选择。");
                    }

                    decision = ClaimDecision.ChiWith(chosen);
                    break;
                default:
                    return CommandResult.Fail("未知操作。");
            }

            _claimDecisions[seat] = decision;

            if (_claimOptions.Keys.All(s => _claimDecisions.ContainsKey(s)))
            {
                ResolveClaims();
            }

            return CommandResult.Ok();
        }

        private void BuildClaimWindow(int discarderSeat, int tileId)
        {
            _claimOptions.Clear();
            _claimDecisions.Clear();

            int kind = TileCatalog.KindOf(tileId);
            bool specialTile = kind == JokerKind || kind == TileCatalog.RedCenter;

            if (!specialTile)
            {
                for (int seat = 0; seat < Seat.Count; seat++)
                {
                    if (seat == discarderSeat)
                    {
                        continue;
                    }

                    ClaimOption option = BuildClaimOption(seat, discarderSeat, tileId, kind);
                    if (option.HasAny)
                    {
                        _claimOptions[seat] = option;
                    }
                }
            }

            if (_claimOptions.Count == 0)
            {
                AdvanceToNextSeat();
                return;
            }

            Phase = GamePhase.ClaimWindow;
            AddEvent("claim", discarderSeat,
                "等待响应：" + string.Join("、", _claimOptions.Keys.Select(s => "座位" + s)));
        }

        private ClaimOption BuildClaimOption(int seat, int discarderSeat, int tileId, int kind)
        {
            var option = new ClaimOption
            {
                Seat = seat,
                DiscarderSeat = discarderSeat,
                TileKind = kind
            };
            PlayerState player = _players[seat];

            if (_rules.AllowRonOnLastDiscard || !_discardWasLastTile)
            {
                var context = new WinContext
                {
                    IsSelfDraw = false,
                    DiscarderSeat = discarderSeat,
                    WinningTileId = tileId,
                    IsRobKong = false
                };

                WinResult win = WinChecker.EvaluateWithWinningTile(
                    player.Hand, tileId, player.Melds, JokerKind, context, _rules);

                if (win.CanWin)
                {
                    option.CanHu = true;
                    option.HuPreview = win;
                }
            }

            if (!_discardWasLastTile)
            {
                int count = player.CountInHand(kind);
                if (count >= 2)
                {
                    option.CanPong = true;
                }

                if (count >= 3 && !_wall.IsEmpty)
                {
                    option.CanExposedKong = true;
                }

                if (Seat.Next(discarderSeat) == seat)
                {
                    option.ChiOptions.AddRange(BuildChiOptions(player, kind));
                }
            }

            return option;
        }

        private List<ChiOption> BuildChiOptions(PlayerState player, int discardKind)
        {
            var options = new List<ChiOption>();
            if (!TileCatalog.IsSuited(discardKind))
            {
                return options;
            }

            int rank = TileCatalog.RankOf(discardKind);
            int suitBase = discardKind - (rank - 1);

            for (int startRank = rank - 2; startRank <= rank; startRank++)
            {
                if (startRank < 1 || startRank > 7)
                {
                    continue;
                }

                int startKind = suitBase + (startRank - 1);
                int[] sequence = { startKind, startKind + 1, startKind + 2 };
                int[] needed = sequence.Where(k => k != discardKind).ToArray();

                if (needed.Any(k => k == JokerKind))
                {
                    continue; // 癞子牌不能参与吃
                }

                int tileA = FindTileOfKind(player.Hand, needed[0]);
                int tileB = FindTileOfKind(player.Hand, needed[1]);
                if (tileA >= 0 && tileB >= 0)
                {
                    options.Add(new ChiOption
                    {
                        StartKind = startKind,
                        TileIdA = tileA,
                        TileIdB = tileB
                    });
                }
            }

            return options;
        }

        private static int FindTileOfKind(List<int> hand, int kind)
        {
            foreach (int tileId in hand)
            {
                if (TileCatalog.KindOf(tileId) == kind)
                {
                    return tileId;
                }
            }

            return -1;
        }

        private void ResolveClaims()
        {
            var options = new Dictionary<int, ClaimOption>(_claimOptions);
            var decisions = new Dictionary<int, ClaimDecision>(_claimDecisions);
            _claimOptions.Clear();
            _claimDecisions.Clear();

            int discarder = LastDiscardSeat;
            int winningSeat = FindNearest(options, decisions, ClaimAction.Win, discarder);
            if (winningSeat >= 0)
            {
                HandleClaimWin(winningSeat);
                return;
            }

            int kongSeat = FindNearest(options, decisions, ClaimAction.ExposedKong, discarder);
            if (kongSeat >= 0)
            {
                HandleExposedKong(kongSeat);
                return;
            }

            int pongSeat = FindNearest(options, decisions, ClaimAction.Pong, discarder);
            if (pongSeat >= 0)
            {
                HandlePong(pongSeat);
                return;
            }

            int chiSeat = -1;
            foreach (KeyValuePair<int, ClaimDecision> pair in decisions)
            {
                if (pair.Value.Action == ClaimAction.Chi)
                {
                    chiSeat = pair.Key;
                    break;
                }
            }

            if (chiSeat >= 0)
            {
                HandleChi(chiSeat, decisions[chiSeat].Chi);
                return;
            }

            AddEvent("pass", -1, "无人响应。");
            AdvanceToNextSeat();
        }

        private static int FindNearest(
            Dictionary<int, ClaimOption> options,
            Dictionary<int, ClaimDecision> decisions,
            ClaimAction action,
            int discarderSeat)
        {
            int bestSeat = -1;
            int bestDistance = int.MaxValue;

            foreach (KeyValuePair<int, ClaimDecision> pair in decisions)
            {
                if (pair.Value.Action != action)
                {
                    continue;
                }

                ClaimOption option = options[pair.Key];
                bool allowed;
                switch (action)
                {
                    case ClaimAction.Win: allowed = option.CanHu; break;
                    case ClaimAction.Pong: allowed = option.CanPong; break;
                    case ClaimAction.ExposedKong: allowed = option.CanExposedKong; break;
                    default: allowed = false; break;
                }

                if (!allowed)
                {
                    continue;
                }

                int distance = Seat.DistanceFrom(discarderSeat, pair.Key);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSeat = pair.Key;
                }
            }

            return bestSeat;
        }

        private void HandleClaimWin(int winnerSeat)
        {
            PlayerState winner = _players[winnerSeat];
            winner.Hand.Add(LastDiscardTileId);
            RemoveLastDiscardFromRiver();

            var context = new WinContext
            {
                IsSelfDraw = false,
                DiscarderSeat = LastDiscardSeat,
                WinningTileId = LastDiscardTileId,
                IsRobKong = false
            };

            WinResult win = WinChecker.Evaluate(winner.Hand, winner.Melds, JokerKind, context, _rules);
            if (!win.CanWin)
            {
                FinishDraw("胡牌校验失败");
                return;
            }

            FinishWin(winnerSeat, context, win);
        }

        private void HandleExposedKong(int seat)
        {
            PlayerState player = _players[seat];
            int kind = TileCatalog.KindOf(LastDiscardTileId);
            List<int> used = player.Hand.Where(t => TileCatalog.KindOf(t) == kind).Take(3).ToList();
            foreach (int tileId in used)
            {
                player.Hand.Remove(tileId);
            }

            used.Add(LastDiscardTileId);
            player.Melds.Add(new Meld(MeldType.ExposedKong, used.ToArray(), LastDiscardSeat, LastDiscardTileId));
            RemoveLastDiscardFromRiver();

            AddEvent("kong", seat, $"座位{seat} 明杠 {TileCatalog.DisplayName(kind)}。");
            DrawReplacement(seat);
        }

        private void HandlePong(int seat)
        {
            PlayerState player = _players[seat];
            int kind = TileCatalog.KindOf(LastDiscardTileId);
            List<int> used = player.Hand.Where(t => TileCatalog.KindOf(t) == kind).Take(2).ToList();
            foreach (int tileId in used)
            {
                player.Hand.Remove(tileId);
            }

            used.Add(LastDiscardTileId);
            player.Melds.Add(new Meld(MeldType.Pong, used.ToArray(), LastDiscardSeat, LastDiscardTileId));
            RemoveLastDiscardFromRiver();

            CurrentSeat = seat;
            Phase = GamePhase.AwaitingDiscard;
            _canSelfDraw = false;
            _afterKongReplacement = false;
            _lastDrawnTileId = -1;
            _lastDrawWasLastWallTile = false;

            AddEvent("pong", seat, $"座位{seat} 碰 {TileCatalog.DisplayName(kind)}。");
            EnsureProgress();
        }

        private void HandleChi(int seat, ChiOption option)
        {
            PlayerState player = _players[seat];
            player.Hand.Remove(option.TileIdA);
            player.Hand.Remove(option.TileIdB);

            int[] tiles = { option.TileIdA, option.TileIdB, LastDiscardTileId };
            player.Melds.Add(new Meld(MeldType.Chi, tiles, LastDiscardSeat, LastDiscardTileId));
            RemoveLastDiscardFromRiver();

            CurrentSeat = seat;
            Phase = GamePhase.AwaitingDiscard;
            _canSelfDraw = false;
            _afterKongReplacement = false;
            _lastDrawnTileId = -1;
            _lastDrawWasLastWallTile = false;

            AddEvent("chi", seat, $"座位{seat} {option.DisplayName()}。");
            EnsureProgress();
        }

        private void RemoveLastDiscardFromRiver()
        {
            PlayerState discarder = _players[LastDiscardSeat];
            if (discarder.Discards.Count > 0)
            {
                discarder.Discards.RemoveAt(discarder.Discards.Count - 1);
            }
        }

        // ---------------------------------------------------------------- 自己回合的杠/胡

        public CommandResult DeclareWin(int seat)
        {
            if (Phase != GamePhase.AwaitingDiscard || seat != CurrentSeat)
            {
                return CommandResult.Fail("现在不能胡牌。");
            }

            if (!_canSelfDraw)
            {
                return CommandResult.Fail("只有摸牌后才能自摸。");
            }

            PlayerState player = _players[seat];
            var context = new WinContext
            {
                IsSelfDraw = true,
                IsAfterKongReplacement = _afterKongReplacement,
                IsLastWallDraw = _lastDrawWasLastWallTile,
                IsRobKong = false,
                WinningTileId = _lastDrawnTileId
            };

            WinResult win = WinChecker.Evaluate(player.Hand, player.Melds, JokerKind, context, _rules);
            if (!win.CanWin)
            {
                return CommandResult.Fail("手牌不能胡。");
            }

            FinishWin(seat, context, win);
            return CommandResult.Ok();
        }

        public CommandResult DeclareConcealedKong(int seat, int kind)
        {
            CommandResult validation = ValidateOwnKong(seat, kind);
            if (!validation.Success)
            {
                return validation;
            }

            PlayerState player = _players[seat];
            List<int> tiles = player.Hand.Where(t => TileCatalog.KindOf(t) == kind).Take(4).ToList();
            if (tiles.Count < 4)
            {
                return CommandResult.Fail("没有四张相同的牌。");
            }

            foreach (int tileId in tiles)
            {
                player.Hand.Remove(tileId);
            }

            player.Melds.Add(new Meld(MeldType.ConcealedKong, tiles.ToArray()));
            AddEvent("kong", seat, $"座位{seat} 暗杠 {TileCatalog.DisplayName(kind)}。");
            DrawReplacement(seat);
            return CommandResult.Ok();
        }

        public CommandResult DeclareAddedKong(int seat, int kind)
        {
            CommandResult validation = ValidateOwnKong(seat, kind);
            if (!validation.Success)
            {
                return validation;
            }

            PlayerState player = _players[seat];
            int meldIndex = player.Melds.FindIndex(m => m.Type == MeldType.Pong && m.ContainsKind(kind));
            if (meldIndex < 0)
            {
                return CommandResult.Fail("没有可补杠的碰。");
            }

            int tileId = FindTileOfKind(player.Hand, kind);
            if (tileId < 0)
            {
                return CommandResult.Fail("手中没有第四张牌。");
            }

            player.Hand.Remove(tileId);
            Meld pong = player.Melds[meldIndex];
            player.Melds[meldIndex] = pong.UpgradeToAddedKong(tileId);

            _robKongSeat = seat;
            _robKongTileId = tileId;
            _robKongPongMeldIndex = meldIndex;
            _robKongOriginalPong = pong;

            AddEvent("kong", seat, $"座位{seat} 补杠 {TileCatalog.DisplayName(kind)}。");
            BuildRobKongWindow(seat, tileId);
            return CommandResult.Ok();
        }

        private CommandResult ValidateOwnKong(int seat, int kind)
        {
            if (Phase != GamePhase.AwaitingDiscard || seat != CurrentSeat)
            {
                return CommandResult.Fail("现在不能杠。");
            }

            if (_wall.IsEmpty)
            {
                return CommandResult.Fail("牌墙已空，不能杠。");
            }

            if (kind == TileCatalog.RedCenter || kind == JokerKind)
            {
                return CommandResult.Fail("红中、癞子不能按普通杠处理。");
            }

            return CommandResult.Ok();
        }

        public CommandResult DeclareRedCenterGong(int seat, int tileId)
        {
            return DeclareSpecialGong(seat, tileId, true);
        }

        public CommandResult DeclareLaiziGong(int seat, int tileId)
        {
            return DeclareSpecialGong(seat, tileId, false);
        }

        private CommandResult DeclareSpecialGong(int seat, int tileId, bool redCenter)
        {
            if (Phase != GamePhase.AwaitingDiscard || seat != CurrentSeat)
            {
                return CommandResult.Fail("现在不能杠。");
            }

            if (_wall.IsEmpty)
            {
                return CommandResult.Fail("牌墙已空，不能杠。");
            }

            PlayerState player = _players[seat];
            if (!player.Hand.Contains(tileId))
            {
                return CommandResult.Fail("手牌中没有这张牌。");
            }

            int kind = TileCatalog.KindOf(tileId);
            if (redCenter)
            {
                if (kind != TileCatalog.RedCenter)
                {
                    return CommandResult.Fail("这不是红中。");
                }
            }
            else
            {
                if (kind != JokerKind)
                {
                    return CommandResult.Fail("这不是癞子。");
                }
            }

            player.Hand.Remove(tileId);
            player.SpecialGongs.Add(new SpecialGong(
                redCenter ? SpecialGongType.RedCenter : SpecialGongType.Laizi, tileId));

            AddEvent("kong", seat,
                $"座位{seat} {(redCenter ? "红中杠" : "癞子杠")} {TileCatalog.DisplayName(kind)}。");
            DrawReplacement(seat);
            return CommandResult.Ok();
        }

        // ---------------------------------------------------------------- 抢杠

        private void BuildRobKongWindow(int kongerSeat, int tileId)
        {
            _robKongOptions.Clear();
            _robKongDecisions.Clear();

            for (int seat = 0; seat < Seat.Count; seat++)
            {
                if (seat == kongerSeat)
                {
                    continue;
                }

                PlayerState player = _players[seat];
                var context = new WinContext
                {
                    IsSelfDraw = false,
                    IsRobKong = true,
                    DiscarderSeat = kongerSeat,
                    WinningTileId = tileId
                };

                WinResult win = WinChecker.EvaluateWithWinningTile(
                    player.Hand, tileId, player.Melds, JokerKind, context, _rules);

                if (win.CanWin)
                {
                    _robKongOptions[seat] = new RobKongOption { Seat = seat, CanHu = true, HuPreview = win };
                }
            }

            if (_robKongOptions.Count == 0)
            {
                CompleteAddedKong(kongerSeat);
                return;
            }

            Phase = GamePhase.RobKongWindow;
            AddEvent("robkong", kongerSeat,
                "抢杠机会：" + string.Join("、", _robKongOptions.Keys.Select(s => "座位" + s)));
        }

        public CommandResult SubmitRobKongDecision(int seat, bool declareWin)
        {
            if (Phase != GamePhase.RobKongWindow)
            {
                return CommandResult.Fail("当前没有抢杠窗口。");
            }

            if (!_robKongOptions.ContainsKey(seat))
            {
                return CommandResult.Fail("你没有抢杠资格。");
            }

            if (_robKongDecisions.ContainsKey(seat))
            {
                return CommandResult.Fail("已经响应过了。");
            }

            _robKongDecisions[seat] = declareWin;

            if (_robKongOptions.Keys.All(s => _robKongDecisions.ContainsKey(s)))
            {
                ResolveRobKong();
            }

            return CommandResult.Ok();
        }

        private void ResolveRobKong()
        {
            int kongerSeat = _robKongSeat;
            int tileId = _robKongTileId;
            int winnerSeat = -1;
            int bestDistance = int.MaxValue;

            foreach (KeyValuePair<int, bool> pair in _robKongDecisions)
            {
                if (!pair.Value)
                {
                    continue;
                }

                int distance = Seat.DistanceFrom(kongerSeat, pair.Key);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    winnerSeat = pair.Key;
                }
            }

            _robKongOptions.Clear();
            _robKongDecisions.Clear();

            if (winnerSeat < 0)
            {
                CompleteAddedKong(kongerSeat);
                return;
            }

            PlayerState winner = _players[winnerSeat];
            winner.Hand.Add(tileId);

            // 被抢杠：补杠不成立，恢复为碰
            _players[kongerSeat].Melds[_robKongPongMeldIndex] = _robKongOriginalPong;

            var context = new WinContext
            {
                IsSelfDraw = false,
                IsRobKong = true,
                DiscarderSeat = kongerSeat,
                WinningTileId = tileId
            };

            WinResult win = WinChecker.Evaluate(winner.Hand, winner.Melds, JokerKind, context, _rules);
            ClearRobKongState();

            if (!win.CanWin)
            {
                FinishDraw("抢杠校验失败");
                return;
            }

            AddEvent("robkong", winnerSeat, $"座位{winnerSeat} 抢杠胡！");
            FinishWin(winnerSeat, context, win);
        }

        private void CompleteAddedKong(int kongerSeat)
        {
            ClearRobKongState();
            AddEvent("kong", kongerSeat, $"座位{kongerSeat} 补杠成立。");
            DrawReplacement(kongerSeat);
        }

        private void ClearRobKongState()
        {
            _robKongSeat = -1;
            _robKongTileId = -1;
            _robKongPongMeldIndex = -1;
            _robKongOriginalPong = null;
        }

        // ---------------------------------------------------------------- 回合推进

        private void AdvanceToNextSeat()
        {
            _discardWasLastTile = false;
            _canSelfDraw = false;

            if (_wall.IsEmpty)
            {
                FinishDraw("牌墙摸完");
                return;
            }

            CurrentSeat = Seat.Next(CurrentSeat);
            DrawForCurrent();
        }

        private void DrawForCurrent()
        {
            if (!_wall.TryDraw(out int tileId))
            {
                FinishDraw("牌墙摸完");
                return;
            }

            PlayerState player = _players[CurrentSeat];
            player.Hand.Add(tileId);

            _canSelfDraw = true;
            _afterKongReplacement = false;
            _lastDrawnTileId = tileId;
            _lastDrawWasLastWallTile = _wall.Remaining == 0;
            Phase = GamePhase.AwaitingDiscard;

            EnsureProgress();
        }

        private void DrawReplacement(int seat)
        {
            if (!_wall.TryDraw(out int tileId))
            {
                FinishDraw("杠后无牌可补");
                return;
            }

            PlayerState player = _players[seat];
            player.Hand.Add(tileId);

            CurrentSeat = seat;
            _canSelfDraw = true;
            _afterKongReplacement = true;
            _lastDrawnTileId = tileId;
            _lastDrawWasLastWallTile = _wall.Remaining == 0;
            Phase = GamePhase.AwaitingDiscard;

            EnsureProgress();
        }

        private void EnsureProgress()
        {
            if (Phase != GamePhase.AwaitingDiscard)
            {
                return;
            }

            LegalActionSet actions = GetLegalActions(CurrentSeat);
            if (!actions.HasAnyAction)
            {
                FinishDraw("无可执行动作");
            }
        }

        // ---------------------------------------------------------------- 结算

        private void FinishWin(int winnerSeat, WinContext context, WinResult win)
        {
            ScoreBreakdown score = ScoreCalculator.Calculate(
                winnerSeat, win, context, _players, DealerSeat, _rules);

            Result = new HandResult
            {
                IsDraw = false,
                WinnerSeat = winnerSeat,
                DiscarderSeat = context.IsSelfDraw ? -1 : context.DiscarderSeat,
                IsRobKong = context.IsRobKong,
                DealerSeat = DealerSeat,
                Context = context,
                Win = win,
                Score = score
            };

            Phase = GamePhase.HandEnded;
            AddEvent("win", winnerSeat,
                $"座位{winnerSeat} 胡牌：{win.Describe()}，赢得 {score.Deltas[winnerSeat]} 分。");
        }

        private void FinishDraw(string reason)
        {
            Result = new HandResult
            {
                IsDraw = true,
                DealerSeat = DealerSeat,
                Score = ScoreCalculator.CreateDraw(DealerSeat)
            };

            Phase = GamePhase.HandEnded;
            AddEvent("draw", -1, $"荒庄：{reason}。");
        }

        private void AddEvent(string type, int seat, string message)
        {
            _events.Add(new GameEvent(type, seat, message));
        }
    }
}
