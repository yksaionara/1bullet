using System.Collections.ObjectModel;
using System.Windows.Media;

namespace OneBullet;

public sealed class MatchTeamViewModel
{
    public MatchTeamViewModel(string title, string summary)
    {
        Title = title;
        Summary = summary;
    }

    public string Title { get; }
    public string Summary { get; }
    public ObservableCollection<MatchPlayerViewModel> Players { get; } = new();
}

public sealed class MatchPlayerViewModel : ObservableObject
{
    private ImageSource? _agentPortrait;

    public MatchPlayerViewModel(MatchPlayerDto dto)
    {
        Dto = dto;
    }

    public MatchPlayerDto Dto { get; }
    public ImageSource? AgentPortrait { get => _agentPortrait; set => Set(ref _agentPortrait, value); }
    public string Kda => $"{Dto.Kills}/{Dto.Deaths}/{Dto.Assists}";
    public string Detail => $"{Dto.Acs} ACS · {(Dto.HsPct.HasValue ? $"{Dto.HsPct.Value:0}% HS" : "— HS")}";
    public bool IsSubject => Dto.IsSubject;
}

public sealed class MatchViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly ImageCache _images;
    private readonly string _matchId;
    private readonly string? _subject;
    private string _title = "Loading match…";
    private string _summary = "";

    public ObservableCollection<MatchTeamViewModel> Teams { get; } = new();

    public string Title { get => _title; set => Set(ref _title, value); }
    public string Summary { get => _summary; set => Set(ref _summary, value); }

    public MatchViewModel(ApiClient api, ImageCache images, string matchId, string? subject)
    {
        _api = api;
        _images = images;
        _matchId = matchId;
        _subject = subject;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        MatchDetailDto? detail;
        try
        {
            detail = await _api.GetMatchAsync(_matchId, _subject).ConfigureAwait(true);
        }
        catch { detail = null; }
        if (detail is null || !string.IsNullOrEmpty(detail.Error) || detail.Players.Count == 0)
        {
            Title = "Match details unavailable.";
            Summary = detail?.Error ?? "The backend could not load this match.";
            return;
        }
        Title = $"{detail.Map} · {detail.Mode}";
        var scores = detail.Scores.Count > 0
            ? string.Join(" : ", detail.Scores.OrderBy(kv => kv.Key).Select(kv => kv.Value))
            : "";
        Summary = string.IsNullOrWhiteSpace(scores) ? (detail.Result ?? "") : scores;

        foreach (var group in detail.Players.GroupBy(p => p.Team ?? "").OrderBy(g => g.Key))
        {
            var team = new MatchTeamViewModel(
                group.Key == "" ? "Players" : $"Team {group.Key}", "");
            foreach (var p in group.OrderByDescending(p => p.Acs))
            {
                var vm = new MatchPlayerViewModel(p);
                team.Players.Add(vm);
                _ = LoadArtAsync(vm);
            }
            Teams.Add(team);
        }
    }

    private async Task LoadArtAsync(MatchPlayerViewModel vm)
    {
        try { vm.AgentPortrait = await _images.GetAsync(vm.Dto.AgentPortrait).ConfigureAwait(true); }
        catch { }
    }
}
