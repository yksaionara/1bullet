using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace OneBullet;

public sealed class StatBox
{
    public StatBox(string value, string label, bool highlight = false)
    {
        Value = value;
        Label = label;
        Highlight = highlight;
    }

    public string Value { get; }
    public string Label { get; }
    public bool Highlight { get; }
}

public sealed class PastGameViewModel : ObservableObject
{
    private ImageSource? _agentPortrait;

    public PastGameViewModel(CareerMatchDto match)
    {
        Match = match;
    }

    public CareerMatchDto Match { get; }
    public ImageSource? AgentPortrait { get => _agentPortrait; set => Set(ref _agentPortrait, value); }
    public string Kda => $"{Match.Kills}/{Match.Deaths}/{Match.Assists}";
    public string Detail => $"{Match.Acs} ACS · {(Match.HsPct.HasValue ? $"{Match.HsPct.Value:0}% HS" : "— HS")}";
    public string ResultLetter => Match.Result.StartsWith("V") ? "W"
        : Match.Result.StartsWith("D") ? "L" : "–";
    public string ResultColor => Match.Result.StartsWith("V") ? "#18E5A7"
        : Match.Result.StartsWith("D") ? "#FF4655" : "#9AA4AB";
}

public sealed class ProfileViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly ImageCache _images;
    private readonly string _puuid;
    private ImageSource? _portrait;
    private ImageSource? _bannerArt;
    private string _status = "Loading…";
    private string _subLine = "";

    public string DisplayName { get; }
    public ObservableCollection<StatBox> RecentForm { get; } = new();
    public ObservableCollection<SkinViewModel> Skins { get; } = new();
    public ObservableCollection<PastGameViewModel> PastGames { get; } = new();

    public ImageSource? Portrait { get => _portrait; set => Set(ref _portrait, value); }
    public ImageSource? BannerArt { get => _bannerArt; set => Set(ref _bannerArt, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public string SubLine { get => _subLine; set => Set(ref _subLine, value); }
    public bool HasGames => PastGames.Count > 0;

    public RelayCommand OpenMatchCommand { get; }

    public ProfileViewModel(ApiClient api, ImageCache images, string puuid, string displayName)
    {
        _api = api;
        _images = images;
        _puuid = puuid;
        DisplayName = displayName;
        OpenMatchCommand = new RelayCommand(p =>
        {
            if (p is PastGameViewModel game) OpenMatch(game);
        });
        _ = LoadAsync();
    }

    private void OpenMatch(PastGameViewModel game)
    {
        try
        {
            var window = new MatchWindow(_api, _images, game.Match.MatchId, _puuid)
            {
                Owner = Application.Current.MainWindow,
            };
            window.Show();
        }
        catch { }
    }

    private async Task LoadAsync()
    {
        CareerDto? career;
        try
        {
            career = await _api.GetProfileAsync(_puuid).ConfigureAwait(true);
        }
        catch { career = null; }
        if (career is null || career.Matches.Count == 0)
        {
            Status = "No recent history available for this player.";
            return;
        }
        Status = "";

        var avg = career.Averages;
        RecentForm.Add(new StatBox(avg is null ? "—" : $"{avg.Kd:0.00}", "K/D", true));
        RecentForm.Add(new StatBox(avg is null ? "—" : $"{avg.WinRate:0}%", "WIN%"));
        RecentForm.Add(new StatBox(avg?.HsPct.HasValue == true ? $"{avg.HsPct.Value:0}%" : "—", "HS%"));
        RecentForm.Add(new StatBox(avg is null ? "—" : $"{avg.Kills:0.0}", "KILLS"));
        RecentForm.Add(new StatBox(avg is null ? "—" : $"{avg.Deaths:0.0}", "DEATHS"));
        RecentForm.Add(new StatBox(avg is null ? "—" : $"{avg.Assists:0.0}", "ASSISTS"));

        var first = career.Matches[0];
        var bits = new List<string>();
        if (!string.IsNullOrWhiteSpace(first.Agent)) bits.Add(first.Agent);
        bits.Add($"{career.Matches.Count} games");
        SubLine = string.Join(" · ", bits);
        Portrait = await _images.GetAsync(first.AgentPortrait).ConfigureAwait(true);

        foreach (var match in career.Matches)
        {
            var vm = new PastGameViewModel(match);
            PastGames.Add(vm);
            _ = LoadPastGameArtAsync(vm);
        }
        Raise(nameof(HasGames));

        // Equipped loadout is served with the live profile payload.
        try
        {
            var board = await _api.GetBoardAsync().ConfigureAwait(true);
            var live = board?.Players.FirstOrDefault(p => p.Puuid == _puuid);
            if (live is not null)
            {
                BannerArt = await _images.GetAsync(live.AgentArt).ConfigureAwait(true);
                foreach (var weapon in live.Weapons)
                {
                    var skin = new SkinViewModel(weapon.Weapon,
                        string.IsNullOrWhiteSpace(weapon.Skin?.Name) ? "—" : weapon.Skin!.Name);
                    Skins.Add(skin);
                    _ = LoadSkinArtAsync(skin, weapon.Skin?.Icon);
                }
            }
            if (Skins.Count == 0) Status = "Loadout unavailable for this player.";
        }
        catch { }
    }

    private async Task LoadPastGameArtAsync(PastGameViewModel vm)
    {
        try { vm.AgentPortrait = await _images.GetAsync(vm.Match.AgentPortrait).ConfigureAwait(true); }
        catch { }
    }

    private async Task LoadSkinArtAsync(SkinViewModel skin, string? url)
    {
        try { skin.Icon = await _images.GetAsync(url).ConfigureAwait(true); }
        catch { }
    }
}
