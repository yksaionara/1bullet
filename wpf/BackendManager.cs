using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace OneBullet;

// Owns the Python backend child process: finds it, launches it with the
// right loopback-only environment, waits for real health (never shows the
// UI a dead backend), captures its output to a log file on every launch,
// and restarts it a bounded number of times if it dies mid-session.
// Nothing here ever touches the network beyond 127.0.0.1.
public sealed class BackendManager : ObservableObject, IDisposable
{
    public enum BackendState { Starting, Connected, Reconnecting, Failed }

    private readonly ApiClient _api;
    private readonly string _logPath;
    private readonly object _logLock = new();
    private Process? _process;
    private CancellationTokenSource? _watchdogCts;
    private int _misses;
    private int _restarts;
    private DateTime _restartWindowStart = DateTime.UtcNow;
    private bool _disposed;

    private BackendState _state = BackendState.Starting;
    private string _statusText = "Starting backend…";
    private string _logTail = "";

    public BackendState State { get => _state; private set => Set(ref _state, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string LogTail { get => _logTail; private set => Set(ref _logTail, value); }
    public int BackendPort { get; private set; } = 5000;
    public string BaseUrl => $"http://127.0.0.1:{BackendPort}";
    public bool ManagesProcess { get; private set; } = true;

    public BackendManager(ApiClient api)
    {
        _api = api;
        var local = Environment.GetEnvironmentVariable("LOCALAPPDATA")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "AppData", "Local");
        Directory.CreateDirectory(Path.Combine(local, "1Bullet"));
        _logPath = Path.Combine(local, "1Bullet", "backend-console.log");
    }

    public string LogPath => _logPath;

    public void Fail(string message)
    {
        AppendLog($"[manager] {message}");
        State = BackendState.Failed;
        StatusText = message;
        RefreshLogTail();
    }

    public async Task<bool> StartAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        State = BackendState.Starting;

        var devUrl = Environment.GetEnvironmentVariable("ONEBULLET_BACKEND_URL");
        if (!string.IsNullOrWhiteSpace(devUrl))
        {
            // Developer mode: connect to an already-running backend, manage nothing.
            ManagesProcess = false;
            var uri = new Uri(devUrl);
            BackendPort = uri.Port;
            _api.SetBaseUrl(devUrl.TrimEnd('/'));
            return await WaitForHealthAsync(TimeSpan.FromSeconds(15), progress, ct).ConfigureAwait(false);
        }

        BackendPort = PickPort(int.TryParse(Environment.GetEnvironmentVariable("ONEBULLET_BACKEND_PORT"),
                                            out var p) ? p : 5000);
        var wsPort = PickPort(BackendPort + 2878, excluding: BackendPort);
        _api.SetBaseUrl(BaseUrl);

        KillProcess();
        StopWatchdog();
        _misses = 0;

        var exe = FindBackendExe()
            ?? throw new FileNotFoundException(
                "1bullet-backend.exe was not found next to 1bullet.exe. Reinstall 1 Bullet.");
        AppendLog($"[manager] launching {exe} on 127.0.0.1:{BackendPort} (ws {wsPort})");

        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["BACKEND_PORT"] = BackendPort.ToString();
        psi.Environment["WS_PORT"] = wsPort.ToString();
        psi.Environment["FRONTEND_URL"] = $"http://127.0.0.1:{BackendPort}";
        psi.Environment["SCOUT_NO_BROWSER"] = "1";
        psi.Environment["SCOUT_SYNC"] = "false";
        psi.Environment["DATA_SOURCE"] = "local";

        TruncateLogIfLarge();
        _process = Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start the backend process.");
        // The WPF parent is BelowNormal, but Riot processes launched through
        // offline mode must inherit Normal priority from the backend.
        try { _process.PriorityClass = ProcessPriorityClass.Normal; }
        catch { }
        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) => AppendLog($"[manager] backend exited (code {_process?.ExitCode})");
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) AppendLog(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) AppendLog(e.Data); };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        if (!await WaitForHealthAsync(TimeSpan.FromSeconds(45), progress, ct).ConfigureAwait(false))
            return false;

        StartWatchdog();
        return true;
    }

    public async Task<bool> WaitForHealthAsync(
        TimeSpan timeout, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                attempt.CancelAfter(TimeSpan.FromSeconds(3));
                var health = await _api.GetHealthAsync(attempt.Token).ConfigureAwait(false);
                if (health?.Ok == true)
                {
                    State = BackendState.Connected;
                    StatusText = $"Backend connected · v{health.AppVersion}";
                    return true;
                }
            }
            catch { }
            progress?.Report(StatusText);
            try { await Task.Delay(500, cts.Token).ConfigureAwait(false); }
            catch { break; }
        }
        State = BackendState.Failed;
        StatusText = "Backend did not start.";
        RefreshLogTail();
        return false;
    }

    public async Task RestartAsync()
    {
        if (_restartWindowStart.AddMinutes(10) < DateTime.UtcNow)
        {
            _restartWindowStart = DateTime.UtcNow;
            _restarts = 0;
        }
        if (_restarts >= 3 || !ManagesProcess)
        {
            State = BackendState.Failed;
            StatusText = "Backend keeps stopping. See the log, then Retry.";
            RefreshLogTail();
            return;
        }
        _restarts++;
        State = BackendState.Reconnecting;
        StatusText = $"Backend stopped — restarting ({_restarts}/3)…";
        KillProcess();
        await StartAsync(progress: null).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        StopWatchdog();
        if (!ManagesProcess || _process is null || _process.HasExited) return;
        try { await _api.RequestShutdownAsync().ConfigureAwait(false); } catch { }
        if (_process.WaitForExit(10_000) && _process.ExitCode == 42)
        {
            AppendLog("[manager] backend shut down cleanly (user request)");
            return;
        }
        KillProcess();
    }

    public void OpenLogs()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_logPath}\"")
            {
                UseShellExecute = true,
            });
        }
        catch { }
    }

    private void StartWatchdog()
    {
        StopWatchdog();
        _watchdogCts = new CancellationTokenSource();
        var token = _watchdogCts.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(15), token).ConfigureAwait(false); }
                catch { break; }
                if (State != BackendState.Connected) continue;
                var alive = await IsHealthyAsync().ConfigureAwait(false);
                if (token.IsCancellationRequested) break;
                if (alive) { _misses = 0; continue; }
                if (++_misses >= 3) { _misses = 0; await RestartAsync().ConfigureAwait(false); }
            }
        }, token);
    }

    private async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            return (await _api.GetHealthAsync(cts.Token).ConfigureAwait(false))?.Ok == true;
        }
        catch { return false; }
    }

    private void StopWatchdog()
    {
        try { _watchdogCts?.Cancel(); } catch { }
        _watchdogCts?.Dispose();
        _watchdogCts = null;
    }

    private void KillProcess()
    {
        try
        {
            var proc = _process;
            _process = null;
            if (proc is not null && !proc.HasExited) proc.Kill(entireProcessTree: true);
        }
        catch { }
    }

    private static string? FindBackendExe()
    {
        var overrideExe = Environment.GetEnvironmentVariable("ONEBULLET_BACKEND_EXE");
        if (!string.IsNullOrWhiteSpace(overrideExe) && File.Exists(overrideExe)) return overrideExe;
        var nextToApp = Path.Combine(AppContext.BaseDirectory, "1bullet-backend.exe");
        return File.Exists(nextToApp) ? nextToApp : null;
    }

    private static int PickPort(int preferred, int excluding = -1)
    {
        for (var port = preferred; port < preferred + 50; port++)
        {
            if (port == excluding) continue;
            try
            {
                using var listener = new TcpListener(
                    System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch { }
        }
        return preferred;
    }

    private void AppendLog(string line)
    {
        lock (_logLock)
        {
            try { File.AppendAllText(_logPath, $"[{DateTime.UtcNow:HH:mm:ss}] {line}{Environment.NewLine}"); }
            catch { }
        }
    }

    private void TruncateLogIfLarge()
    {
        try
        {
            var info = new FileInfo(_logPath);
            if (info.Exists && info.Length > 5_000_000) File.WriteAllText(_logPath, "");
        }
        catch { }
    }

    private void RefreshLogTail()
    {
        try
        {
            var lines = File.ReadAllLines(_logPath);
            LogTail = string.Join(Environment.NewLine, lines.TakeLast(40));
        }
        catch { LogTail = ""; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopWatchdog();
    }
}
