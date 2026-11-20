using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WuXueMaJong.Core;

namespace WuXueMaJong.Client
{
    /// <summary>单机对局驱动：人类玩家 + 3 AI，或全 AI 观战。</summary>
    public sealed class MahjongGameApp : MonoBehaviour
    {
        public GameEngine Engine => _engine;
        public MahjongSession Session => _session;
        public bool IsHandEnded => _engine != null && _engine.Phase == GamePhase.HandEnded;

        private MahjongTableUI _ui;
        private MahjongSession _session;
        private GameEngine _engine;
        private BotAI[] _bots;
        private int _humanSeat = 0;
        private bool _spectator;
        private bool _fast;
        private int _selectedTileId = -1;
        private Coroutine _loop;
        private float _humanClaimDeadline = -1f;
        private float _humanRobDeadline = -1f;
        private long _seedBase;
        private int _handCounter;

        private void Awake()
        {
            _ui = new MahjongTableUI(transform);

            _ui.OnStartHuman += () => StartGame(false, NewSeed());
            _ui.OnStartSpectator += () => StartGame(true, NewSeed(), true);
            _ui.OnTileClicked += OnTileClicked;
            _ui.OnDiscard += TryHumanDiscard;
            _ui.OnWin += TryHumanWin;
            _ui.OnConcealedKong += kind => ExecuteOwn(_engine?.DeclareConcealedKong(_humanSeat, kind));
            _ui.OnAddedKong += kind => ExecuteOwn(_engine?.DeclareAddedKong(_humanSeat, kind));
            _ui.OnRedCenterGong += TryRedCenterGong;
            _ui.OnLaiziGong += TryLaiziGong;
            _ui.OnClaim += TryHumanClaim;
            _ui.OnRobKong += TryHumanRobKong;
            _ui.OnNextHand += StartNextHand;
            _ui.OnRestart += () => StartGame(_spectator, NewSeed(), _fast);

            _ui.ShowMenu();
            TryAutoStartFromCommandLine();
        }

        private void TryAutoStartFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            int modeIndex = Array.IndexOf(args, "-mahjong-autostart");
            if (modeIndex < 0 || modeIndex + 1 >= args.Length)
            {
                return;
            }

            long seed = 2026L;
            int seedIndex = Array.IndexOf(args, "-mahjong-seed");
            if (seedIndex >= 0 && seedIndex + 1 < args.Length &&
                long.TryParse(args[seedIndex + 1], out long parsedSeed))
            {
                seed = parsedSeed;
            }

            switch (args[modeIndex + 1])
            {
                case "human":
                    StartGame(false, seed);
                    break;
                case "spectator":
                    StartGame(true, seed);
                    break;
                case "spectator-fast":
                    StartGame(true, seed, true);
                    break;
            }
        }

        /// <summary>供 PlayMode 测试和观战使用。</summary>
        public void StartSpectatorFast(long seed)
        {
            StartGame(true, seed, true);
        }

        public void StartGame(bool spectator, long seed, bool fast = false)
        {
            StopLoop();

            _spectator = spectator;
            _fast = fast;
            _humanSeat = spectator ? -1 : 0;
            _session = new MahjongSession(0);
            _seedBase = seed;
            _handCounter = 0;

            BeginHand(seed);
            _ui.ShowTable();
            Refresh();
            _loop = StartCoroutine(GameLoop());
        }

        private void BeginHand(long seed)
        {
            _engine = _session.StartHand(seed);
            _bots = AutoGameRunner.CreateBots(seed);
            _selectedTileId = -1;
            _humanClaimDeadline = -1f;
            _humanRobDeadline = -1f;
        }

        private void StartNextHand()
        {
            if (_session == null)
            {
                return;
            }

            StopLoop();
            _session.ApplyResult();

            _handCounter++;
            long seed = _seedBase + _handCounter * 7919L + 17L;
            BeginHand(seed);
            _ui.ShowTable();
            Refresh();
            _loop = StartCoroutine(GameLoop());
        }

        private void StopLoop()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
        }

        private void Refresh()
        {
            if (_engine == null)
            {
                return;
            }

            _ui.Refresh(_session, _engine, _humanSeat, _selectedTileId);
        }

        // ---------------------------------------------------------------- 人类操作

        private void OnTileClicked(int tileId)
        {
            if (!CanHumanAct())
            {
                return;
            }

            if (_selectedTileId == tileId)
            {
                TryHumanDiscard();
                return;
            }

            _selectedTileId = tileId;
            Refresh();
        }

        private bool CanHumanAct()
        {
            return !_spectator &&
                   _engine != null &&
                   _engine.Phase == GamePhase.AwaitingDiscard &&
                   _engine.CurrentSeat == _humanSeat;
        }

        private void TryHumanDiscard()
        {
            if (!CanHumanAct())
            {
                return;
            }

            if (_selectedTileId < 0)
            {
                _ui.ShowHint("先选择一张牌，再点击「打出」。");
                return;
            }

            int kind = TileCatalog.KindOf(_selectedTileId);
            if (kind == TileCatalog.RedCenter)
            {
                _ui.ShowHint("红中不能普通打出，请点击「红中杠」。");
                return;
            }

            if (kind == _engine.JokerKind)
            {
                _ui.ShowHint("癞子不能普通打出，请点击「癞子杠」。");
                return;
            }

            CommandResult result = _engine.Discard(_humanSeat, _selectedTileId);
            if (!result.Success)
            {
                _ui.ShowHint(result.Error);
                return;
            }

            _selectedTileId = -1;
            Refresh();
        }

        private void TryHumanWin()
        {
            if (!CanHumanAct())
            {
                return;
            }

            ExecuteOwn(_engine.DeclareWin(_humanSeat));
        }

        private void TryRedCenterGong()
        {
            if (!CanHumanAct())
            {
                return;
            }

            LegalActionSet legal = _engine.GetLegalActions(_humanSeat);
            if (legal.RedCenterGongTileIds.Count == 0)
            {
                _ui.ShowHint("当前没有可杠的红中。");
                return;
            }

            ExecuteOwn(_engine.DeclareRedCenterGong(_humanSeat, legal.RedCenterGongTileIds[0]));
        }

        private void TryLaiziGong()
        {
            if (!CanHumanAct())
            {
                return;
            }

            LegalActionSet legal = _engine.GetLegalActions(_humanSeat);
            if (legal.LaiziGongTileIds.Count == 0)
            {
                _ui.ShowHint("当前没有可杠的癞子。");
                return;
            }

            ExecuteOwn(_engine.DeclareLaiziGong(_humanSeat, legal.LaiziGongTileIds[0]));
        }

        private void ExecuteOwn(CommandResult result)
        {
            if (result == null)
            {
                return;
            }

            if (!result.Success)
            {
                _ui.ShowHint(result.Error);
                return;
            }

            _selectedTileId = -1;
            Refresh();
        }

        private void TryHumanClaim(ClaimDecision decision)
        {
            if (_spectator || _engine == null || _engine.Phase != GamePhase.ClaimWindow)
            {
                return;
            }

            CommandResult result = _engine.SubmitClaim(_humanSeat, decision);
            if (!result.Success)
            {
                _ui.ShowHint(result.Error);
                return;
            }

            _humanClaimDeadline = -1f;
            Refresh();
        }

        private void TryHumanRobKong(bool declareWin)
        {
            if (_spectator || _engine == null || _engine.Phase != GamePhase.RobKongWindow)
            {
                return;
            }

            CommandResult result = _engine.SubmitRobKongDecision(_humanSeat, declareWin);
            if (!result.Success)
            {
                _ui.ShowHint(result.Error);
                return;
            }

            _humanRobDeadline = -1f;
            Refresh();
        }

        // ---------------------------------------------------------------- 主循环

        private IEnumerator GameLoop()
        {
            while (_engine != null && _engine.Phase != GamePhase.HandEnded)
            {
                switch (_engine.Phase)
                {
                    case GamePhase.AwaitingDiscard:
                        if (!_spectator && _engine.CurrentSeat == _humanSeat)
                        {
                            yield return null;
                            continue;
                        }

                        yield return ThinkDelay();
                        if (_engine.Phase != GamePhase.AwaitingDiscard)
                        {
                            continue;
                        }

                        RunBotTurn(_engine.CurrentSeat);
                        break;

                    case GamePhase.ClaimWindow:
                        yield return HandleClaimWindow();
                        break;

                    case GamePhase.RobKongWindow:
                        yield return HandleRobKongWindow();
                        break;
                }

                yield return null;
            }

            if (_session != null && _engine != null && _engine.Result != null)
            {
                _session.ApplyResult();
                Refresh();
            }
        }

        private void RunBotTurn(int seat)
        {
            BotObservation observation = BotObservation.From(_engine, seat);
            LegalActionSet legal = _engine.GetLegalActions(seat);
            CommandResult result = _bots[seat].ActTurn(_engine, observation, legal);
            if (!result.Success)
            {
                Debug.LogWarning($"AI 动作失败（座位{seat}）：{result.Error}");
            }

            Refresh();
        }

        private IEnumerator HandleClaimWindow()
        {
            List<int> pending = _engine.PendingClaimSeats.ToList();

            foreach (int seat in pending)
            {
                if (seat == _humanSeat)
                {
                    continue;
                }

                yield return ThinkDelay();
                if (_engine.Phase != GamePhase.ClaimWindow)
                {
                    yield break;
                }

                if (!_engine.PendingClaimSeats.Contains(seat))
                {
                    continue;
                }

                ClaimOption option = _engine.GetClaimOption(seat);
                ClaimDecision decision = _bots[seat].DecideClaim(option, BotObservation.From(_engine, seat));
                _engine.SubmitClaim(seat, decision);
                Refresh();
            }

            if (_engine.Phase == GamePhase.ClaimWindow &&
                !_spectator &&
                _engine.PendingClaimSeats.Contains(_humanSeat))
            {
                if (_humanClaimDeadline < 0f)
                {
                    _humanClaimDeadline = Time.unscaledTime + 8f;
                }

                if (Time.unscaledTime >= _humanClaimDeadline)
                {
                    _engine.SubmitClaim(_humanSeat, ClaimDecision.Pass());
                    _humanClaimDeadline = -1f;
                    Refresh();
                }
                else
                {
                    yield return null;
                }
            }
        }

        private IEnumerator HandleRobKongWindow()
        {
            List<int> pending = _engine.PendingRobKongSeats.ToList();

            foreach (int seat in pending)
            {
                if (seat == _humanSeat)
                {
                    continue;
                }

                yield return ThinkDelay();
                if (_engine.Phase != GamePhase.RobKongWindow)
                {
                    yield break;
                }

                if (!_engine.PendingRobKongSeats.Contains(seat))
                {
                    continue;
                }

                RobKongOption option = _engine.GetRobKongOption(seat);
                _engine.SubmitRobKongDecision(seat, option != null && option.CanHu);
                Refresh();
            }

            if (_engine.Phase == GamePhase.RobKongWindow &&
                !_spectator &&
                _engine.PendingRobKongSeats.Contains(_humanSeat))
            {
                if (_humanRobDeadline < 0f)
                {
                    _humanRobDeadline = Time.unscaledTime + 8f;
                }

                if (Time.unscaledTime >= _humanRobDeadline)
                {
                    _engine.SubmitRobKongDecision(_humanSeat, false);
                    _humanRobDeadline = -1f;
                    Refresh();
                }
                else
                {
                    yield return null;
                }
            }
        }

        private IEnumerator ThinkDelay()
        {
            if (_fast)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(0.35f);
            }
        }

        private static long NewSeed()
        {
            return DateTime.UtcNow.Ticks & 0x7FFFFFFF;
        }
    }
}
