namespace RoundBatman.Api.Dtos;

public record GroupDto(string Id, string Name, List<TeamDto> Teams);
public record CreateGroupDto(string Name);
public record AssignTeamsDto(List<string> TeamIds);

public static class GroupMapper
{
    public static GroupDto ToDto(this RoundBatman.Domain.Group group) =>
        new(group.Id, group.Name, group.Teams.Select(t => t.ToDto()).ToList());
}
