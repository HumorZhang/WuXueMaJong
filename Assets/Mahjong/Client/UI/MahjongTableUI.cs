using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WuXueMaJong.Core;

namespace WuXueMaJong.Client
{
    /// <summary>牌桌界面：代码构建，不依赖 prefab。</summary>
    public sealed class MahjongTableUI
    {
        private const float TileWidth = 64f;
        private const float TileHeight = 88f;
        private const float MiniTileWidth = 40f;
        private const float MiniTileHeight = 52f;

        private readonly RectTransform _root;
        private readonly Text _topInfo;
        private readonly Text _centerText;
        private readonly Text _hintText;
        private readonly Text[] _seatLabels = new Text[Seat.Count];
        private readonly RectTransform[] _riverRoots = new RectTransform[Seat.Count];
        private readonly RectTransform[] _backRoots = new RectTransform[Seat.Count];
        private readonly RectTransform _handRow;
        private readonly RectTransform _meldRow;
        private readonly RectTransform _actionBar;
        private readonly RectTransform _claimBar;
        private readonly GameObject _menuOverlay;
        private readonly GameObject _resultOverlay;
        private Text _resultText;

        public event Action OnStartHuman;
        public event Action OnStartSpectator;
        public event Action<int> OnTileClicked;
        public event Action OnDiscard;
        public event Action OnWin;
        public event Action<int> OnConcealedKong;
        public event Action<int> OnAddedKong;
        public event Action OnRedCenterGong;
        public event Action OnLaiziGong;
        public event Action<ClaimDecision> OnClaim;
        public event Action<bool> OnRobKong;
        public event Action OnNextHand;
        public event Action OnRestart;

        public MahjongTableUI(Transform parent)
        {
            _root = UiKit.CreateRect(parent, "TableUI");
            UiKit.Stretch(_root);

            Image background = UiKit.CreateImage(_root, "Background", UiKit.TableColor);
            UiKit.Stretch(background.rectTransform);

            _topInfo = UiKit.CreateText(_root, "TopInfo", "", 24, UiKit.TextLight);
            UiKit.Place(_topInfo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(1600f, 36f));

            Image centerPanel = UiKit.CreateImage(_root, "CenterPanel", UiKit.PanelColor);
            centerPanel.raycastTarget = false;
            UiKit.Place(centerPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(400f, 150f));

            _centerText = UiKit.CreateText(centerPanel.rectTransform, "CenterText", "", 22, UiKit.TextLight);
            UiKit.Stretch(_centerText.rectTransform, 12f);

            _hintText = UiKit.CreateText(_root, "Hint", "", 20, UiKit.Accent);
            UiKit.Place(_hintText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(1100f, 28f));

            BuildSeats();
            BuildRivers();

            _handRow = UiKit.CreateRect(_root, "HandRow");
            UiKit.Place(_handRow, new Vector2(0.5f, 0.5f), new Vector2(0f, -450f), new Vector2(1500f, TileHeight + 8f));

            _meldRow = UiKit.CreateRect(_root, "MeldRow");
            UiKit.Place(_meldRow, new Vector2(0.5f, 0.5f), new Vector2(0f, -272f), new Vector2(1300f, MiniTileHeight + 4f));

            _actionBar = UiKit.CreateRect(_root, "ActionBar");
            UiKit.Place(_actionBar, new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(1500f, 56f));
            _actionBar.gameObject.SetActive(false);

            _claimBar = UiKit.CreateRect(_root, "ClaimBar");
            UiKit.Place(_claimBar, new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(1500f, 56f));
            _claimBar.gameObject.SetActive(false);

            _resultOverlay = BuildResultOverlay();
            _menuOverlay = BuildMenuOverlay();
        }

        // ---------------------------------------------------------------- 构建

        private void BuildSeats()
        {
            for (int seat = 0; seat < Seat.Count; seat++)
            {
                Text label = UiKit.CreateText(_root, "SeatLabel" + seat, "", 17, UiKit.TextDim);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.lineSpacing = 0.95f;

                switch (seat)
                {
                    case 0:
                        UiKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -386f), new Vector2(900f, 26f));
                        break;
                    case 1:
                        UiKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760f, 150f), new Vector2(280f, 100f));
                        break;
                    case 2:
                        UiKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 310f), new Vector2(760f, 70f));
                        break;
                    default:
                        UiKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-760f, 150f), new Vector2(280f, 100f));
                        break;
                }

                _seatLabels[seat] = label;

                _backRoots[seat] = UiKit.CreateRect(_root, "Backs" + seat);
                switch (seat)
                {
                    case 0:
                        UiKit.Place(_backRoots[seat], new Vector2(0.5f, 0.5f), new Vector2(0f, -450f), new Vector2(760f, 40f));
                        break;
                    case 1:
                        UiKit.Place(_backRoots[seat], new Vector2(0.5f, 0.5f), new Vector2(760f, -60f), new Vector2(60f, 320f));
                        break;
                    case 2:
                        UiKit.Place(_backRoots[seat], new Vector2(0.5f, 0.5f), new Vector2(0f, 390f), new Vector2(760f, 40f));
                        break;
                    default:
                        UiKit.Place(_backRoots[seat], new Vector2(0.5f, 0.5f), new Vector2(-760f, -60f), new Vector2(60f, 320f));
                        break;
                }
            }
        }

        private void BuildRivers()
        {
            for (int seat = 0; seat < Seat.Count; seat++)
            {
                RectTransform container = UiKit.CreateRect(_root, "River" + seat);
                switch (seat)
                {
                    case 0:
                        UiKit.Place(container, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(760f, 112f));
                        break;
                    case 1:
                        UiKit.Place(container, new Vector2(0.5f, 0.5f), new Vector2(560f, 0f), new Vector2(176f, 430f));
                        break;
                    case 2:
                        UiKit.Place(container, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(760f, 112f));
                        break;
                    default:
                        UiKit.Place(container, new Vector2(0.5f, 0.5f), new Vector2(-560f, 0f), new Vector2(176f, 430f));
                        break;
                }

                Image bg = UiKit.CreateImage(container, "RiverBg", new Color(0f, 0f, 0f, 0.14f));
                UiKit.Stretch(bg.rectTransform);

                RectTransform tileRoot = UiKit.CreateRect(container, "Tiles");
                UiKit.Stretch(tileRoot);
                _riverRoots[seat] = tileRoot;
            }
        }

        private GameObject BuildMenuOverlay()
        {
            RectTransform overlay = UiKit.CreateRect(_root, "MenuOverlay");
            UiKit.Stretch(overlay);

            Image dim = UiKit.CreateImage(overlay, "Dim", UiKit.OverlayColor);
            dim.raycastTarget = true;
            UiKit.Stretch(dim.rectTransform);

            Image panel = UiKit.CreateImage(overlay, "MenuPanel", UiKit.PanelColor);
            panel.raycastTarget = true;
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 560f));

            Text title = UiKit.CreateText(panel.rectTransform, "Title", "武学麻将", 68, UiKit.Accent);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(800f, 90f));

            Text subtitle = UiKit.CreateText(panel.rectTransform, "Subtitle",
                "武汉红中赖子杠 · 单机版", 28, UiKit.TextDim);
            UiKit.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 40f));

            Text rules = UiKit.CreateText(panel.rectTransform, "Rules",
                "136 张牌 · 翻牌定癞子 · 二五八将 · 必须开口 · 口口翻计分", 20, UiKit.TextDim);
            UiKit.Place(rules.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(820f, 30f));

            Button human = UiKit.CreateButton(panel.rectTransform, "玩家 vs 3 AI", 300f, 64f,
                () => OnStartHuman?.Invoke(), 26);
            UiKit.Place(human.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(300f, 64f));

            Button spectator = UiKit.CreateButton(panel.rectTransform, "全 AI 观战", 300f, 64f,
                () => OnStartSpectator?.Invoke(), 26);
            UiKit.Place(spectator.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(300f, 64f));

            return overlay.gameObject;
        }

        private GameObject BuildResultOverlay()
        {
            RectTransform overlay = UiKit.CreateRect(_root, "ResultOverlay");
            UiKit.Stretch(overlay);

            Image dim = UiKit.CreateImage(overlay, "Dim", new Color(0f, 0f, 0f, 0.70f));
            dim.raycastTarget = true;
            UiKit.Stretch(dim.rectTransform);

            Image panel = UiKit.CreateImage(overlay, "ResultPanel", UiKit.PanelColor);
            panel.raycastTarget = true;
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 640f));

            Text title = UiKit.CreateText(panel.rectTransform, "ResultTitle", "本局结算", 34, UiKit.Accent);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 270f), new Vector2(800f, 50f));

            _resultText = UiKit.CreateText(panel.rectTransform, "ResultText", "", 20, UiKit.TextLight,
                TextAnchor.UpperLeft);
            _resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _resultText.lineSpacing = 1.05f;
            UiKit.Place(_resultText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(900f, 430f));

            Button next = UiKit.CreateButton(panel.rectTransform, "下一局", 240f, 60f,
                () => OnNextHand?.Invoke(), 24);
            UiKit.Place(next.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-140f, -260f), new Vector2(240f, 60f));

            Button restart = UiKit.CreateButton(panel.rectTransform, "重开", 180f, 60f,
                () => OnRestart?.Invoke(), 24);
            UiKit.Place(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(150f, -260f), new Vector2(180f, 60f));

            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        // ---------------------------------------------------------------- 显示控制

        public void ShowMenu()
        {
            _menuOverlay.SetActive(true);
            _resultOverlay.SetActive(false);
        }

        public void ShowTable()
        {
            _menuOverlay.SetActive(false);
            _resultOverlay.SetActive(false);
        }

        public void ShowHint(string message)
        {
            _hintText.text = message ?? string.Empty;
        }

        // ---------------------------------------------------------------- 刷新

        public void Refresh(MahjongSession session, GameEngine engine, int humanSeat, int selectedTileId)
        {
            if (engine == null)
            {
                return;
            }

            _menuOverlay.SetActive(false);

            _topInfo.text =
                $"第 {session.HandNumber} 局 · 牌墙 {engine.Wall.Remaining} 张 · " +
                $"指示牌 {TileCatalog.DisplayName(TileCatalog.KindOf(engine.IndicatorTileId))} · " +
                $"癞子 {TileCatalog.DisplayName(engine.JokerKind)}";

            for (int seat = 0; seat < Seat.Count; seat++)
            {
                PlayerState player = engine.Player(seat);
                _seatLabels[seat].text = BuildSeatLabel(session, engine, seat, humanSeat);
                _seatLabels[seat].color = IsSeatActive(engine, seat) ? UiKit.Accent : UiKit.TextDim;
                RebuildBacks(seat, player.Hand.Count,
                    seat == 0 ? 34f : (seat == 2 ? 26f : 16f),
                    seat == 0 ? 40f : (seat == 2 ? 34f : 22f),
                    seat == 1 || seat == 3);
                RebuildRiver(seat, player.Discards);
            }

            _centerText.text = BuildCenterText(engine, humanSeat);

            string lastEvent = engine.Events.Count > 0 ? engine.Events[engine.Events.Count - 1].Message : "";
            _hintText.text = lastEvent;

            RebuildHandAndMelds(engine, humanSeat, selectedTileId);
            RebuildActionBar(engine, humanSeat);
            RebuildClaimBar(engine, humanSeat);

            if (engine.Phase == GamePhase.HandEnded && engine.Result != null)
            {
                _resultText.text = BuildResultText(session, engine, humanSeat);
                _resultOverlay.SetActive(true);
            }
            else
            {
                _resultOverlay.SetActive(false);
            }
        }

        private static bool IsSeatActive(GameEngine engine, int seat)
        {
            switch (engine.Phase)
            {
                case GamePhase.AwaitingDiscard:
                    return engine.CurrentSeat == seat;
                case GamePhase.ClaimWindow:
                    return engine.ClaimOptions.ContainsKey(seat) &&
                           engine.PendingClaimSeats.Contains(seat);
                case GamePhase.RobKongWindow:
                    return engine.RobKongOptions.ContainsKey(seat) &&
                           engine.PendingRobKongSeats.Contains(seat);
                default:
                    return false;
            }
        }

        private string BuildSeatLabel(MahjongSession session, GameEngine engine, int seat, int humanSeat)
        {
            PlayerState player = engine.Player(seat);
            string name = seat == humanSeat
                ? "你"
                : (humanSeat >= 0 ? Seat.RelativeName(humanSeat, seat) : "座位" + seat);
            string dealer = seat == engine.DealerSeat ? " · 庄" : "";
            long total = session.Scores[seat];

            var builder = new StringBuilder();
            builder.Append(name).Append(dealer);
            builder.Append('\n').Append("累计 ").Append(total)
                .Append(" 分 · 手牌 ").Append(player.Hand.Count).Append(" 张");

            if (seat != humanSeat)
            {
                var melds = player.Melds.Select(m => m.DisplayName()).ToList();
                if (player.RedCenterGongCount > 0)
                {
                    melds.Add("红中杠×" + player.RedCenterGongCount);
                }

                if (player.LaiziGongCount > 0)
                {
                    melds.Add("癞子杠×" + player.LaiziGongCount);
                }

                if (melds.Count > 0)
                {
                    builder.Append('\n').Append(string.Join("  ", melds));
                }
            }

            return builder.ToString();
        }

        private string BuildCenterText(GameEngine engine, int humanSeat)
        {
            string current;
            switch (engine.Phase)
            {
                case GamePhase.ClaimWindow:
                    current = "等待响应";
                    break;
                case GamePhase.RobKongWindow:
                    current = "抢杠机会";
                    break;
                case GamePhase.HandEnded:
                    current = "本局结束";
                    break;
                default:
                    current = engine.CurrentSeat == humanSeat
                        ? "你"
                        : (humanSeat >= 0
                            ? Seat.RelativeName(humanSeat, engine.CurrentSeat)
                            : "座位" + engine.CurrentSeat);
                    current += " 行动";
                    break;
            }

            return $"牌墙：{engine.Wall.Remaining} 张\n" +
                   $"指示牌：{TileCatalog.DisplayName(TileCatalog.KindOf(engine.IndicatorTileId))}    " +
                   $"癞子：{TileCatalog.DisplayName(engine.JokerKind)}\n" +
                   $"当前：{current}";
        }

        private void RebuildHandAndMelds(GameEngine engine, int humanSeat, int selectedTileId)
        {
            UiKit.Clear(_handRow);
            UiKit.Clear(_meldRow);

            if (humanSeat < 0)
            {
                RebuildBacks(0, engine.Player(0).Hand.Count, 34f, 40f, false, _handRow);
                _backRoots[0].gameObject.SetActive(false);
                return;
            }

            PlayerState player = engine.Player(humanSeat);
            _backRoots[humanSeat].gameObject.SetActive(false);

            List<int> sorted = player.Hand
                .OrderBy(TileCatalog.KindOf)
                .ThenBy(t => t)
                .ToList();

            var tiles = new List<RectTransform>();
            foreach (int tileId in sorted)
            {
                TileView view = TileView.Create(
                    _handRow, tileId, engine.JokerKind, OnTileClicked, TileWidth, TileHeight, 28);
                view.SetSelected(tileId == selectedTileId);
                tiles.Add(view.GetComponent<RectTransform>());
            }

            LayoutRow(_handRow, tiles, 6f);

            var meldItems = new List<RectTransform>();
            foreach (Meld meld in player.Melds)
            {
                foreach (int tileId in meld.TileIds)
                {
                    TileView tile = TileView.Create(
                        _meldRow, tileId, engine.JokerKind, null,
                        MiniTileWidth, MiniTileHeight, 18, false);
                    meldItems.Add(tile.GetComponent<RectTransform>());
                }

                RectTransform gap = UiKit.CreateRect(_meldRow, "Gap");
                gap.sizeDelta = new Vector2(18f, 1f);
                meldItems.Add(gap);
            }

            foreach (SpecialGong gong in player.SpecialGongs)
            {
                TileView tile = TileView.Create(
                    _meldRow, gong.TileId, engine.JokerKind, null,
                    MiniTileWidth, MiniTileHeight, 18, false);
                meldItems.Add(tile.GetComponent<RectTransform>());
                RectTransform gap = UiKit.CreateRect(_meldRow, "Gap");
                gap.sizeDelta = new Vector2(18f, 1f);
                meldItems.Add(gap);
            }

            LayoutRow(_meldRow, meldItems, 3f);
        }

        private void RebuildBacks(
            int seat,
            int count,
            float width,
            float height,
            bool vertical,
            RectTransform overrideRoot = null)
        {
            RectTransform root = overrideRoot ?? _backRoots[seat];
            if (overrideRoot == null)
            {
                root.gameObject.SetActive(true);
            }

            UiKit.Clear(root);

            var items = new List<RectTransform>();
            for (int i = 0; i < count; i++)
            {
                TileView back = TileView.CreateBack(root, width, height);
                items.Add(back.GetComponent<RectTransform>());
            }

            if (vertical)
            {
                LayoutColumn(root, items, 1.5f);
            }
            else
            {
                LayoutRow(root, items, 2f);
            }
        }

        private void RebuildRiver(int seat, List<int> discards)
        {
            RectTransform root = _riverRoots[seat];
            UiKit.Clear(root);

            int columns = seat == 1 || seat == 3 ? 4 : 18;
            float cellW = 34f;
            float cellH = 44f;
            float spacing = 2f;

            for (int i = 0; i < discards.Count; i++)
            {
                TileView tile = TileView.Create(
                    root, discards[i], -1, null, cellW, cellH, 16, false);

                RectTransform rect = tile.GetComponent<RectTransform>();
                int col = i % columns;
                int row = i / columns;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(col * (cellW + spacing), -row * (cellH + spacing));
            }
        }

        private void RebuildActionBar(GameEngine engine, int humanSeat)
        {
            UiKit.Clear(_actionBar);
            bool myTurn = humanSeat >= 0 &&
                          engine.Phase == GamePhase.AwaitingDiscard &&
                          engine.CurrentSeat == humanSeat;
            _actionBar.gameObject.SetActive(myTurn);
            if (!myTurn)
            {
                return;
            }

            LegalActionSet legal = engine.GetLegalActions(humanSeat);
            var buttons = new List<RectTransform>();

            if (legal.CanWin)
            {
                buttons.Add(AddBarButton(_actionBar, "胡", () => OnWin?.Invoke()));
            }

            foreach (int kind in legal.ConcealedKongKinds)
            {
                int captured = kind;
                buttons.Add(AddBarButton(_actionBar, "暗杠 " + TileCatalog.DisplayName(kind),
                    () => OnConcealedKong?.Invoke(captured)));
            }

            foreach (int kind in legal.AddedKongKinds)
            {
                int captured = kind;
                buttons.Add(AddBarButton(_actionBar, "补杠 " + TileCatalog.DisplayName(kind),
                    () => OnAddedKong?.Invoke(captured)));
            }

            if (legal.RedCenterGongTileIds.Count > 0)
            {
                buttons.Add(AddBarButton(_actionBar, "红中杠", () => OnRedCenterGong?.Invoke()));
            }

            if (legal.LaiziGongTileIds.Count > 0)
            {
                buttons.Add(AddBarButton(_actionBar, "癞子杠", () => OnLaiziGong?.Invoke()));
            }

            buttons.Add(AddBarButton(_actionBar, "打出", () => OnDiscard?.Invoke()));

            LayoutRow(_actionBar, buttons, 14f);
        }

        private void RebuildClaimBar(GameEngine engine, int humanSeat)
        {
            UiKit.Clear(_claimBar);
            bool show = false;
            var buttons = new List<RectTransform>();

            if (humanSeat >= 0 && engine.Phase == GamePhase.ClaimWindow &&
                engine.PendingClaimSeats.Contains(humanSeat))
            {
                ClaimOption option = engine.GetClaimOption(humanSeat);
                if (option != null && option.HasAny)
                {
                    show = true;
                    if (option.CanHu)
                    {
                        buttons.Add(AddBarButton(_claimBar, "胡", () => OnClaim?.Invoke(ClaimDecision.Win())));
                    }

                    if (option.CanExposedKong)
                    {
                        buttons.Add(AddBarButton(_claimBar, "杠", () => OnClaim?.Invoke(ClaimDecision.Kong())));
                    }

                    if (option.CanPong)
                    {
                        buttons.Add(AddBarButton(_claimBar, "碰", () => OnClaim?.Invoke(ClaimDecision.Pong())));
                    }

                    foreach (ChiOption chi in option.ChiOptions)
                    {
                        ChiOption captured = chi;
                        buttons.Add(AddBarButton(_claimBar, chi.DisplayName(),
                            () => OnClaim?.Invoke(ClaimDecision.ChiWith(captured))));
                    }

                    buttons.Add(AddBarButton(_claimBar, "过", () => OnClaim?.Invoke(ClaimDecision.Pass())));
                }
            }
            else if (humanSeat >= 0 && engine.Phase == GamePhase.RobKongWindow &&
                     engine.PendingRobKongSeats.Contains(humanSeat))
            {
                RobKongOption option = engine.GetRobKongOption(humanSeat);
                if (option != null && option.CanHu)
                {
                    show = true;
                    buttons.Add(AddBarButton(_claimBar, "抢杠胡", () => OnRobKong?.Invoke(true)));
                    buttons.Add(AddBarButton(_claimBar, "过", () => OnRobKong?.Invoke(false)));
                }
            }

            _claimBar.gameObject.SetActive(show);
            if (show)
            {
                _actionBar.gameObject.SetActive(false);
                LayoutRow(_claimBar, buttons, 14f);
            }
        }

        private static RectTransform AddBarButton(Transform parent, string label, Action onClick)
        {
            float width = Mathf.Max(96f, 40f + label.Length * 23f);
            Button button = UiKit.CreateButton(parent, label, width, 52f, onClick, 22);
            return button.GetComponent<RectTransform>();
        }

        private static void LayoutRow(RectTransform container, List<RectTransform> items, float spacing)
        {
            if (items.Count == 0)
            {
                return;
            }

            float total = items.Sum(item => item.sizeDelta.x) + spacing * (items.Count - 1);
            float x = -total / 2f;

            foreach (RectTransform item in items)
            {
                float width = item.sizeDelta.x;
                item.anchorMin = new Vector2(0.5f, 0.5f);
                item.anchorMax = new Vector2(0.5f, 0.5f);
                item.pivot = new Vector2(0.5f, 0.5f);
                item.anchoredPosition = new Vector2(x + width / 2f, 0f);
                x += width + spacing;
            }
        }

        private static void LayoutColumn(RectTransform container, List<RectTransform> items, float spacing)
        {
            if (items.Count == 0)
            {
                return;
            }

            float total = items.Sum(item => item.sizeDelta.y) + spacing * (items.Count - 1);
            float y = total / 2f;

            foreach (RectTransform item in items)
            {
                float height = item.sizeDelta.y;
                item.anchorMin = new Vector2(0.5f, 0.5f);
                item.anchorMax = new Vector2(0.5f, 0.5f);
                item.pivot = new Vector2(0.5f, 0.5f);
                item.anchoredPosition = new Vector2(0f, y - height / 2f);
                y -= height + spacing;
            }
        }

        private static string BuildResultText(MahjongSession session, GameEngine engine, int humanSeat)
        {
            HandResult result = engine.Result;
            var builder = new StringBuilder();

            if (result.IsDraw)
            {
                builder.AppendLine("荒庄（流局） · 庄家连庄");
            }
            else
            {
                string method = result.IsRobKong
                    ? "抢杠胡"
                    : (result.Context.IsSelfDraw ? "自摸" : "点炮胡");
                builder.Append("座位").Append(result.WinnerSeat)
                    .Append(result.WinnerSeat == humanSeat ? "（你）" : "")
                    .Append(" 胡牌 · ").AppendLine(method);
                builder.AppendLine(result.Win.Describe());
                builder.AppendLine();
            }

            builder.AppendLine("本局得分 / 累计：");
            for (int seat = 0; seat < Seat.Count; seat++)
            {
                long delta = result.Score.Deltas[seat];
                long total = session.Scores[seat];
                builder.Append("  座位").Append(seat)
                    .Append(seat == humanSeat ? "（你）" : "")
                    .Append(seat == result.DealerSeat ? "（庄）" : "")
                    .Append("：")
                    .Append(delta.ToString("+0;-0;0"))
                    .Append("  /  累计 ")
                    .AppendLine(total.ToString());
            }

            if (!result.IsDraw && result.Score.DetailLines.Count > 0)
            {
                builder.AppendLine();
                foreach (string line in result.Score.DetailLines)
                {
                    builder.AppendLine(line);
                }
            }

            builder.AppendLine();
            builder.Append("下一局庄家：座位").Append(result.NextDealerSeat);
            return builder.ToString();
        }
    }
}
