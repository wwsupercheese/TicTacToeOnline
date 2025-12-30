using Grpc.Net.Client;
using TicTacToeOnline.Protos;
using Consul;
using System.Configuration;
using System.Text;
using Grpc.Core;

namespace TicTacToeOnline
{
    public partial class MainForm : Form
    {
        private GameService.GameServiceClient? _client;
        private GrpcChannel? _currentChannel;
        private string? _currentLeaderUrl;
        private string? _gameId;
        private string _playerId = "";
        private SmallBoardUC[] _boards = new SmallBoardUC[9];
        private System.Windows.Forms.Timer _timer = new();
        private CancellationTokenSource _cts = new();

        public MainForm()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            InitializeComponent();
            for (int i = 0; i < 9; i++)
            {
                _boards[i] = new SmallBoardUC(i);
                int idx = i;
                _boards[i].OnCellClick += (cIdx) => MakeMove(idx, cIdx);
                flowLayoutPanel1.Controls.Add(_boards[i]);
            }
            _timer.Interval = 2000;
            _timer.Tick += async (s, e) => await RefreshState();
            _ = MonitorLeaderAsync();
        }

        private async Task MonitorLeaderAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    string leaderUrl = await GetLeaderUrlFromConsul();
                    if (string.IsNullOrEmpty(leaderUrl)) 
                    {
                        ResetConnectionAndSearch("Ожидание лидера серверов...");
                    }
                    else if (leaderUrl != _currentLeaderUrl) 
                    {
                        await UpdateClientConnection(leaderUrl);
                    }
                }
                catch { SetOfflineState("Ошибка Consul"); }
                await Task.Delay(2000); 
            }
        }

        private async Task<string> GetLeaderUrlFromConsul()
        {
            try
            {
                string consulAddr = ConfigurationManager.AppSettings["ConsulAddress"] ?? "http://localhost:8500";
                using var consul = new ConsulClient(c => c.Address = new Uri(consulAddr));
                var kv = await consul.KV.Get("service/tictactoe-service/leader");
                if (kv.Response?.Value != null) return Encoding.UTF8.GetString(kv.Response.Value);
            }
            catch { }
            return string.Empty;
        }

        private async Task UpdateClientConnection(string leaderUrl)
        {
            try
            {
                _currentLeaderUrl = leaderUrl;
                if (_currentChannel != null) await _currentChannel.ShutdownAsync();
                _currentChannel = GrpcChannel.ForAddress(leaderUrl);
                _client = new GameService.GameServiceClient(_currentChannel);
                Console.WriteLine($"[Client] Подключен к новому лидеру: {leaderUrl}");
            }
            catch { ResetConnectionAndSearch("Сбой подключения"); }
        }

        private void SetOfflineState(string msg)
        {
            if (this.InvokeRequired) { this.BeginInvoke(new Action(() => SetOfflineState(msg))); return; }
            lblStatus.Text = "ПАУЗА: " + msg;
            lblStatus.BackColor = Color.LightCoral;
            LockBoard();
        }

        private void LockBoard()
        {
            foreach (var b in _boards)
            {
                b.SetHighlight(false);
                foreach (Control c in b.Controls) if (c is Button btn) btn.Enabled = false;
            }
        }

        private async Task<bool> EnsureClient() => _client != null;

        private void HandleException(Exception ex)
        {
            if (ex is RpcException rpcEx && (rpcEx.StatusCode == StatusCode.Unavailable || rpcEx.Status.Detail == "SYSTEM_SYNCING"))
                SetOfflineState("Синхронизация системы (ORM/DB)...");
            else
                ResetConnectionAndSearch("Переподключение...");
        }

        private async void OnCheckPlayer()
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || !await EnsureClient()) return;
            _playerId = txtLogin.Text.Trim();

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var res = await _client!.CheckSessionAsync(new CheckRequest { PlayerId = _playerId });
                    if (res.Exists) StartGameSession(_playerId, res.GameId, true);
                    else { loginPanel.Visible = false; roomPanel.Visible = true; }
                    return;
                }
                catch (Exception ex)
                {
                    if (ex is RpcException rpcEx && rpcEx.Status.Detail == "SYSTEM_SYNCING" && attempt < 3)
                    {
                        lblStatus.Text = $"Синхронизация системы... (попытка {attempt})";
                        lblStatus.BackColor = Color.LightSkyBlue;
                        await Task.Delay(1500);
                        continue;
                    }
                    HandleException(ex);
                    break;
                }
            }
        }

        private async void StartGameSession(string nick, string rid, bool isJoin)
        {
            if (!await EnsureClient()) return;
            _gameId = rid;
            try
            {
                var r = await _client!.CreateGameAsync(new CreateRequest { 
                    PlayerId = $"{nick}|{rid}",
                    IsJoinOnly = isJoin 
                });

                // ОБРАБОТКА ОШИБОК ЧЕРЕЗ СТАТУСНУЮ СТРОКУ
                if (!string.IsNullOrEmpty(r.Error))
                {
                    if (r.Error == "ROOM_NOT_FOUND")
                    {
                        lblStatus.Text = $"ОШИБКА: Комната #{rid} не найдена!";
                        lblStatus.BackColor = Color.LightCoral;
                    }
                    else if (r.Error == "ROOM_FULL")
                    {
                        lblStatus.Text = $"ОШИБКА: В комнате #{rid} нет мест!";
                        lblStatus.BackColor = Color.LightCoral;
                    }
                    else
                    {
                        lblStatus.Text = "ОШИБКА: " + r.Error;
                        lblStatus.BackColor = Color.LightCoral;
                    }
                    return;
                }

                roomPanel.Visible = false; 
                loginPanel.Visible = false; 
                headerPanel.Visible = true; 
                flowLayoutPanel1.Visible = true;
                UpdateUI(r);
                _timer.Start();
            }
            catch (Exception ex) { HandleException(ex); }
        }

        private async void MakeMove(int bIdx, int cIdx)
        {
            if (!await EnsureClient() || _gameId == null) return;
            try
            {
                var r = await _client!.MakeMoveAsync(new MoveRequest { GameId = _gameId, PlayerId = _playerId, BoardX = bIdx % 3, BoardY = bIdx / 3, CellX = cIdx % 3, CellY = cIdx / 3 });
                UpdateUI(r);
            }
            catch (Exception ex) { HandleException(ex); }
        }

        private async Task RefreshState()
        {
            if (_gameId == null || !await EnsureClient()) return;
            try
            {
                var r = await _client!.GetStateAsync(new StateRequest { GameId = _gameId, PlayerId = _playerId });
                UpdateUI(r);
            }
            catch (Exception ex) { HandleException(ex); }
        }

        private void ResetConnectionAndSearch(string msg)
        {
            _client = null; _currentLeaderUrl = null;
            SetOfflineState(msg);
        }

        private void UpdateUI(GameResponse r)
        {
            if (this.InvokeRequired) { this.BeginInvoke(new Action(() => UpdateUI(r))); return; }
            if (!string.IsNullOrEmpty(r.Error) && r.Error == "NOT_FOUND") { SetOfflineState("Комната не найдена"); return; }

            lblInfo.Text = $"ВЫ: {_playerId} | КОМНАТА: #{r.GameId}\nИГРОКИ: X({r.PlayerX}) vs O({r.PlayerO})";
            btnNewGame.Visible = (r.Status != "Playing");
            bool isMyTurn = (r.CurrentPlayerId == _playerId) && (r.Status == "Playing");

            if (r.Status == "Playing")
            {
                lblStatus.Text = isMyTurn ? "ВАШ ХОД!" : $"Ход: {r.CurrentPlayerId}";
                lblStatus.BackColor = Color.SkyBlue;
            }
            else
            {
                lblStatus.Text = "ИГРА ЗАВЕРШЕНА: " + r.Status;
                lblStatus.BackColor = Color.LightYellow;
            }

            for (int i = 0; i < 9; i++)
            {
                string sub = GetSubBoardString(r.FullBoard, i);
                bool active = (r.ActiveBoardX == -1) || (r.ActiveBoardX == i % 3 && r.ActiveBoardY == i / 3);
                _boards[i].UpdateBoard(sub, r.SmallBoardWinners[i], isMyTurn, active);
                _boards[i].SetHighlight(active && isMyTurn && r.Status == "Playing");
            }
        }

        private string GetSubBoardString(string full, int bIdx)
        {
            if (string.IsNullOrEmpty(full) || full.Length < 81) return ".........";
            char[] sub = new char[9];
            int bX = bIdx % 3, bY = bIdx / 3;
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    sub[y * 3 + x] = full[((bY * 3 + y) * 9 + (bX * 3 + x))];
            return new string(sub);
        }

        private void OnRulesClick()
        {
            string rulesText = 
                "Ultimate Tic-Tac-Toe (Мега Крестики-Нолики)\n\n" +
                "1. Поле: Состоит из 9 малых полей 3x3, объединенных в одну большую сетку.\n\n" +
                "2. Начало: Первый игрок может сделать ход в любую из 81 клетки.\n\n" +
                "3. Направление хода: Ваш выбор клетки в малом поле определяет, в каком малом поле должен ходить противник.\n" +
                "   (Например: если вы пошли в правый верхний угол малого поля, противник ОБЯЗАН ходить в малом поле, которое находится в правом верхнем углу большой сетки).\n\n" +
                "4. Свободный ход: Если малая доска, в которую вас отправили, уже выиграна кем-то или полностью заполнена, вы получаете «свободный ход» и можете играть в любой доступной клетке на любой другой доске.\n\n" +
                "5. Победа в малом поле: Доска считается захваченной игроком, если он собрал 3 в ряд в пределах этого поля.\n\n" +
                "6. Победа в игре: Цель — захватить три малых поля, которые образуют линию (горизонталь, вертикаль или диагональ) в масштабе большой сетки.";

            MessageBox.Show(rulesText, "Правила игры Ultimate Tic-Tac-Toe", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnCreateRoom() { StartGameSession(_playerId, new Random().Next(1000, 9999).ToString(), false); }
        private void OnJoinRoom() { if (txtRoomId.Text.Length == 4) StartGameSession(_playerId, txtRoomId.Text, true); }
        private async void OnResetGame() { try { var r = await _client!.ResetGameAsync(new StateRequest { GameId = _gameId!, PlayerId = _playerId }); UpdateUI(r); } catch (Exception ex) { HandleException(ex); } }
        private async void OnExitRoom() { try { if (await EnsureClient()) await _client!.ExitGameAsync(new ExitRequest { GameId = _gameId!, PlayerId = _playerId }); } catch { } Application.Restart(); }
        protected override void OnFormClosing(FormClosingEventArgs e) { _cts.Cancel(); base.OnFormClosing(e); }
    }
}