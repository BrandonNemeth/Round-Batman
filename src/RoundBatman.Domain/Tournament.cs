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
    public string? ChampionTeamId { get; set; }

    public TournamentStatus Status
    {
        get
        {
            if (Matches.Count == 0)
                return TournamentStatus.NOT_STARTED;

            var hasEveryConfiguredGroup = Groups.Count == Format.NumberOfGroups;
            var everyGroupHasMatches = Groups.All(group =>
                Matches.Any(match => match.GroupId == group.Id));

            if (!hasEveryConfiguredGroup || !everyGroupHasMatches)
                return TournamentStatus.IN_PROGRESS;

            var groupMatches = Matches.Where(match => match.Stage == Enums.MatchStage.GROUP);
            if (groupMatches.Any(match => !match.IsCompleted))
                return TournamentStatus.IN_PROGRESS;

            var knockoutMatches = Matches.Where(match => match.Stage == Enums.MatchStage.KNOCKOUT).ToList();
            if (knockoutMatches.Count == 0)
                return ChampionTeamId is not null
                    ? TournamentStatus.FINISHED
                    : TournamentStatus.IN_PROGRESS;

            return ChampionTeamId is not null && knockoutMatches.All(match => match.IsCompleted)
                ? TournamentStatus.FINISHED
                : TournamentStatus.IN_PROGRESS;
        }
    }
}
