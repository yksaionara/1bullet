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
    public double? AvgKd { get; set; }
    public double? AvgWinRate { get; set; }
}

public sealed class PlayerDto
{
    public string Puuid { get; set; } = "";
    public string Name { get; set; } = "";
    public bool NameHidden { get; set; }
    public string Team { get; set; } = "";
    public bool IsSelf { get; set; }
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
    public int Leaderboard { get; set; }
    public string PeakRank { get; set; } = "";
    public string PeakColor { get; set; } = "";
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
