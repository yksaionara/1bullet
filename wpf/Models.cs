using System.Text.Json.Serialization;

namespace OneBullet;

// DTOs for the local 1 Bullet Python backend (127.0.0.1 only).
// Unknown JSON properties are ignored; everything nullable that the
// backend may omit or null out.

public sealed class HealthResponse
{
    public bool Ok { get; set; }
    public string Service { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public int Protocol { get; set; }
    public string ClientStatus { get; set; } = "";
    public string DataSourcePreference { get; set; } = "";
}

public sealed class NoticeDto
{
    public string Level { get; set; } = "";
    public string Action { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class ScoreDto
{
    public int Ally { get; set; }
    public int Enemy { get; set; }
    public int? Round { get; set; }
}

public sealed class LockProgressDto
{
    public int Locked { get; set; }
    public int Total { get; set; }
}

public sealed class PartyDto
{
    public string Id { get; set; } = "";
    public string Color { get; set; } = "";
    public int Number { get; set; }
    public int Size { get; set; }
}

public sealed class SkinDto
{
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
}

public sealed class WeaponDto
{
    public string Weapon { get; set; } = "";
    public SkinDto? Skin { get; set; }
}

public sealed class EncounterDto
{
    public int WithCount { get; set; }
    public int AgainstCount { get; set; }
    public int WinsWith { get; set; }
    public int LossesWith { get; set; }
    public int WinsAgainst { get; set; }
    public int LossesAgainst { get; set; }
}

public sealed class TeamStatDto
{
    public string AvgRank { get; set; } = "";
    public string RankColor { get; set; } = "";
    public string? RankIcon { get; set; }
    public double? AvgKd { get; set; }
    public double? AvgWinRate { get; set; }
    public int SmurfCount { get; set; }
    public int Size { get; set; }
}

public sealed class PlayerDto
{
    public string Puuid { get; set; } = "";
    public string Name { get; set; } = "";
    public bool NameHidden { get; set; }
    public string Team { get; set; } = "";
    public bool IsSelf { get; set; }
    public string? Title { get; set; }
    public string? PlayerCard { get; set; }
    public string? Agent { get; set; }
    public string? AgentPortrait { get; set; }
    public string? AgentColor { get; set; }
    public string? Role { get; set; }
    public string? Selection { get; set; }
    public int RankTier { get; set; }
    public string Rank { get; set; } = "";
    public string RankColor { get; set; } = "";
    public string? RankIcon { get; set; }
    public int Rr { get; set; }
    public int? RrEarned { get; set; }
    public string? AgentArt { get; set; }
    public int Leaderboard { get; set; }
    public string PeakRank { get; set; } = "";
    public string PeakColor { get; set; } = "";
    public string? PeakIcon { get; set; }
    public string? PeakAct { get; set; }
    public string PreviousRank { get; set; } = "";
    public double? WinRate { get; set; }
    public int Games { get; set; }
    public double? Kd { get; set; }
    public double? HsPct { get; set; }
    public List<WeaponDto> Weapons { get; set; } = new();
    public int Level { get; set; }
    public bool LevelHidden { get; set; }
    public PartyDto? Party { get; set; }
    public bool Smurf { get; set; }
    public EncounterDto? Encounter { get; set; }
}

public sealed class BoardDto
{
    public string State { get; set; } = "";
    public string StateLabel { get; set; } = "";
    public string Source { get; set; } = "";
    public string? Map { get; set; }
    public string? Mode { get; set; }
    public string? MatchId { get; set; }
    public string? SelfTeam { get; set; }
    public string? Side { get; set; }
    public List<PlayerDto> Players { get; set; } = new();
    public Dictionary<string, List<PlayerDto>> Teams { get; set; } = new();
    public Dictionary<string, TeamStatDto> TeamStats { get; set; } = new();
    public int? WinProb { get; set; }
    public List<PartyDto> Parties { get; set; } = new();
    public ScoreDto? Score { get; set; }
    public LockProgressDto? LockProgress { get; set; }
    public NoticeDto? Notice { get; set; }
    public string AppVersion { get; set; } = "";
    public string? Error { get; set; }
}

public sealed class QueueDto
{
    public bool Available { get; set; }
    public string? Message { get; set; }
    public string? QueueId { get; set; }
    public string? QueueName { get; set; }
    public string State { get; set; } = "";
    public bool InQueue { get; set; }
    public double? QueueElapsed { get; set; }
    public int PartySize { get; set; }
    public bool IsOwner { get; set; }
    public bool AllReady { get; set; }
    public List<QueueOptionDto> Eligible { get; set; } = new();
}

public sealed class QueueOptionDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class AgentListDto
{
    public List<AgentDto> Agents { get; set; } = new();
}

public sealed class AgentDto
{
    public string Name { get; set; } = "";
    public string Uuid { get; set; } = "";
    public string Role { get; set; } = "";
    public string Color { get; set; } = "";
    public string? Portrait { get; set; }
}

public sealed class ActionResponseDto
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string Status { get; set; } = "";
    public string Agent { get; set; } = "";
    public bool Running { get; set; }
    public bool Enabled { get; set; }
    public bool Connected { get; set; }
    public QueueDto? Queue { get; set; }
}

public sealed class OfflineStatusDto
{
    public bool Running { get; set; }
    public bool Active { get; set; }
    public string Status { get; set; } = "online";
    public bool Enabled { get; set; }
    public bool Connected { get; set; }
    public bool FriendsLoaded { get; set; }
}

public sealed class PerformanceDto
{
    public PerformanceAccountDto Account { get; set; } = new();
    public List<PerformancePointDto> Points { get; set; } = new();
    public PerformanceSummaryDto Summary { get; set; } = new();
    public PerformanceSessionsDto Sessions { get; set; } = new();
}

public sealed class PerformanceAccountDto
{
    public string? Puuid { get; set; }
    public string? RiotId { get; set; }
}

public sealed class PerformancePointDto
{
    public string MatchId { get; set; } = "";
    public long? Ts { get; set; }
    public string Map { get; set; } = "Unknown";
    public string Mode { get; set; } = "Competitive";
    public string Result { get; set; } = "";
    public int? Delta { get; set; }
    public int? Tier { get; set; }
    public int? Rr { get; set; }
    public string Agent { get; set; } = "";
    public string? AgentPortrait { get; set; }
    public int? Kills { get; set; }
    public int? Deaths { get; set; }
    public int? Assists { get; set; }
    public double? Kd { get; set; }
    public int? Acs { get; set; }
    public double? HsPct { get; set; }
    public string? SeasonId { get; set; }
}

public sealed class PerformanceSummaryDto
{
    public int Matches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double? WinRate { get; set; }
    public int Net { get; set; }
    public double? AvgWin { get; set; }
    public double? AvgLoss { get; set; }
    public RankSnapshotDto Current { get; set; } = new();
}

public sealed class RankSnapshotDto
{
    public string Name { get; set; } = "Unranked";
    public string Color { get; set; } = "#8A8A91";
    public int? Rr { get; set; }
    public int? Tier { get; set; }
}

public sealed class PerformanceSessionsDto
{
    public PerformanceSessionDto? Active { get; set; }
    public List<PerformanceSessionDto> Archive { get; set; } = new();
}

public sealed class PerformanceSessionDto
{
    public string Id { get; set; } = "";
    public long StartedAt { get; set; }
    public long? EndedAt { get; set; }
    public PerformanceSummaryDto Summary { get; set; } = new();
}

public sealed class SettingsDto
{
    public string? AccentColor { get; set; }
    public string? Background { get; set; }
    public bool StartWithWindows { get; set; }
}

public sealed class SettingsSaveResponse
{
    public bool Ok { get; set; }
    public SettingsDto? Settings { get; set; }
    public string? Message { get; set; }
}

public sealed class CareerAveragesDto
{
    public int Games { get; set; }
    public int Wins { get; set; }
    public double WinRate { get; set; }
    public double Kd { get; set; }
    public double Kills { get; set; }
    public double Deaths { get; set; }
    public double Assists { get; set; }
    public double? HsPct { get; set; }
}

public sealed class CareerMatchDto
{
    public string MatchId { get; set; } = "";
    public string Map { get; set; } = "";
    public string? MapSplash { get; set; }
    public string Mode { get; set; } = "";
    public string Result { get; set; } = "";
    public string? Team { get; set; }
    public int Score { get; set; }
    public int? OpponentScore { get; set; }
    public string Agent { get; set; } = "";
    public string? AgentPortrait { get; set; }
    public string? AgentColor { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public double Kd { get; set; }
    public int Acs { get; set; }
    public double? HsPct { get; set; }
    public int PartySize { get; set; }
    public int? RrDelta { get; set; }
    public string? RankAfter { get; set; }
    public string? RankColor { get; set; }
}

public sealed class CareerDto
{
    public string Source { get; set; } = "";
    public string Puuid { get; set; } = "";
    public List<CareerMatchDto> Matches { get; set; } = new();
    public CareerAveragesDto? Averages { get; set; }
}

public sealed class MatchPlayerDto
{
    public string Puuid { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Team { get; set; }
    public string Agent { get; set; } = "";
    public string? AgentPortrait { get; set; }
    public string? AgentColor { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public double Kd { get; set; }
    public int Acs { get; set; }
    public double? HsPct { get; set; }
    public string Rank { get; set; } = "";
    public string RankColor { get; set; } = "";
    public string? RankIcon { get; set; }
    public bool IsSubject { get; set; }
    public bool IsMatchMvp { get; set; }
    public bool IsTeamMvp { get; set; }
}

public sealed class MatchDetailDto
{
    public string MatchId { get; set; } = "";
    public string Map { get; set; } = "";
    public string? MapSplash { get; set; }
    public string Mode { get; set; } = "";
    public Dictionary<string, int> Scores { get; set; } = new();
    public string? Result { get; set; }
    public List<MatchPlayerDto> Players { get; set; } = new();
    public string? Error { get; set; }
}

public sealed class ReleaseAssetDto
{
    public string Name { get; set; } = "";
    public string BrowserDownloadUrl { get; set; } = "";
}

public sealed class ReleaseDto
{
    public string TagName { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
    public List<ReleaseAssetDto> Assets { get; set; } = new();
}
