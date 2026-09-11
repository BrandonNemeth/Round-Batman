using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;

namespace RoundBatman.Delegates;

public class GroupDelegate(IGroupRepository groupRepository, ITeamRepository teamRepository) : IGroupDelegate
{
    public Task<List<Group>> GetAllAsync(string tournamentId) => groupRepository.GetAllAsync(tournamentId);

    public Task<Group?> GetByIdAsync(string tournamentId, string groupId) =>
        groupRepository.GetByIdAsync(tournamentId, groupId);

    public Task<Group> CreateAsync(string tournamentId, string name)
    {
        var group = new Group { Id = Guid.NewGuid().ToString(), Name = name, TournamentId = tournamentId };
        return groupRepository.AddAsync(tournamentId, group);
    }

    public async Task<Group> UpdateAsync(string tournamentId, string groupId, string name)
    {
        var group = await groupRepository.GetByIdAsync(tournamentId, groupId)
            ?? throw new NotFoundException($"Group {groupId} not found in tournament {tournamentId}.");

        group.Name = name;
        await groupRepository.UpdateAsync(tournamentId, group);
        return group;
    }

    public Task<bool> DeleteAsync(string tournamentId, string groupId) =>
        groupRepository.DeleteAsync(tournamentId, groupId);

    public async Task<Group> AssignTeamsAsync(string tournamentId, string groupId, List<string> teamIds)
    {
        var group = await groupRepository.GetByIdAsync(tournamentId, groupId)
            ?? throw new NotFoundException($"Group {groupId} not found in tournament {tournamentId}.");

        var teams = new List<Team>();
        foreach (var teamId in teamIds)
        {
            var team = await teamRepository.GetByIdAsync(teamId)
                ?? throw new NotFoundException($"Team {teamId} not found.");
            teams.Add(team);
        }

        group.Teams = teams;
        await groupRepository.UpdateAsync(tournamentId, group);
        return group;
    }
}
