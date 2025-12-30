using Grpc.Core;
using TicTacToeOnline.Protos; // ИСПРАВЛЕНО: правильный namespace из .proto файла
using TicTacToeOnlineServer.Logic;
using Consul;
using Grpc.Net.Client;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var port = builder.Configuration.GetValue<int>("port", 5001);
var consulAddr = builder.Configuration.GetValue<string>("ConsulAddress") ?? "http://localhost:8500";

builder.WebHost.ConfigureKestrel(options => {
    options.ListenAnyIP(port, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

builder.Services.AddGrpc();
builder.Services.AddSingleton<GameServiceImpl>();

var app = builder.Build();
app.MapGrpcService<GameServiceImpl>();

app.Lifetime.ApplicationStarted.Register(async () => {
    try {
        var consul = new ConsulClient(c => c.Address = new Uri(consulAddr));
        var hostIp = GetLocalIPAddress();
        var serviceId = $"tictactoe-server-{port}";
        var myUrl = $"http://{hostIp}:{port}";

        await consul.Agent.ServiceRegister(new AgentServiceRegistration {
            ID = serviceId, Name = "tictactoe-service", Address = hostIp, Port = port,
            Check = new AgentServiceCheck { TCP = $"{hostIp}:{port}", Interval = TimeSpan.FromSeconds(2) }
        });

        _ = Task.Run(async () => {
            string leaderKey = "service/tictactoe-service/leader";
            while (!app.Lifetime.ApplicationStopping.IsCancellationRequested) {
                string sessionId = "";
                try {
                    var sResp = await consul.Session.Create(new SessionEntry { 
                        Name = $"game-leader-{port}", TTL = TimeSpan.FromSeconds(10), 
                        LockDelay = TimeSpan.Zero, Behavior = SessionBehavior.Delete 
                    });
                    sessionId = sResp.Response;
                    var kv = new KVPair(leaderKey) { Session = sessionId, Value = Encoding.UTF8.GetBytes(myUrl) };
                    bool acquired = (await consul.KV.Acquire(kv)).Response;
                    if (acquired) {
                        while (acquired && !app.Lifetime.ApplicationStopping.IsCancellationRequested) {
                            await consul.Session.Renew(sessionId);
                            await Task.Delay(1000);
                            var curr = await consul.KV.Get(leaderKey);
                            if (curr.Response == null || curr.Response.Session != sessionId) break;
                        }
                    } else { await Task.Delay(1000); }
                } catch { await Task.Delay(1000); }
                finally { if (!string.IsNullOrEmpty(sessionId)) try { await consul.Session.Destroy(sessionId); } catch { } }
            }
        });
    } catch (Exception ex) { Console.WriteLine($"[Consul Error] {ex.Message}"); }
});

app.Run();

static string GetLocalIPAddress() {
    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()) {
        if (ni.OperationalStatus != OperationalStatus.Up || (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 && ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet)) continue;
        foreach (var ip in ni.GetIPProperties().UnicastAddresses)
            if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !ip.Address.ToString().StartsWith("127.")) return ip.Address.ToString();
    }
    return "127.0.0.1";
}

public class GameServiceImpl : GameService.GameServiceBase {
    private TicTacToeOnlineORM.Orm.OrmClient? _ormClient;
    private string? _currentOrmLeaderUrl;
    private readonly ConsulClient _consul;

    public GameServiceImpl(IConfiguration config) {
        string cAddr = config.GetValue<string>("ConsulAddress") ?? "http://localhost:8500";
        _consul = new ConsulClient(c => c.Address = new Uri(cAddr));
        _ = MonitorOrmLeader();
    }

    private async Task MonitorOrmLeader() {
        while (true) {
            try {
                var kv = await _consul.KV.Get("service/tictactoe-orm/leader");
                if (kv.Response?.Value != null) {
                    string url = Encoding.UTF8.GetString(kv.Response.Value);
                    if (url != _currentOrmLeaderUrl) {
                        _currentOrmLeaderUrl = url;
                        _ormClient = new TicTacToeOnlineORM.Orm.OrmClient(GrpcChannel.ForAddress(url));
                    }
                } else { _ormClient = null; _currentOrmLeaderUrl = null; }
            } catch { }
            await Task.Delay(1000);
        }
    }

    private async Task<TicTacToeOnlineORM.Orm.OrmClient> GetOrmClientAsync() {
        for (int i = 0; i < 10; i++) {
            if (_ormClient != null) return _ormClient;
            await Task.Delay(500); 
        }
        throw new RpcException(new Grpc.Core.Status(StatusCode.Unavailable, "SYSTEM_SYNCING"));
    }

    // ИСПРАВЛЕНО: сигнатуры методов теперь используют правильный namespace
    public override async Task<CheckResponse> CheckSession(CheckRequest r, ServerCallContext c) {
        var orm = await GetOrmClientAsync();
        var res = await orm.CheckSessionAsync(new TicTacToeOnlineORM.CheckRequest { PlayerId = r.PlayerId });
        return new CheckResponse { Exists = res.Exists, GameId = res.GameId };
    }

    public override async Task<GameResponse> CreateGame(CreateRequest r, ServerCallContext c) {
        var parts = r.PlayerId.Split('|');
        if (parts.Length < 2) return new GameResponse { Error = "ID_ERR" };
        var nick = parts[0]; var roomId = parts[1];
        var g = await Load(roomId);

        if (g == null) {
            if (r.IsJoinOnly) return new GameResponse { Error = "ROOM_NOT_FOUND" };
            g = new UltimateGameLogic { PlayerX = nick, PlayerO = "" };
        } else {
            if (g.PlayerX == nick || g.PlayerO == nick) return Map(roomId, g);
            if (string.IsNullOrEmpty(g.PlayerX)) g.PlayerX = nick;
            else if (string.IsNullOrEmpty(g.PlayerO)) g.PlayerO = nick;
            else return new GameResponse { Error = "ROOM_FULL" };
        }
        await Save(roomId, g);
        return Map(roomId, g);
    }

    public override async Task<GameResponse> ResetGame(StateRequest r, ServerCallContext c) {
        var g = await Load(r.GameId);
        if (g == null) return new GameResponse { Error = "NOT_FOUND" };
        var nl = new UltimateGameLogic { PlayerX = g.PlayerX, PlayerO = g.PlayerO, Status = "Playing" };
        await Save(r.GameId, nl);
        return Map(r.GameId, nl);
    }

    public override async Task<ExitResponse> ExitGame(ExitRequest r, ServerCallContext c) {
        var orm = await GetOrmClientAsync();
        var resp = await orm.ExitGameAsync(new TicTacToeOnlineORM.ExitRequest { GameId = r.GameId, PlayerId = r.PlayerId });
        return new ExitResponse { Success = resp.Success };
    }

    public override async Task<GameResponse> MakeMove(MoveRequest r, ServerCallContext c) {
        var g = await Load(r.GameId);
        if (g == null) return new GameResponse { Error = "ROOM_ERR" };
        lock(g) { if (g.ValidMove(r.BoardX, r.BoardY, r.CellX, r.CellY, r.PlayerId)) g.MakeMove(r.BoardX, r.BoardY, r.CellX, r.CellY); }
        await Save(r.GameId, g);
        return Map(r.GameId, g);
    }

    public override async Task<GameResponse> GetState(StateRequest r, ServerCallContext c) {
        var g = await Load(r.GameId);
        return g != null ? Map(r.GameId, g) : new GameResponse { Error = "NOT_FOUND" };
    }

    private async Task Save(string id, UltimateGameLogic g) {
        var orm = await GetOrmClientAsync();
        await orm.SaveAsync(new TicTacToeOnlineORM.SaveRequest {
            GameId = id,
            Game = new TicTacToeOnlineORM.Game {
                Cells = new string(g.Cells), SmallWinners = new string(g.SmallWinners),
                ActiveBoardX = g.ActiveBoardX, ActiveBoardY = g.ActiveBoardY,
                PlayerX = g.PlayerX, PlayerO = g.PlayerO, IsXTurn = g.IsXTurn, Status = g.Status
            }
        });
    }

    private async Task<UltimateGameLogic?> Load(string id) {
        var orm = await GetOrmClientAsync();
        var res = await orm.LoadAsync(new TicTacToeOnlineORM.LoadRequest { GameId = id });
        if (res.Success && res.Game != null) return new UltimateGameLogic {
            Cells = res.Game.Cells.ToCharArray(), SmallWinners = res.Game.SmallWinners.ToCharArray(),
            ActiveBoardX = res.Game.ActiveBoardX, ActiveBoardY = res.Game.ActiveBoardY,
            PlayerX = res.Game.PlayerX, PlayerO = res.Game.PlayerO, IsXTurn = res.Game.IsXTurn, Status = res.Game.Status
        };
        return null;
    }

    private GameResponse Map(string id, UltimateGameLogic g) => new GameResponse {
        GameId = id, FullBoard = new string(g.Cells), SmallBoardWinners = new string(g.SmallWinners),
        CurrentPlayerId = g.IsXTurn ? g.PlayerX : g.PlayerO, Status = g.Status,
        ActiveBoardX = g.ActiveBoardX, ActiveBoardY = g.ActiveBoardY,
        PlayerX = g.PlayerX, PlayerO = g.PlayerO
    };
}