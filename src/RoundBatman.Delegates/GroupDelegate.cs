using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;

namespace RoundBatman.Delegates;

public class GroupDelegate(
    IGroupRepository groupRepository,
    ITeamRepository teamRepository,
    ITournamentRepository tournamentRepository) : IGroupDelegate
{
    public Task<List<Group>> GetAllAsync(string tournamentId) => groupRepository.GetAllAsync(tournamentId);

    public Task<Group?> GetByIdAsync(string tournamentId, string groupId) =>
        groupRepository.GetByIdAsync(tournamentId, groupId);

    public async Task<Group> CreateAsync(string tournamentId, string name)
    {
        var existingGroups = await groupRepository.GetAllAsync(tournamentId);
        if (existingGroups.Any(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException($"A group named '{name}' already exists in this tournament.");

        var group = new Group { Id = Guid.NewGuid().ToString(), Name = name, TournamentId = tournamentId };
        return await groupRepository.AddAsync(tournamentId, group);
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
        if (teamIds.Distinct().Count() != teamIds.Count)
            throw new BusinessRuleException("Duplicate team IDs in the request.");

        var group = await groupRepository.GetByIdAsync(tournamentId, groupId)
            ?? throw new NotFoundException($"Group {groupId} not found in tournament {tournamentId}.");

        var tournament = await tournamentRepository.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Tournament {tournamentId} not found.");

        if (teamIds.Count > tournament.Format.MaxTeamsPerGroup)
            throw new BusinessRuleException(
                $"Group cannot have more than {tournament.Format.MaxTeamsPerGroup} teams.");

        var allGroups = await groupRepository.GetAllAsync(tournamentId);

        var teams = new List<Team>();
        foreach (var teamId in teamIds)
        {
            var team = await teamRepository.GetByIdAsync(teamId)
                ?? throw new NotFoundException($"Team {teamId} not found.");

            var assignedElsewhere = allGroups
                .Where(g => g.Id != groupId)
                .Any(g => g.Teams.Any(t => t.Id == teamId));

            if (assignedElsewhere)
                throw new BusinessRuleException(
                    $"Team '{team.Name}' is already assigned to another group in this tournament.");

            teams.Add(team);
        }

        group.Teams = teams;
        await groupRepository.UpdateAsync(tournamentId, group);
        return group;
    }
}
