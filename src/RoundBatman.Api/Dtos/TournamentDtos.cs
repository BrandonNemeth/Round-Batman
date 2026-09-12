using RoundBatman.Domain.Enums;

namespace RoundBatman.Api.Dtos;

public record TournamentFormatDto(TournamentType Type, int NumberOfGroups, int MaxTeamsPerGroup);
public record CreateTournamentDto(string Name, TournamentFormatDto Format);
public record UpdateTournamentDto(string Name, TournamentFormatDto Format);

public record PatchTournamentFormatDto(TournamentType? Type, int? NumberOfGroups, int? MaxTeamsPerGroup);
public record PatchTournamentDto(string? Name, PatchTournamentFormatDto? Format);

public record TournamentDto(
    string Id,
    string Name,
    TournamentFormatDto Format,
    List<GroupDto> Groups,
    List<MatchDto> Matches);

public static class TournamentMapper
{
    public static TournamentDto ToDto(this RoundBatman.Domain.Tournament tournament)
    {
        var teamsById = tournament.Groups
            .SelectMany(g => g.Teams)
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.First());

        return new(
            tournament.Id,
            tournament.Name,
            new TournamentFormatDto(tournament.Format.Type, tournament.Format.NumberOfGroups, tournament.Format.MaxTeamsPerGroup),
            tournament.Groups.Select(g => g.ToDto()).ToList(),
            tournament.Matches.Select(m => m.ToDto(
                teamsById.GetValueOrDefault(m.HomeTeamId),
                teamsById.GetValueOrDefault(m.VisitorTeamId))).ToList());
    }
}
