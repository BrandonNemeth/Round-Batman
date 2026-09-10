using RoundBatman.Domain.Enums;

namespace RoundBatman.Domain;

public class TournamentFormat
{
    public int MaxTeamsPerGroup { get; set; }
    public int NumberOfGroups { get; set; }
    public TournamentType Type { get; set; }
}

public class Tournament
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TournamentFormat Format { get; set; } = new();
    public List<Group> Groups { get; set; } = [];
    public List<Match> Matches { get; set; } = [];
}
