namespace RoundBatman.Api.Dtos;

public record TeamDto(string Id, string Name);
public record CreateTeamDto(string Name);

public static class TeamMapper
{
    public static TeamDto ToDto(this RoundBatman.Domain.Team team) =>
        new(team.Id, team.Name);
}
