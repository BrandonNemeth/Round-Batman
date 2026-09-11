using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;

namespace RoundBatman.Delegates;

public class TeamDelegate(ITeamRepository teamRepository) : ITeamDelegate
{
    public Task<List<Team>> GetAllAsync() => teamRepository.GetAllAsync();

    public Task<Team?> GetByIdAsync(string id) => teamRepository.GetByIdAsync(id);

    public Task<Team> CreateAsync(string name)
    {
        var team = new Team { Id = Guid.NewGuid().ToString(), Name = name };
        return teamRepository.AddAsync(team);
    }

    public async Task<Team> UpdateAsync(string id, string name)
    {
        var team = await teamRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Team {id} not found.");

        team.Name = name;
        await teamRepository.UpdateAsync(team);
        return team;
    }

    public Task<bool> DeleteAsync(string id) => teamRepository.DeleteAsync(id);
}
