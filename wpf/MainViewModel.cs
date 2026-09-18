using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace OneBullet;

public sealed class SkinViewModel : ObservableObject
{
    private ImageSource? _icon;
    public string Title { get; }
    public string Name { get; }
    public ImageSource? Icon { get => _icon; set => Set(ref _icon, value); }

    public SkinViewModel(string title, string name)
    {
        Title = title;
        Name = name;
    }
}

public sealed class PlayerViewModel : ObservableObject
{
    private readonly ImageCache _images;
    private ImageSource? _agentPortrait;
    private ImageSource? _rankIcon;
    private ImageSource? _cardBanner;

    public PlayerDto Dto { get; }
    public SkinViewModel Vandal { get; }
    public SkinViewModel Phantom { get; }

    public ImageSource? AgentPortrait { get => _agentPortrait; set => Set(ref _agentPortrait, value); }
    public ImageSource? RankIcon { get => _rankIcon; set => Set(ref _rankIcon, value); }
    public ImageSource? CardBanner { get => _cardBanner; set => Set(ref _cardBanner, value); }

    public string DisplayName => string.IsNullOrWhiteSpace(Dto.Name) ? "Player" : Dto.Name;
    public string HiddenTag => Dto.NameHidden ? "  (hidden)" : "";
    public string SubLine
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Dto.Agent)) parts.Add(Dto.Agent);
            parts.Add(string.IsNullOrWhiteSpace(Dto.Rank) ? "Unranked" : Dto.Rank);
            parts.Add($"{Dto.Rr} RR");
            if (!string.IsNullOrWhiteSpace(Dto.Selection)) parts.Add(Dto.Selection);
            return string.Join(" · ", parts);
        }
    }
    public string StatsLine
    {
        get
        {
            string F(double? v, string suffix = "") => v.HasValue ? $"{v.Value}{suffix}" : "—";
            return $"K/D {F(Dto.Kd)} · WR {F(Dto.WinRate, "%")} ({Dto.Games}) · HS {F(Dto.HsPct, "%")} · Lv {Dto.Level}{(Dto.LevelHidden ? " *" : "")}";
        }
    }
    public string PeakLine
    {
        get
        {
            var peak = string.IsNullOrWhiteSpace(Dto.PeakRank) ? "—" : Dto.PeakRank;
            if (!string.IsNullOrWhiteSpace(Dto.PeakAct)) peak += $" ({Dto.PeakAct})";
            return $"Peak {peak} · Prev {Dto.PreviousRank}";
        }
    }
    public bool HasParty => Dto.Party is not null;
    public string PartyText => Dto.Party is null ? "" : $"Party {Dto.Party.Number}";
    public string PartyColor => Dto.Party?.Color ?? "#888888";
    public bool IsSmurf => Dto.Smurf;
    public string EncounterText
    {
        get
        {
            var e = Dto.Encounter;
            if (e is null || e.WithCount + e.AgainstCount <= 0) return "";
            return $"Seen {e.WithCount + e.AgainstCount}× · with {e.WithCount} · against {e.AgainstCount}";
        }
    }

    public PlayerViewModel(PlayerDto dto, ImageCache images)
    {
        Dto = dto;
        _images = images;
        Vandal = MakeSkin("Vandal", dto);
        Phantom = MakeSkin("Phantom", dto);
        _ = LoadImagesAsync();
    }

    private static SkinViewModel MakeSkin(string weapon, PlayerDto dto)
    {
        var skin = dto.Weapons.FirstOrDefault(w =>
            string.Equals(w.Weapon, weapon, StringComparison.OrdinalIgnoreCase))?.Skin;
        return new SkinViewModel(weapon, string.IsNullOrWhiteSpace(skin?.Name) ? "—" : skin!.Name);
    }

    private async Task LoadImagesAsync()
    {
        try
        {
            var portrait = await _images.GetAsync(Dto.AgentPortrait).ConfigureAwait(true);
            AgentPortrait = portrait;
            var rank = await _images.GetAsync(Dto.RankIcon).ConfigureAwait(true);
            RankIcon = rank;
            var banner = await _images.GetAsync(Dto.PlayerCard).ConfigureAwait(true);
            CardBanner = banner;
            Vandal.Icon = await _images.GetAsync(
                Dto.Weapons.FirstOrDefault(w => w.Weapon == "Vandal")?.Skin?.Icon).ConfigureAwait(true);
            Phantom.Icon = await _images.GetAsync(
                Dto.Weapons.FirstOrDefault(w => w.Weapon == "Phantom")?.Skin?.Icon).ConfigureAwait(true);
        }
        catch { }
    }
}

public sealed class TeamGroup : ObservableObject
{
    public string Title { get; }
    public string Info { get; }
    public bool IsSelfTeam { get; }
    public ObservableCollection<PlayerViewModel> Players { get; } = new();

    public TeamGroup(string title, string info, bool isSelfTeam)
    {
        Title = title;
        Info = info;
        IsSelfTeam = isSelfTeam;
    }
}

public sealed class MainViewModel : ObservableObject
{
    public const string DefaultAccent = "#FF4655";

    private readonly ApiClient _api;
    private readonly BackendManager _backend;
    private readonly ImageCache _images;
    private readonly DispatcherTimer _boardTimer;
    private bool _started;
    private bool _quitting;
    private int _boardFailures;
    private string _bgKey = "";

    private string _stateLabel = "Starting…";
    private string _scoreLine = "";
    private string _noticeText = "";
    private bool _hasNotice;
    private string _emptyMessage = "Open VALORANT — lobby, Agent Select or a match.";
    private string _accentHex = DefaultAccent;
    private bool _startWithWindows;
    private bool _hasBackground;
    private ImageSource? _backgroundImage;
    private bool _hasUpdate;
    private string _updateText = "";
    private string? _updateUrl;
    private string _backendVersion = "";

    public ObservableCollection<TeamGroup> Teams { get; } = new();
    public IReadOnlyList<string> AccentPresets { get; } =
        new[] { "#FF4655", "#18E5A7", "#9ADEFF", "#FFB454", "#D864C7", "#ECE8E1" };

    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";

    public string VersionText => $"v{AppVersion}";
    public BackendManager Backend => _backend;

    public string StateLabel { get => _stateLabel; set => Set(ref _stateLabel, value); }
    public string ScoreLine { get => _scoreLine; set => Set(ref _scoreLine, value); }
    public string NoticeText { get => _noticeText; set => Set(ref _noticeText, value); }
    public bool HasNotice { get => _hasNotice; set => Set(ref _hasNotice, value); }
    public string EmptyMessage { get => _emptyMessage; set => Set(ref _emptyMessage, value); }
    public bool HasPlayers => Teams.Sum(t => t.Players.Count) > 0;
    public string AccentHex { get => _accentHex; set => Set(ref _accentHex, value); }
    public bool StartWithWindows { get => _startWithWindows; set => Set(ref _startWithWindows, value); }
    public bool HasBackground { get => _hasBackground; set => Set(ref _hasBackground, value); }
    public ImageSource? BackgroundImage { get => _backgroundImage; set => Set(ref _backgroundImage, value); }
    public bool HasUpdate { get => _hasUpdate; set => Set(ref _hasUpdate, value); }
    public string UpdateText { get => _updateText; set => Set(ref _updateText, value); }
    public string BackendVersion { get => _backendVersion; set => Set(ref _backendVersion, value); }

    public RelayCommand SaveAccentCommand { get; }
    public RelayCommand ResetAccentCommand { get; }
    public RelayCommand PickAccentCommand { get; }
    public RelayCommand BrowseBackgroundCommand { get; }
    public RelayCommand RemoveBackgroundCommand { get; }
    public RelayCommand QuitCommand { get; }
    public RelayCommand RetryBackendCommand { get; }
    public RelayCommand OpenLogsCommand { get; }
    public RelayCommand DownloadUpdateCommand { get; }
    public RelayCommand CheckUpdatesCommand { get; }

    public MainViewModel(ApiClient api, BackendManager backend, ImageCache images)
    {
        _api = api;
        _backend = backend;
        _images = images;
        _backend.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(BackendManager.StatusText)
                or nameof(BackendManager.State)) Raise(nameof(Backend));
        };

        SaveAccentCommand = new RelayCommand(_ => _ = SaveAccentAsync(AccentHex));
        ResetAccentCommand = new RelayCommand(_ => _ = SaveAccentAsync(DefaultAccent));
        PickAccentCommand = new RelayCommand(p => { if (p is string s) _ = SaveAccentAsync(s); });
        BrowseBackgroundCommand = new RelayCommand(_ => _ = BrowseBackgroundAsync());
        RemoveBackgroundCommand = new RelayCommand(_ => _ = RemoveBackgroundAsync());
        QuitCommand = new RelayCommand(_ => _ = QuitAsync());
        RetryBackendCommand = new RelayCommand(_ => _ = StartAsync());
        OpenLogsCommand = new RelayCommand(_ => _backend.OpenLogs());
        DownloadUpdateCommand = new RelayCommand(_ => OpenUpdateUrl());
        CheckUpdatesCommand = new RelayCommand(_ => _ = CheckForUpdatesAsync());

        _boardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _boardTimer.Tick += async (_, _) => await RefreshBoardAsync().ConfigureAwait(false);
    }

    public async Task StartAsync()
    {
        if (_started && Backend.State is not BackendManager.BackendState.Failed) return;
        _started = true;
        ApplyAccent(AccentHex);
        var progress = new Progress<string>(s => BackendVersion = s);
        if (!await _backend.StartAsync(progress).ConfigureAwait(true)) return;
        try
        {
            var health = await _api.GetHealthAsync().ConfigureAwait(true);
            if (health is not null) BackendVersion = $"Backend v{health.AppVersion}";
        }
        catch { }
        await LoadSettingsAsync().ConfigureAwait(true);
        await RefreshBoardAsync().ConfigureAwait(true);
        _ = CheckForUpdatesAsync();
        _boardTimer.Start();
    }

    public async Task ShutdownAsync()
    {
        if (_quitting) return;
        _quitting = true;
        try
        {
            _boardTimer.Stop();
            await _backend.StopAsync().ConfigureAwait(false);
        }
        catch { }
    }

    private async Task RefreshBoardAsync()
    {
        if (_backend.State != BackendManager.BackendState.Connected) return;
        BoardDto? board;
        try
        {
            board = await _api.GetBoardAsync().ConfigureAwait(true);
        }
        catch { board = null; }
        if (board is null)
        {
            if (++_boardFailures >= 3)
                StateLabel = "Reconnecting…";
            return;
        }
        _boardFailures = 0;
        StateLabel = string.IsNullOrWhiteSpace(board.StateLabel) ? board.State : board.StateLabel;
        ScoreLine = BuildScoreLine(board);
        HasNotice = !string.IsNullOrWhiteSpace(board.Notice?.Message);
        NoticeText = board.Notice?.Message ?? "";
        EmptyMessage = !string.IsNullOrWhiteSpace(board.Notice?.Message) ? board.Notice!.Message
            : !string.IsNullOrWhiteSpace(board.Error) ? board.Error
            : "Open VALORANT — lobby, Agent Select or a match.";

        Teams.Clear();
        if (board.Teams.Count > 0)
        {
            var ordered = board.Teams.OrderByDescending(kv => kv.Key == board.SelfTeam).ToList();
            foreach (var (team, players) in ordered)
            {
                var isSelf = team == board.SelfTeam;
                var title = board.Teams.Count == 1 ? "Players"
                    : isSelf ? "Your team" : "Enemy team";
                var info = "";
                if (board.TeamStats.TryGetValue(team, out var stats)
                    && !string.IsNullOrWhiteSpace(stats.AvgRank) && stats.AvgRank != "Unranked")
                    info = $"Avg {stats.AvgRank}";
                if (isSelf && board.WinProb.HasValue && board.State == "INGAME")
                    info = string.IsNullOrEmpty(info) ? $"Win {board.WinProb}%" : $"{info} · Win {board.WinProb}%";
                var group = new TeamGroup(title, info, isSelf);
                foreach (var p in players)
                    group.Players.Add(new PlayerViewModel(p, _images));
                Teams.Add(group);
            }
        }
        else if (board.Players.Count > 0)
        {
            var group = new TeamGroup("Players", "", true);
            foreach (var p in board.Players)
                group.Players.Add(new PlayerViewModel(p, _images));
            Teams.Add(group);
        }
        Raise(nameof(HasPlayers));
    }

    private static string BuildScoreLine(BoardDto board)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(board.Map)) parts.Add(board.Map);
        if (!string.IsNullOrWhiteSpace(board.Mode)) parts.Add(board.Mode);
        if (board.Score is not null && board.State == "INGAME")
        {
            var round = board.Score.Round.HasValue ? $" · Round {board.Score.Round}" : "";
            parts.Add($"{board.Score.Ally} : {board.Score.Enemy}{round}");
        }
        if (board.LockProgress is not null && board.State == "PREGAME")
            parts.Add($"{board.LockProgress.Locked}/{board.LockProgress.Total} locked");
        return string.Join(" · ", parts);
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _api.GetSettingsAsync().ConfigureAwait(true);
            if (settings is null) return;
            var accent = string.IsNullOrWhiteSpace(settings.AccentColor) ? DefaultAccent : settings.AccentColor!;
            AccentHex = accent;
            ApplyAccent(accent);
            Set(ref _startWithWindows, settings.StartWithWindows, nameof(StartWithWindows));
            await LoadBackgroundAsync(settings.Background).ConfigureAwait(true);
        }
        catch { }
    }

    private async Task LoadBackgroundAsync(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _bgKey = "";
            HasBackground = false;
            BackgroundImage = null;
            return;
        }
        if (key == _bgKey && BackgroundImage is not null) return;
        try
        {
            var bytes = await _api.GetBackgroundAsync().ConfigureAwait(true);
            if (bytes is null || bytes.Length == 0) return;
            BackgroundImage = ImageCache.Decode(bytes);
            _bgKey = key;
            HasBackground = BackgroundImage is not null;
        }
        catch { }
    }

    private async Task SaveAccentAsync(string? hex)
    {
        hex = (hex ?? "").Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        if (hex.Length != 7) return;
        try
        {
            _ = ColorConverter.ConvertFromString(hex);
        }
        catch { return; }
        AccentHex = hex;
        ApplyAccent(hex);
        try
        {
            await _api.SaveSettingsAsync(new Dictionary<string, object?> { ["accentColor"] = hex })
                .ConfigureAwait(true);
        }
        catch { }
    }

    private static void ApplyAccent(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var app = Application.Current;
            if (app is null) return;
            app.Resources["AccentBrush"] = new SolidColorBrush(color);
            color.A = 64;
            app.Resources["AccentDimBrush"] = new SolidColorBrush(color);
        }
        catch { }
    }

    private async Task BrowseBackgroundAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose dashboard background",
            Filter = "Images (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp",
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            if (!await _api.UploadBackgroundAsync(dialog.FileName).ConfigureAwait(true))
            {
                MessageBox.Show("Background upload failed. Use PNG, JPG, JPEG or WEBP.",
                    "1 Bullet", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _bgKey = "";
            await LoadSettingsAsync().ConfigureAwait(true);
        }
        catch
        {
            MessageBox.Show("Background upload failed.", "1 Bullet",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task RemoveBackgroundAsync()
    {
        try
        {
            await _api.RemoveBackgroundAsync().ConfigureAwait(true);
            _bgKey = "";
            HasBackground = false;
            BackgroundImage = null;
        }
        catch { }
    }

    public async Task SaveStartupAsync(bool enabled)
    {
        try
        {
            var (ok, message) = await _api.SaveSettingsAsync(
                new Dictionary<string, object?> { ["startWithWindows"] = enabled }).ConfigureAwait(true);
            if (!ok)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(message)
                    ? "Could not update Windows startup." : message,
                    "1 Bullet", MessageBoxButton.OK, MessageBoxImage.Warning);
                Set(ref _startWithWindows, !enabled, nameof(StartWithWindows));
            }
        }
        catch
        {
            Set(ref _startWithWindows, !enabled, nameof(StartWithWindows));
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        var info = await UpdateChecker.CheckAsync(AppVersion).ConfigureAwait(true);
        if (info is null) return;
        _updateUrl = info.DownloadUrl;
        UpdateText = $"Update available — Version {info.Version}";
        HasUpdate = true;
    }

    private void OpenUpdateUrl()
    {
        if (string.IsNullOrWhiteSpace(_updateUrl)) return;
        try
        {
            Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
        }
        catch { }
    }

    private async Task QuitAsync()
    {
        if (MessageBox.Show("Quit 1 Bullet?", "1 Bullet",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await ShutdownAsync().ConfigureAwait(true);
        Application.Current.Shutdown();
    }
}
