using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
    public string TitleLine => string.IsNullOrWhiteSpace(Dto.Title) ? "" : $"“{Dto.Title}”";
    public string RankName => string.IsNullOrWhiteSpace(Dto.Rank) ? "Unranked" : Dto.Rank;
    public string RrText => $"{Dto.Rr} RR";
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

    // Main-card artwork only: agent portrait, rank icons, Vandal + Phantom.
    // Splashes, banners and other weapon slots stay out of the hot refresh path.
    // ImageCache deduplicates concurrent requests, so unchanged URLs cost nothing.
    private async Task LoadImagesAsync()
    {
        try
        {
            AgentPortrait = await _images.GetAsync(Dto.AgentPortrait).ConfigureAwait(true);
            RankIcon = await _images.GetAsync(Dto.RankIcon).ConfigureAwait(true);
            PeakIcon = await _images.GetAsync(Dto.PeakIcon).ConfigureAwait(true);
            Vandal.Icon = await _images.GetAsync(
                Dto.Weapons.FirstOrDefault(w =>
                    string.Equals(w.Weapon, "Vandal", StringComparison.OrdinalIgnoreCase))?.Skin?.Icon)
                .ConfigureAwait(true);
            Phantom.Icon = await _images.GetAsync(
                Dto.Weapons.FirstOrDefault(w =>
                    string.Equals(w.Weapon, "Phantom", StringComparison.OrdinalIgnoreCase))?.Skin?.Icon)
                .ConfigureAwait(true);
        }
        catch { }
    }
}

public sealed class InstalockAgentViewModel : ObservableObject
{
    private ImageSource? _portrait;
    private bool _isSelected;

    public InstalockAgentViewModel(AgentDto agent)
    {
        Name = agent.Name;
        Role = agent.Role;
        PortraitUrl = agent.Portrait;
    }

    public string Name { get; }
    public string Role { get; }
    public string? PortraitUrl { get; }
    public ImageSource? Portrait { get => _portrait; set => Set(ref _portrait, value); }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
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

public sealed class QueueOptionViewModel : ObservableObject
{
    private bool _isSelected;
    private bool _isEligible = true;

    public QueueOptionViewModel(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
    public bool IsEligible { get => _isEligible; set => Set(ref _isEligible, value); }
}

public sealed class PerformancePointViewModel : ObservableObject
{
    private readonly ImageCache _images;
    private ImageSource? _agentPortrait;

    public PerformancePointViewModel(PerformancePointDto dto, ImageCache images, string chartMode = "RR change")
    {
        Dto = dto;
        _images = images;
        ChartValueText = chartMode == "Rank journey" && dto.Rr.HasValue
            ? $"{dto.Rr} RR" : DeltaText;
        OpenMatchCommand = new RelayCommand(_ => OpenMatch?.Invoke(this));
    }

    public PerformancePointDto Dto { get; }
    public Action<PerformancePointViewModel>? OpenMatch { get; set; }
    public RelayCommand OpenMatchCommand { get; }
    public ImageSource? AgentPortrait { get => _agentPortrait; set => Set(ref _agentPortrait, value); }
    public string Map => string.IsNullOrWhiteSpace(Dto.Map) ? "Unknown" : Dto.Map;
    public string Mode => string.IsNullOrWhiteSpace(Dto.Mode) ? "Competitive" : Dto.Mode.ToUpperInvariant();
    public string Agent => string.IsNullOrWhiteSpace(Dto.Agent) ? "Unknown agent" : Dto.Agent;
    public string Result => string.IsNullOrWhiteSpace(Dto.Result) ? "PENDING" : Dto.Result.ToUpperInvariant();
    public string ResultColor => Dto.Result == "Victory" ? "#36D399" : Dto.Result == "Defeat" ? "#FF4655" : "#8A8A91";
    public string DeltaText => Dto.Delta.HasValue ? $"{(Dto.Delta.Value >= 0 ? "+" : "")}{Dto.Delta.Value} RR" : "--";
    public string ChartValueText { get; }
    public string ScoreText => Dto.Kills.HasValue
        ? $"{Dto.Kills}/{Dto.Deaths ?? 0}/{Dto.Assists ?? 0}"
        : "Stats loading";
    public string DetailText => Dto.Acs.HasValue
        ? $"{Dto.Acs} ACS  ·  {(Dto.HsPct.HasValue ? $"{Dto.HsPct.Value:0}% HS" : "-- HS")}" : "";
    public string DateText => Dto.Ts.HasValue
        ? DateTimeOffset.FromUnixTimeSeconds(Dto.Ts.Value).LocalDateTime.ToString("MMM d · h:mm tt", CultureInfo.InvariantCulture)
        : "";

    public async Task EnsureImageAsync()
    {
        if (AgentPortrait is not null) return;
        try { AgentPortrait = await _images.GetAsync(Dto.AgentPortrait).ConfigureAwait(true); }
        catch { }
    }
}

public sealed class SessionViewModel
{
    public SessionViewModel(PerformanceSessionDto dto, bool active)
    {
        Id = dto.Id;
        Status = active ? "ACTIVE SESSION" : "COMPLETED";
        DateText = dto.StartedAt > 0
            ? DateTimeOffset.FromUnixTimeSeconds(dto.StartedAt).LocalDateTime.ToString("MMM d · h:mm tt", CultureInfo.InvariantCulture)
            : "Unknown start";
        Matches = $"{dto.Summary.Matches} matches";
        Record = $"{dto.Summary.Wins}W  {dto.Summary.Losses}L";
        Net = $"{(dto.Summary.Net >= 0 ? "+" : "")}{dto.Summary.Net} RR";
        NetColor = dto.Summary.Net >= 0 ? "#36D399" : "#FF4655";
    }

    public string Id { get; }
    public string Status { get; }
    public string DateText { get; }
    public string Matches { get; }
    public string Record { get; }
    public string Net { get; }
    public string NetColor { get; }
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
    public string Subtitle { get; set; } = "";

    public string AvgRank { get; private set; } = "";
    public string AvgRankColor { get; private set; } = "#8C8C94";
    public string AvgKd { get; private set; } = "—";
    public string AvgWr { get; private set; } = "—";
    public string Smurfs { get; private set; } = "0";
    public string SmurfColor { get; private set; } = "#8C8C94";
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
    private bool _refreshing;
    private bool _performanceFollowUpScheduled;
    private int _boardFailures;
    private string _bgKey = "";

    private string _noticeText = "";
    private bool _hasNotice;
    private string _emptyMessage = "Launch VALORANT and enter the lobby, Agent Select, or a match to view live data.";
    private string _accentHex = DefaultAccent;
    private bool _startWithWindows;
    private bool _hasBackground;
    private ImageSource? _backgroundImage;
    private bool _hasUpdate;
    private string _updateText = "";
    private string? _updateUrl;
    private string _backendVersion = "";
    private DateTime _lastPerformanceRefresh = DateTime.MinValue;
    private DateTime _lastQueueRefresh = DateTime.MinValue;
    private DateTime _lastOfflineRefresh = DateTime.MinValue;
    private DateTime _lastInstalockRefresh = DateTime.MinValue;
    private string _gameState = "OFFLINE";
    private string _rosterFingerprint = "";
    private string _partyFingerprint = "";
    private int _requestCount;
    private bool _autoRefresh = true;
    private string _activePanel = "";
    private string _queueStatusText = "No lobby";
    private string _queueHint = "Open VALORANT to enable matchmaking controls.";
    private string _queueActionText = "Start Queue";
    private string _clientBadgeText = "Starting…";
    private string _clientBadgeColor = "#8C8C94";
    private bool _hasMatchContext;
    private bool _canControlQueue;
    private bool _isInQueue;
    private bool _offlineEnabled;
    private bool _offlineRunning;
    private string _offlineButtonText = "Presence";
    private string _controlMessage = "";
    private string _matchActionMessage = "";
    private string _selectedAgent = "Jett";
    private string _instalockMode = "lock";
    private double _instalockDelay = 2;
    private bool _instalockRunning;
    private string _instalockStatus = "Choose an agent and arm instalock.";
    private string _instalockButtonText = "Instalock";
    private string _updateStatusText = "";
    private bool _hasParties;
    private bool _hasSessions;
    private DateTime _lastBoardAt = DateTime.MinValue;
    private ImageSource? _currentRankIcon;
    private string _performanceTab = "Overview";
    private string _performanceScope = "Act";
    private string _chartMode = "RR change";
    private int _matchLimit = 20;
    private string _performanceAccount = "Competitive history";
    private string _performanceEmptyText = "Play a competitive match to begin tracking rank progress.";
    private string _netRrText = "—";
    private string _recordText = "No matches yet";
    private string _currentRankText = "Unranked";
    private string _currentRrText = "";
    private readonly List<PerformancePointDto> _performancePoints = new();

    public ObservableCollection<TeamPanelViewModel> TeamPanels { get; } = new();
    public ObservableCollection<PartyPillViewModel> PartyPills { get; } = new();
    public ObservableCollection<QueueOptionViewModel> QueueOptions { get; } = new();
    public ObservableCollection<InstalockAgentViewModel> Agents { get; } = new();
    public ObservableCollection<PerformancePointViewModel> PerformanceMatches { get; } = new();
    public ObservableCollection<SessionViewModel> PerformanceSessions { get; } = new();
    public IReadOnlyList<string> AccentPresets { get; } =
        new[] { "#FF4655", "#18E5A7", "#9ADEFF", "#FFB454", "#D864C7", "#ECE8E1" };

    // Raw informational version may carry commit/build metadata
    // (e.g. "2.2.5+e9ad03f"); never shown directly in the release UI.
    public string FullAppVersion { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";

    // Clean semantic version for all user-facing display and update checks.
    public string AppVersion => CleanVersion(FullAppVersion);

    public string VersionText => $"v{AppVersion}";

    private static string CleanVersion(string version)
    {
        version = (version ?? "").Trim();
        var plus = version.IndexOf('+');
        if (plus >= 0) version = version[..plus];
        version = version.TrimStart('v', 'V');
        return string.IsNullOrWhiteSpace(version) ? "0.0.0" : version;
    }
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
    public bool AutoRefresh { get => _autoRefresh; set => Set(ref _autoRefresh, value); }
    public string AutoRefreshText => AutoRefresh ? "● Live" : "○ Paused";
    public string LastUpdatedText { get; private set; } = "Waiting for VALORANT";
    public string RequestText => $"Updates: {_requestCount}";
    public string MatchActionMessage { get => _matchActionMessage; set => Set(ref _matchActionMessage, value); }
    public bool HasMatchActionMessage => !string.IsNullOrWhiteSpace(MatchActionMessage);
    public string ActivePanel
    {
        get => _activePanel;
        set
        {
            if (!Set(ref _activePanel, value)) return;
            Raise(nameof(IsPanelOpen));
            Raise(nameof(IsDashboardEnabled));
            Raise(nameof(IsSettingsOpen));
            Raise(nameof(IsOfflineOpen));
            Raise(nameof(IsInstalockOpen));
            Raise(nameof(IsNotificationsOpen));
        }
    }
    public bool IsPanelOpen => ActivePanel != "";
    public bool IsDashboardEnabled => !IsPanelOpen;
    public bool IsSettingsOpen => ActivePanel == "Settings";
    public bool IsControlsOpen => IsSettingsOpen;
    public bool IsOfflineOpen => ActivePanel == "Offline";
    public bool IsInstalockOpen => ActivePanel == "Instalock";
    public bool IsNotificationsOpen => ActivePanel == "Notifications";
    public string QueueStatusText { get => _queueStatusText; set => Set(ref _queueStatusText, value); }
    public string QueueHint { get => _queueHint; set => Set(ref _queueHint, value); }
    public string QueueActionText { get => _queueActionText; set => Set(ref _queueActionText, value); }
    public string ClientBadgeText { get => _clientBadgeText; set => Set(ref _clientBadgeText, value); }
    public string ClientBadgeColor { get => _clientBadgeColor; set => Set(ref _clientBadgeColor, value); }
    public bool HasMatchContext { get => _hasMatchContext; set => Set(ref _hasMatchContext, value); }
    public bool CanControlQueue { get => _canControlQueue; set => Set(ref _canControlQueue, value); }
    public bool IsInQueue { get => _isInQueue; set => Set(ref _isInQueue, value); }
    public bool OfflineEnabled { get => _offlineEnabled; set => Set(ref _offlineEnabled, value); }
    public string OfflineButtonText { get => _offlineButtonText; set => Set(ref _offlineButtonText, value); }
    public string InstalockButtonText { get => _instalockButtonText; set => Set(ref _instalockButtonText, value); }
    public string UpdateStatusText { get => _updateStatusText; set => Set(ref _updateStatusText, value); }
    public bool HasUpdateStatus => !string.IsNullOrWhiteSpace(UpdateStatusText);
    public bool HasParties { get => _hasParties; set => Set(ref _hasParties, value); }
    public bool HasSessions { get => _hasSessions; set => Set(ref _hasSessions, value); }
    public ImageSource? CurrentRankIcon { get => _currentRankIcon; set => Set(ref _currentRankIcon, value); }
    public bool HasBackendFailure => Backend.State == BackendManager.BackendState.Failed;
    public string ControlMessage { get => _controlMessage; set => Set(ref _controlMessage, value); }
    public string SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (!Set(ref _selectedAgent, value)) return;
            foreach (var agent in Agents) agent.IsSelected = agent.Name == value;
            SyncInstalockButton();
        }
    }
    public string InstalockMode { get => _instalockMode; set => Set(ref _instalockMode, value); }
    public double InstalockDelay { get => _instalockDelay; set => Set(ref _instalockDelay, value); }
    public bool InstalockRunning
    {
        get => _instalockRunning;
        set
        {
            if (Set(ref _instalockRunning, value)) Raise(nameof(InstalockPrimaryText));
        }
    }
    public string InstalockStatus { get => _instalockStatus; set => Set(ref _instalockStatus, value); }
    public string InstalockPrimaryText => InstalockRunning ? "STOP AUTO-LOCK" : "START AUTO-LOCK";
    public string PerformanceTab
    {
        get => _performanceTab;
        set
        {
            if (!Set(ref _performanceTab, value)) return;
            Raise(nameof(IsOverviewTab));
            Raise(nameof(IsMatchesTab));
            Raise(nameof(IsSessionsTab));
            if (value == "Matches")
                foreach (var match in PerformanceMatches) _ = match.EnsureImageAsync();
        }
    }
    public bool IsOverviewTab => PerformanceTab == "Overview";
    public bool IsMatchesTab => PerformanceTab == "Matches";
    public bool IsSessionsTab => PerformanceTab == "Sessions";
    public string PerformanceScope { get => _performanceScope; set { if (Set(ref _performanceScope, value)) ApplyPerformanceFilter(); } }
    public string ChartMode { get => _chartMode; set { if (Set(ref _chartMode, value)) ApplyPerformanceFilter(); } }
    public int MatchLimit { get => _matchLimit; set { if (Set(ref _matchLimit, value)) ApplyPerformanceFilter(); } }
    public string PerformanceAccount { get => _performanceAccount; set => Set(ref _performanceAccount, value); }
    public string PerformanceEmptyText { get => _performanceEmptyText; set => Set(ref _performanceEmptyText, value); }
    public bool HasPerformance => PerformanceMatches.Count > 0;
    public string MatchCountText => $"{PerformanceMatches.Count} matches shown";
    public string NetRrText { get => _netRrText; set => Set(ref _netRrText, value); }
    public string RecordText { get => _recordText; set => Set(ref _recordText, value); }
    public string CurrentRankText { get => _currentRankText; set => Set(ref _currentRankText, value); }
    public string CurrentRrText { get => _currentRrText; set => Set(ref _currentRrText, value); }

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
    public RelayCommand ShowPanelCommand { get; }
    public RelayCommand ClosePanelCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ToggleAutoRefreshCommand { get; }
    public RelayCommand SelectQueueCommand { get; }
    public RelayCommand QueueActionCommand { get; }
    public RelayCommand SetOfflineStatusCommand { get; }
    public RelayCommand CheckSideCommand { get; }
    public RelayCommand ShowTeamSideCommand { get; }
    public RelayCommand DodgeCommand { get; }
    public RelayCommand StartInstalockCommand { get; }
    public RelayCommand StopInstalockCommand { get; }
    public RelayCommand ToggleInstalockCommand { get; }
    public RelayCommand SelectInstalockAgentCommand { get; }
    public RelayCommand SetInstalockModeCommand { get; }
    public RelayCommand SelectPerformanceTabCommand { get; }
    public RelayCommand SetMatchLimitCommand { get; }
    public RelayCommand SetPerformanceScopeCommand { get; }
    public RelayCommand SetChartModeCommand { get; }
    public RelayCommand SessionActionCommand { get; }
    public RelayCommand OpenLinkCommand { get; }

    public MainViewModel(ApiClient api, BackendManager backend, ImageCache images)
    {
        _api = api;
        _backend = backend;
        _images = images;
        _backend.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(BackendManager.StatusText)
                or nameof(BackendManager.State))
            {
                Raise(nameof(Backend));
                Raise(nameof(HasBackendFailure));
            }
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
        ShowPanelCommand = new RelayCommand(p =>
        {
            var name = p as string ?? "";
            if (name == "Controls") name = "Settings";
            ActivePanel = name;
            if (name == "Instalock") _ = LoadAgentsAsync();
        });
        ClosePanelCommand = new RelayCommand(_ => ActivePanel = "");
        RefreshCommand = new RelayCommand(_ => _ = RefreshAllAsync(forcePerformance: true));
        ToggleAutoRefreshCommand = new RelayCommand(_ =>
        {
            AutoRefresh = !AutoRefresh;
            Raise(nameof(AutoRefreshText));
        });
        SelectQueueCommand = new RelayCommand(p =>
        {
            if (p is QueueOptionViewModel option) _ = SelectQueueAsync(option);
        });
        QueueActionCommand = new RelayCommand(_ => _ = RunQueueActionAsync());
        SetOfflineStatusCommand = new RelayCommand(p =>
        {
            if (p is string status) _ = SetOfflineStatusAsync(status);
        });
        CheckSideCommand = new RelayCommand(_ => ShowTeamSide());
        ShowTeamSideCommand = new RelayCommand(_ => ShowTeamSide());
        DodgeCommand = new RelayCommand(_ => _ = DodgeAsync());
        StartInstalockCommand = new RelayCommand(_ => _ = StartInstalockAsync());
        StopInstalockCommand = new RelayCommand(_ => _ = StopInstalockAsync());
        ToggleInstalockCommand = new RelayCommand(_ => _ = ToggleInstalockAsync());
        SelectInstalockAgentCommand = new RelayCommand(p =>
        {
            if (p is InstalockAgentViewModel agent) SelectedAgent = agent.Name;
        });
        SetInstalockModeCommand = new RelayCommand(p =>
        {
            if (p is string mode && mode is "lock" or "select") InstalockMode = mode;
        });
        SelectPerformanceTabCommand = new RelayCommand(p => PerformanceTab = p as string ?? "Overview");
        SetMatchLimitCommand = new RelayCommand(p =>
        {
            if (int.TryParse(p?.ToString(), out var value)) MatchLimit = value;
        });
        SetPerformanceScopeCommand = new RelayCommand(p => PerformanceScope = p as string ?? "Act");
        SetChartModeCommand = new RelayCommand(p => ChartMode = p as string ?? "RR change");
        SessionActionCommand = new RelayCommand(p =>
        {
            if (p is string action) _ = RunSessionActionAsync(action);
        });
        OpenLinkCommand = new RelayCommand(p =>
        {
            if (p is not string url || string.IsNullOrWhiteSpace(url)) return;
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        });

        foreach (var (id, name) in new[]
        {
            ("unrated", "UNRATED"), ("competitive", "COMPETITIVE"),
            ("fortcollins", "RETAKE"), ("skirmish2v2", "SKIRMISH 2V2"),
            ("swiftplay", "SWIFTPLAY"), ("deathmatch", "DEATHMATCH"),
            ("hurm", "TEAM DEATHMATCH"), ("spikerush", "SPIKE RUSH"),
            ("ggteam", "ESCALATION"),
        }) QueueOptions.Add(new QueueOptionViewModel(id, name));
        QueueOptions.First(q => q.Id == "competitive").IsSelected = true;

        _boardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _boardTimer.Tick += async (_, _) =>
        {
            UpdateFreshnessText();
            if (AutoRefresh) await RefreshAllAsync().ConfigureAwait(false);
        };
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
        await LoadAgentsAsync().ConfigureAwait(true);
        await RefreshAllAsync().ConfigureAwait(true);
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

    private async Task RefreshAllAsync(bool forcePerformance = false)
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            await RefreshBoardAsync().ConfigureAwait(true);
            var now = DateTime.UtcNow;
            var queueInterval = _gameState == "MENUS" || IsInQueue
                ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(15);
            if (forcePerformance || now - _lastQueueRefresh >= queueInterval)
            {
                _lastQueueRefresh = now;
                await RefreshQueueAsync().ConfigureAwait(true);
            }
            if (forcePerformance || IsOfflineOpen || now - _lastOfflineRefresh >= TimeSpan.FromSeconds(30))
            {
                _lastOfflineRefresh = now;
                await RefreshOfflineAsync().ConfigureAwait(true);
            }
            var instalockInterval = InstalockRunning || IsInstalockOpen || _gameState == "PREGAME"
                ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(30);
            if (forcePerformance || now - _lastInstalockRefresh >= instalockInterval)
            {
                _lastInstalockRefresh = now;
                await RefreshInstalockAsync().ConfigureAwait(true);
            }
            if (forcePerformance || _gameState == "MENUS" &&
                now - _lastPerformanceRefresh >= TimeSpan.FromSeconds(90))
                await RefreshPerformanceAsync().ConfigureAwait(true);
        }
        finally { _refreshing = false; }
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
            {
                MatchStateText = "RECONNECTING…";
                ClientBadgeText = "Reconnecting…";
                ClientBadgeColor = "#FFB454";
                UpdateFreshnessText();
            }
            return;
        }
        _boardFailures = 0;
        var returnedToMenus = _gameState != "MENUS" && board.State == "MENUS";
        _gameState = board.State;
        if (returnedToMenus) _lastPerformanceRefresh = DateTime.MinValue;
        _requestCount++;
        _lastBoardAt = DateTime.UtcNow;
        LastUpdatedText = "Updated just now";
        Raise(nameof(LastUpdatedText));
        Raise(nameof(RequestText));
        var stateLabel = string.IsNullOrWhiteSpace(board.StateLabel) ? board.State : board.StateLabel;
        (MatchStateText, MatchStateColor) = board.State switch
        {
            "INGAME" => ($"● LIVE · {stateLabel.ToUpperInvariant()}", "#FF4655"),
            "PREGAME" => ($"◆ {stateLabel.ToUpperInvariant()}", "#FFB454"),
            "MENUS" => ($"◆ {stateLabel.ToUpperInvariant()}", "#36D399"),
            _ => (stateLabel.ToUpperInvariant(), "#8C8C94"),
        };
        (ClientBadgeText, ClientBadgeColor) = board.State switch
        {
            "INGAME" => ("In Match", "#36D399"),
            "PREGAME" => ("Agent Select", "#FFB454"),
            "MENUS" => ("VALORANT connected", "#36D399"),
            _ => ("VALORANT closed", "#8C8C94"),
        };
        HasMatchContext = board.State is "INGAME" or "PREGAME";
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
            : "Launch VALORANT and enter the lobby, Agent Select, or a match to view live data.";

        var partyFingerprint = string.Join("|", board.Parties.OrderBy(p => p.Number)
            .Select(p => $"{p.Id}:{p.Number}:{p.Size}:{p.Color}"));
        if (partyFingerprint != _partyFingerprint)
        {
            _partyFingerprint = partyFingerprint;
        PartyPills.Clear();
        foreach (var party in board.Parties.OrderBy(p => p.Number))
            PartyPills.Add(new PartyPillViewModel(party));
        HasParties = PartyPills.Count > 0;
        }

        var orderedTeams = board.Teams.OrderByDescending(kv => kv.Key == board.SelfTeam).ToList();
        if (orderedTeams.Count == 0 && board.Players.Count > 0)
            orderedTeams = new List<KeyValuePair<string, List<PlayerDto>>>
                { new("Blue", board.Players) };
        var rosterFingerprint = RosterFingerprint(board, orderedTeams);
        if (rosterFingerprint == _rosterFingerprint) return;
        _rosterFingerprint = rosterFingerprint;
        TeamPanels.Clear();
        foreach (var (team, players) in orderedTeams)
        {
            var isSelf = team == board.SelfTeam;
            var title = orderedTeams.Count == 1
                ? board.State switch
                {
                    "MENUS" => "YOUR LOBBY",
                    "PREGAME" => "AGENT SELECT",
                    "INGAME" => "LIVE MATCH",
                    _ => "PLAYERS",
                }
                : isSelf ? "YOUR TEAM" : "ENEMY TEAM";
            var panel = new TeamPanelViewModel(title, players.Count, isSelf)
            {
                Subtitle = orderedTeams.Count == 1
                    ? LobbySubtitle(players.Count, board.Parties.Count)
                    : $"{players.Count} players",
            };
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
            _ = panel.LoadIconAsync(_images, stats?.RankIcon);
            TeamPanels.Add(panel);
        }
        Raise(nameof(HasPlayers));
    }

    private static string LobbySubtitle(int players, int parties)
    {
        if (players <= 1) return "1 player · Solo";
        if (parties <= 0) return $"{players} players";
        return $"{players} players · {parties} part{(parties == 1 ? "y" : "ies")}";
    }

    private void UpdateFreshnessText()
    {
        if (_boardFailures > 0)
        {
            LastUpdatedText = "Reconnecting…";
        }
        else if (_lastBoardAt == DateTime.MinValue)
        {
            LastUpdatedText = "Waiting for VALORANT";
        }
        else
        {
            var age = DateTime.UtcNow - _lastBoardAt;
            LastUpdatedText = age < TimeSpan.FromSeconds(10) ? "Updated just now"
                : age < TimeSpan.FromMinutes(1) ? $"Updated {(int)age.TotalSeconds}s ago"
                : $"Updated {(int)age.TotalMinutes}m ago";
        }
        Raise(nameof(LastUpdatedText));
    }

    private static string RosterFingerprint(
        BoardDto board, List<KeyValuePair<string, List<PlayerDto>>> teams)
    {
        var players = teams.SelectMany(team => team.Value)
            .OrderBy(player => player.Puuid, StringComparer.Ordinal)
            .Select(player => string.Join(":", new object?[]
            {
                player.Puuid, player.Name, player.NameHidden, player.Team, player.IsSelf, player.Title,
                player.Agent, player.Role, player.Rank, player.Rr, player.RrEarned,
                player.PeakRank, player.PeakColor, player.PeakAct, player.Kd, player.HsPct, player.WinRate,
                player.Level, player.Party?.Id, player.Party?.Number, player.Party?.Color, player.Smurf,
                player.RankIcon, player.PeakIcon,
                player.Encounter?.WithCount, player.Encounter?.AgainstCount,
            }));
        var stats = board.TeamStats.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{pair.Value.AvgRank}:{pair.Value.AvgKd}:" +
                            $"{pair.Value.AvgWinRate}:{pair.Value.SmurfCount}:{pair.Value.RankColor}:{pair.Value.RankIcon}");
        return $"{board.State}:{board.SelfTeam}|{string.Join("|", players)}#" +
               string.Join("|", stats);
    }

    private async Task RefreshQueueAsync()
    {
        try
        {
            var queue = await _api.GetQueueAsync().ConfigureAwait(true);
            if (queue is null || !queue.Available)
            {
                QueueStatusText = _gameState == "OFFLINE" ? "No lobby" : "Waiting for lobby";
                QueueHint = queue?.Message ?? "Open VALORANT to enable matchmaking controls.";
                CanControlQueue = false;
                return;
            }
            ApplyQueue(queue);
        }
        catch
        {
            QueueStatusText = _gameState == "OFFLINE" ? "No lobby" : "Waiting for lobby";
            CanControlQueue = false;
        }
    }

    private void ApplyQueue(QueueDto queue, string? responseMessage = null)
    {
        var selected = QueueOptions.FirstOrDefault(q => q.Id == queue.QueueId)
            ?? QueueOptions.First(q => q.Id == "competitive");
        var eligible = queue.Eligible.Select(q => q.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var option in QueueOptions)
        {
            option.IsSelected = option == selected;
            option.IsEligible = eligible.Count == 0 || eligible.Contains(option.Id);
        }
        IsInQueue = queue.InQueue;
        QueueActionText = queue.InQueue ? "Stop Queue" : "Start Queue";
        QueueStatusText = queue.InQueue ? "In queue" : "In lobby";
        if (_gameState is "INGAME" or "PREGAME") QueueStatusText = "In match";
        QueueHint = !string.IsNullOrWhiteSpace(responseMessage) ? responseMessage
            : !queue.IsOwner ? "Party owner controls the queue"
            : !queue.AllReady ? "Waiting for every party member to be ready"
            : queue.InQueue && queue.QueueElapsed.HasValue
                ? $"{queue.QueueName ?? selected.Name} · {TimeSpan.FromSeconds(queue.QueueElapsed.Value):mm\\:ss}"
                : "Party owner controls the queue";
        CanControlQueue = queue.IsOwner && queue.AllReady;
    }

    private async Task SelectQueueAsync(QueueOptionViewModel option)
    {
        if (!option.IsEligible || IsInQueue) return;
        ControlMessage = $"Selecting {option.Name.ToLowerInvariant()}...";
        QueueHint = ControlMessage;
        SetMatchActionMessage(ControlMessage);
        try
        {
            var result = await _api.QueueActionAsync("select", option.Id).ConfigureAwait(true);
            ControlMessage = result?.Message ?? "The client did not return a response.";
            QueueHint = ControlMessage;
            SetMatchActionMessage(ControlMessage);
            if (result?.Queue is not null) ApplyQueue(result.Queue, ControlMessage);
            else
            {
                await RefreshQueueAsync().ConfigureAwait(true);
                QueueHint = ControlMessage;
            }
        }
        catch (Exception ex) { ControlMessage = QueueHint = ex.Message; }
    }

    private async Task RunQueueActionAsync()
    {
        ControlMessage = IsInQueue ? "Stopping queue..." : "Starting queue...";
        QueueHint = ControlMessage;
        SetMatchActionMessage(ControlMessage);
        try
        {
            var result = await _api.QueueActionAsync(IsInQueue ? "stop" : "start").ConfigureAwait(true);
            ControlMessage = result?.Message ?? "The client did not return a response.";
            QueueHint = ControlMessage;
            SetMatchActionMessage(ControlMessage);
            if (result?.Queue is not null) ApplyQueue(result.Queue, ControlMessage);
            else
            {
                await RefreshQueueAsync().ConfigureAwait(true);
                QueueHint = ControlMessage;
            }
        }
        catch (Exception ex) { ControlMessage = QueueHint = ex.Message; }
    }

    private async Task RefreshOfflineAsync()
    {
        try
        {
            var status = await _api.GetOfflineStatusAsync().ConfigureAwait(true);
            if (status is null) return;
            _offlineRunning = status.Running;
            OfflineEnabled = status.Enabled;
            OfflineButtonText = status.Enabled && !string.IsNullOrWhiteSpace(status.Status)
                ? $"Presence: {char.ToUpperInvariant(status.Status[0]) + status.Status[1..].ToLowerInvariant()}"
                : "Presence";
        }
        catch { }
    }

    private async Task SetOfflineStatusAsync(string status)
    {
        ControlMessage = status == "online" ? "Restoring online presence..." : $"Setting presence to {status}...";
        try
        {
            var result = await _api.SetOfflineStatusAsync(
                status, launch: status != "online" && !_offlineRunning).ConfigureAwait(true);
            ControlMessage = result?.Message ?? "The client did not return a response.";
            await RefreshOfflineAsync().ConfigureAwait(true);
        }
        catch (Exception ex) { ControlMessage = ex.Message; }
    }

    private async Task LoadAgentsAsync()
    {
        try
        {
            var result = await _api.GetAgentsAsync().ConfigureAwait(true);
            if (result is null) return;
            Agents.Clear();
            foreach (var agent in result.Agents.OrderBy(a => a.Role).ThenBy(a => a.Name))
            {
                var vm = new InstalockAgentViewModel(agent) { IsSelected = agent.Name == SelectedAgent };
                Agents.Add(vm);
                _ = LoadInstalockAgentArtAsync(vm);
            }
            if (!Agents.Any(agent => agent.Name == SelectedAgent) && Agents.Count > 0)
                SelectedAgent = Agents[0].Name;
        }
        catch { }
    }

    private async Task LoadInstalockAgentArtAsync(InstalockAgentViewModel agent)
    {
        try { agent.Portrait = await _images.GetAsync(agent.PortraitUrl).ConfigureAwait(true); }
        catch { }
    }

    private void ShowTeamSide()
    {
        var message = string.IsNullOrWhiteSpace(SideText)
            ? "Team side is only available during Agent Select or a live match."
            : string.Equals(SideText, "Attacker", StringComparison.OrdinalIgnoreCase)
                ? string.IsNullOrWhiteSpace(MapName)
                    ? "Your team is starting on Attack."
                    : $"Your team is starting on Attack on {MapName}."
                : string.IsNullOrWhiteSpace(MapName)
                    ? "Your team is starting on Defense."
                    : $"Your team is starting on Defense on {MapName}.";
        ControlMessage = message;
        SetMatchActionMessage(message);
    }

    private void CheckSide() => ShowTeamSide();

    private void SetMatchActionMessage(string message)
    {
        MatchActionMessage = message;
        Raise(nameof(HasMatchActionMessage));
    }

    private bool _dodging;

    // Release behavior: Dodge is one click and immediate. No confirmation
    // prompt; lightweight inline feedback only via MatchActionMessage.
    private async Task DodgeAsync()
    {
        if (_dodging) return;
        _dodging = true;
        SetMatchActionMessage("Dodging…");
        try
        {
            var result = await _api.DodgeAsync().ConfigureAwait(true);
            var message = !string.IsNullOrWhiteSpace(result?.Message)
                ? result!.Message!
                : "The client did not return a response.";
            ControlMessage = message;
            SetMatchActionMessage(message);
        }
        catch (Exception ex)
        {
            ControlMessage = ex.Message;
            SetMatchActionMessage(ex.Message);
        }
        finally { _dodging = false; }
    }

    private async Task StartInstalockAsync()
    {
        InstalockStatus = $"Arming {SelectedAgent}...";
        try
        {
            var result = await _api.StartInstalockAsync(
                SelectedAgent, InstalockMode, InstalockDelay).ConfigureAwait(true);
            InstalockRunning = result?.Ok == true && result.Running;
            InstalockStatus = !string.IsNullOrWhiteSpace(result?.Message) ? result.Message
                : result?.Ok == true ? $"Armed for {SelectedAgent}." : "The client did not return a response.";
            SyncInstalockButton();
        }
        catch (Exception ex) { InstalockStatus = ex.Message; }
    }

    private async Task StopInstalockAsync()
    {
        try
        {
            var result = await _api.StopInstalockAsync().ConfigureAwait(true);
            InstalockRunning = false;
            InstalockStatus = !string.IsNullOrWhiteSpace(result?.Message) ? result.Message : "Instalock stopped.";
            SyncInstalockButton();
        }
        catch (Exception ex) { InstalockStatus = ex.Message; }
    }

    private async Task ToggleInstalockAsync()
    {
        if (InstalockRunning) await StopInstalockAsync().ConfigureAwait(true);
        else await StartInstalockAsync().ConfigureAwait(true);
    }

    private void SyncInstalockButton() =>
        InstalockButtonText = InstalockRunning ? $"Instalock: {SelectedAgent}" : "Instalock";

    private async Task RefreshInstalockAsync()
    {
        try
        {
            var result = await _api.GetInstalockStatusAsync().ConfigureAwait(true);
            if (result is null) return;
            InstalockRunning = result.Running;
            if (!string.IsNullOrWhiteSpace(result.Message)) InstalockStatus = result.Message;
            else if (result.Running)
                InstalockStatus = $"Armed for {(string.IsNullOrWhiteSpace(result.Agent) ? SelectedAgent : result.Agent)}.";
            else if (result.Status is "locked" or "error")
                InstalockStatus = result.Status == "locked" ? "Agent locked." : "Instalock stopped after an error.";
            SyncInstalockButton();
        }
        catch { }
    }

    private async Task RefreshPerformanceAsync(bool scheduleFollowUp = true)
    {
        try
        {
            var performance = await _api.GetPerformanceAsync(MatchLimit).ConfigureAwait(true);
            if (performance is null) return;
            _lastPerformanceRefresh = DateTime.UtcNow;
            _performancePoints.Clear();
            _performancePoints.AddRange(performance.Points.OrderByDescending(p => p.Ts ?? 0));
            PerformanceAccount = string.IsNullOrWhiteSpace(performance.Account.RiotId)
                ? "Competitive history" : performance.Account.RiotId!;
            if (performance.Summary.Matches > 0)
            {
                NetRrText = $"{(performance.Summary.Net >= 0 ? "+" : "")}{performance.Summary.Net}";
                RecordText = $"{performance.Summary.Wins} - {performance.Summary.Losses}";
                CurrentRankText = performance.Summary.Current.Name;
                CurrentRrText = performance.Summary.Current.Rr.HasValue
                    ? $"{performance.Summary.Current.Rr} RR" : "";
                var tier = performance.Summary.Current.Tier;
                CurrentRankIcon = null;
                if (tier.HasValue
                    && performance.RankIcons.TryGetValue(tier.Value.ToString(), out var iconUrl)
                    && !string.IsNullOrWhiteSpace(iconUrl))
                {
                    try { CurrentRankIcon = await _images.GetAsync(iconUrl).ConfigureAwait(true); }
                    catch { CurrentRankIcon = null; }
                }
            }
            else
            {
                NetRrText = "—";
                RecordText = "No matches yet";
                CurrentRankText = "Unranked";
                CurrentRrText = "";
                CurrentRankIcon = null;
            }
            PerformanceSessions.Clear();
            if (performance.Sessions.Active is not null)
                PerformanceSessions.Add(new SessionViewModel(performance.Sessions.Active, active: true));
            foreach (var session in performance.Sessions.Archive)
                PerformanceSessions.Add(new SessionViewModel(session, active: false));
            HasSessions = PerformanceSessions.Count > 0;
            ApplyPerformanceFilter();
            if (scheduleFollowUp) _ = RefreshPerformanceFollowUpAsync();
        }
        catch { }
    }

    private async Task RefreshPerformanceFollowUpAsync()
    {
        if (_performanceFollowUpScheduled) return;
        _performanceFollowUpScheduled = true;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(true);
            if (_quitting || _gameState != "MENUS") return;
            while (_refreshing && !_quitting)
                await Task.Delay(500).ConfigureAwait(true);
            if (!_quitting) await RefreshPerformanceAsync(scheduleFollowUp: false).ConfigureAwait(true);
        }
        finally { _performanceFollowUpScheduled = false; }
    }

    private void ApplyPerformanceFilter()
    {
        IEnumerable<PerformancePointDto> points = _performancePoints;
        if (PerformanceScope == "Act")
        {
            var season = _performancePoints.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.SeasonId))?.SeasonId;
            if (!string.IsNullOrWhiteSpace(season))
                points = points.Where(p => string.IsNullOrWhiteSpace(p.SeasonId) || p.SeasonId == season);
        }
        PerformanceMatches.Clear();
        foreach (var point in points.Take(MatchLimit))
        {
            var vm = new PerformancePointViewModel(point, _images, ChartMode) { OpenMatch = OpenPerformanceMatch };
            PerformanceMatches.Add(vm);
            if (PerformanceTab == "Matches") _ = vm.EnsureImageAsync();
        }
        PerformanceEmptyText = PerformanceMatches.Count == 0
            ? "Play a competitive match to begin tracking rank progress."
            : "Select a match for the full scoreboard.";
        Raise(nameof(HasPerformance));
        Raise(nameof(MatchCountText));
    }

    private void OpenPerformanceMatch(PerformancePointViewModel point)
    {
        if (string.IsNullOrWhiteSpace(point.Dto.MatchId)) return;
        try
        {
            var window = new MatchWindow(_api, _images, point.Dto.MatchId, null)
            {
                Owner = Application.Current.MainWindow,
            };
            window.Show();
        }
        catch { }
    }

    private async Task RunSessionActionAsync(string action)
    {
        try
        {
            var result = await _api.SessionActionAsync(action).ConfigureAwait(true);
            ControlMessage = result?.Message ?? "Session updated.";
            await RefreshPerformanceAsync().ConfigureAwait(true);
        }
        catch (Exception ex) { ControlMessage = ex.Message; }
    }

    private void OpenProfileFor(PlayerViewModel player)
    {
        try
        {
            var window = new ProfileWindow(_api, _images, player.Dto.Puuid, player.DisplayName,
                player.RankName, player.Dto.RankIcon, player.PeakRankName, player.Dto.PeakIcon)
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
        if (info is null)
        {
            UpdateStatusText = "You're up to date.";
            Raise(nameof(HasUpdateStatus));
            return;
        }
        _updateUrl = info.DownloadUrl;
        UpdateText = $"Update available · v{info.Version}";
        HasUpdate = true;
        UpdateStatusText = $"Update available · v{info.Version}";
        Raise(nameof(HasUpdateStatus));
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
