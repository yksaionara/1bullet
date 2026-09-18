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
    public SkinViewModel OperatorSkin { get; }
    public SkinViewModel SheriffSkin { get; }
    public Action<PlayerViewModel>? OpenProfile { get; set; }
    public RelayCommand OpenProfileCommand { get; }

    public ImageSource? AgentPortrait { get => _agentPortrait; set => Set(ref _agentPortrait, value); }
    public ImageSource? RankIcon { get => _rankIcon; set => Set(ref _rankIcon, value); }
    public ImageSource? CardBanner { get => _cardBanner; set => Set(ref _cardBanner, value); }
    private ImageSource? _peakIcon;
    public ImageSource? PeakIcon { get => _peakIcon; set => Set(ref _peakIcon, value); }

    private ImageSource? _splash;
    public ImageSource? Splash { get => _splash; set => Set(ref _splash, value); }

    public string DisplayName => string.IsNullOrWhiteSpace(Dto.Name) ? "Player" : Dto.Name;
    public string HiddenTag => Dto.NameHidden ? "  (hidden)" : "";
    public string TitleLine => string.IsNullOrWhiteSpace(Dto.Title) ? "" : $"“{Dto.Title}”";
    public string RankName => string.IsNullOrWhiteSpace(Dto.Rank) ? "Unranked" : Dto.Rank;
    public string RrText => $"{Dto.Rr}RR";
    public string RrDeltaText => !Dto.RrEarned.HasValue || Dto.RrEarned.Value == 0 ? ""
        : Dto.RrEarned.Value > 0 ? $"+{Dto.RrEarned.Value}" : $"{Dto.RrEarned.Value}";
    public string RrDeltaColor => !Dto.RrEarned.HasValue || Dto.RrEarned.Value == 0 ? "#9AA4AB"
        : Dto.RrEarned.Value > 0 ? "#18E5A7" : "#FF4655";
    public double? KdValue => Dto.Kd;
    public string KdText => Dto.Kd.HasValue ? $"{Dto.Kd.Value:0.00}" : "—";
    public string HsText => Dto.HsPct.HasValue ? $"{Dto.HsPct.Value:0}%" : "—";
    public string WinText => Dto.WinRate.HasValue ? $"{Dto.WinRate.Value:0}%" : "—";
    public string LvlText => $"{Dto.Level}";
    public string AgentUpper => (Dto.Agent ?? "—").ToUpperInvariant();
    public string RoleUpper => (Dto.Role ?? "").ToUpperInvariant();
    public string PeakRankName => string.IsNullOrWhiteSpace(Dto.PeakRank) ? "—" : Dto.PeakRank;
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

    private string _teamTag = "";
    private string _teamColor = "#888888";

    public string TeamTag { get => _teamTag; set => Set(ref _teamTag, value); }
    public string TeamColor { get => _teamColor; set => Set(ref _teamColor, value); }

    public PlayerViewModel(PlayerDto dto, ImageCache images)
    {
        Dto = dto;
        _images = images;
        Vandal = MakeSkin("Vandal", dto);
        Phantom = MakeSkin("Phantom", dto);
        OperatorSkin = MakeSkin("Operator", dto);
        SheriffSkin = MakeSkin("Sheriff", dto);
        OpenProfileCommand = new RelayCommand(_ => OpenProfile?.Invoke(this));
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
            PeakIcon = await _images.GetAsync(Dto.PeakIcon).ConfigureAwait(true);
            var banner = await _images.GetAsync(Dto.PlayerCard).ConfigureAwait(true);
            CardBanner = banner;
            Splash = await _images.GetAsync(Dto.AgentArt).ConfigureAwait(true);
            Vandal.Icon = await _images.GetAsync(
                Dto.Weapons.FirstOrDefault(w => w.Weapon == "Vandal")?.Skin?.Icon).ConfigureAwait(true);
            Phantom.Icon = await _images.GetAsync(
                Dto.Weapons.FirstOrDefault(w => w.Weapon == "Phantom")?.Skin?.Icon).ConfigureAwait(true);
        }
        catch { }
    }
}



public sealed class PartyPillViewModel
{
    public PartyPillViewModel(PartyDto party)
    {
        Text = $"PARTY {party.Number} · {party.Size}-STACK";
        Color = string.IsNullOrWhiteSpace(party.Color) ? "#888888" : party.Color;
    }

    public string Text { get; }
    public string Color { get; }
}

public sealed class TeamPanelViewModel : ObservableObject
{
    private ImageSource? _avgRankIcon;

    public TeamPanelViewModel(string title, int count, bool isSelfTeam)
    {
        Title = title;
        Count = count;
        IsSelfTeam = isSelfTeam;
    }

    public string Title { get; }
    public int Count { get; }
    public bool IsSelfTeam { get; }
    public ObservableCollection<PlayerViewModel> Players { get; } = new();

    public string AvgRank { get; private set; } = "";
    public string AvgRankColor { get; private set; } = "#9AA4AB";
    public string AvgKd { get; private set; } = "—";
    public string AvgWr { get; private set; } = "—";
    public string Smurfs { get; private set; } = "0";
    public string SmurfColor { get; private set; } = "#9AA4AB";
    public bool HasAverages { get; private set; }
    public ImageSource? AvgRankIcon { get => _avgRankIcon; set => Set(ref _avgRankIcon, value); }

    public void ApplyStats(TeamStatDto? stats)
    {
        if (stats is null || string.IsNullOrWhiteSpace(stats.AvgRank) || stats.AvgRank == "Unranked")
        {
            HasAverages = false;
            return;
        }
        HasAverages = true;
        AvgRank = stats.AvgRank;
        AvgRankColor = string.IsNullOrWhiteSpace(stats.RankColor) ? "#9AA4AB" : stats.RankColor;
        AvgKd = stats.AvgKd.HasValue ? $"{stats.AvgKd.Value:0.00}" : "—";
        AvgWr = stats.AvgWinRate.HasValue ? $"{stats.AvgWinRate.Value:0}%" : "—";
        Smurfs = $"{stats.SmurfCount}";
        SmurfColor = stats.SmurfCount > 0 ? "#FF4655" : "#9AA4AB";
        Raise(nameof(AvgRank));
        Raise(nameof(AvgRankColor));
        Raise(nameof(AvgKd));
        Raise(nameof(AvgWr));
        Raise(nameof(Smurfs));
        Raise(nameof(SmurfColor));
        Raise(nameof(HasAverages));
    }

    public async Task LoadIconAsync(ImageCache images, string? url)
    {
        try { AvgRankIcon = await images.GetAsync(url).ConfigureAwait(true); }
        catch { }
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

    public ObservableCollection<TeamPanelViewModel> TeamPanels { get; } = new();
    public ObservableCollection<PartyPillViewModel> PartyPills { get; } = new();
    public IReadOnlyList<string> AccentPresets { get; } =
        new[] { "#FF4655", "#18E5A7", "#9ADEFF", "#FFB454", "#D864C7", "#ECE8E1" };

    public string AppVersion { get; } =
        (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0").Split('+', 2)[0].Trim();

    public string VersionText => $"v{AppVersion}";
    public BackendManager Backend => _backend;

    public string NoticeText { get => _noticeText; set => Set(ref _noticeText, value); }
    public bool HasNotice { get => _hasNotice; set => Set(ref _hasNotice, value); }
    public string EmptyMessage { get => _emptyMessage; set => Set(ref _emptyMessage, value); }
    public bool HasPlayers => TeamPanels.Sum(t => t.Players.Count) > 0;

    private string _matchStateText = "";
    private string _matchStateColor = "#888888";
    private string _mapName = "";
    private string _modeName = "";
    private string _scoreText = "";
    private string _sideText = "";
    private string _sideColor = "#18E5A7";
    private int? _winProb;

    public string MatchStateText { get => _matchStateText; set => Set(ref _matchStateText, value); }
    public string MatchStateColor { get => _matchStateColor; set => Set(ref _matchStateColor, value); }
    public string MapName { get => _mapName; set => Set(ref _mapName, value); }
    public string ModeName { get => _modeName; set => Set(ref _modeName, value); }
    public string ScoreText { get => _scoreText; set => Set(ref _scoreText, value); }
    public string SideText { get => _sideText; set => Set(ref _sideText, value); }
    public string SideColor { get => _sideColor; set => Set(ref _sideColor, value); }
    public int? WinProb { get => _winProb; set => Set(ref _winProb, value); }
    public bool HasWinProb => WinProb.HasValue;
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
        try
        {
            if (!await _backend.StartAsync(progress).ConfigureAwait(true)) return;
        }
        catch (FileNotFoundException)
        {
            _backend.Fail("Backend component missing (1bullet-backend.exe). " +
                "Install 1 Bullet using 1 Bullet Setup.exe and launch it from the Start Menu — " +
                "the downloaded 1bullet.exe cannot run on its own.");
            return;
        }
        catch (Exception ex)
        {
            _backend.Fail($"Couldn't start backend: {ex.Message}");
            return;
        }
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
                MatchStateText = "RECONNECTING…";
            return;
        }
        _boardFailures = 0;
        var stateLabel = string.IsNullOrWhiteSpace(board.StateLabel) ? board.State : board.StateLabel;
        (MatchStateText, MatchStateColor) = board.State switch
        {
            "INGAME" => ($"● LIVE · {stateLabel.ToUpperInvariant()}", "#FF4655"),
            "PREGAME" => ($"◆ {stateLabel.ToUpperInvariant()}", "#FFB454"),
            "MENUS" => ($"◆ {stateLabel.ToUpperInvariant()}", "#18E5A7"),
            _ => (stateLabel.ToUpperInvariant(), "#888888"),
        };
        MapName = (board.Map ?? "").ToUpperInvariant();
        ModeName = (board.Mode ?? "").ToUpperInvariant();
        ScoreText = board.Score is not null && board.State == "INGAME"
            ? board.Score.Round.HasValue
                ? $"{board.Score.Ally} : {board.Score.Enemy}  RD {board.Score.Round}"
                : $"{board.Score.Ally} : {board.Score.Enemy}"
            : board.LockProgress is not null && board.State == "PREGAME"
            ? $"{board.LockProgress.Locked}/{board.LockProgress.Total} LOCKED"
            : "";
        SideText = (board.Side ?? "").ToUpperInvariant();
        SideColor = string.Equals(board.Side, "Attacker", StringComparison.OrdinalIgnoreCase)
            ? "#FF4655" : "#18E5A7";
        WinProb = board.State == "INGAME" ? board.WinProb : null;
        Raise(nameof(HasWinProb));
        HasNotice = !string.IsNullOrWhiteSpace(board.Notice?.Message);
        NoticeText = board.Notice?.Message ?? "";
        EmptyMessage = !string.IsNullOrWhiteSpace(board.Notice?.Message) ? board.Notice!.Message
            : !string.IsNullOrWhiteSpace(board.Error) ? board.Error
            : "Open VALORANT — lobby, Agent Select or a match.";

        PartyPills.Clear();
        foreach (var party in board.Parties.OrderBy(p => p.Number))
            PartyPills.Add(new PartyPillViewModel(party));

        TeamPanels.Clear();
        var orderedTeams = board.Teams.OrderByDescending(kv => kv.Key == board.SelfTeam).ToList();
        if (orderedTeams.Count == 0 && board.Players.Count > 0)
            orderedTeams = new List<KeyValuePair<string, List<PlayerDto>>>
                { new("Blue", board.Players) };
        foreach (var (team, players) in orderedTeams)
        {
            var isSelf = team == board.SelfTeam;
            var title = orderedTeams.Count == 1 ? "PLAYERS" : isSelf ? "YOUR TEAM" : "ENEMY TEAM";
            var panel = new TeamPanelViewModel(title, players.Count, isSelf);
            board.TeamStats.TryGetValue(team, out var stats);
            panel.ApplyStats(stats);
            foreach (var p in players)
            {
                var vm = new PlayerViewModel(p, _images)
                {
                    OpenProfile = OpenProfileFor,
                    TeamTag = orderedTeams.Count == 1 ? "" : isSelf ? "Your team" : "Enemy team",
                    TeamColor = isSelf ? "#18E5A7" : "#FF4655",
                };
                panel.Players.Add(vm);
            }
            _ = panel.LoadIconAsync(_images,
                orderedTeams.Count == 1 ? null : stats?.RankIcon);
            TeamPanels.Add(panel);
        }
        Raise(nameof(HasPlayers));
    }

    private void OpenProfileFor(PlayerViewModel player)
    {
        try
        {
            var window = new ProfileWindow(_api, _images, player.Dto.Puuid, player.DisplayName)
            {
                Owner = Application.Current.MainWindow,
            };
            window.Show();
        }
        catch { }
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
