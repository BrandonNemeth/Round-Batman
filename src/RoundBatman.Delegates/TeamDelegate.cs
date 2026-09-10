using RoundBatman.Domain;
using RoundBatman.Repositories;

namespace RoundBatman.Delegates;

public class TeamDelegate(ITeamRepository teamRepository) : ITeamDelegate
{
    public Task<List<Team>> GetAllAsync() => teamRepository.GetAllAsync();

    public Task<Team> CreateAsync(string name)
    {
        var team = new Team { Id = Guid.NewGuid().ToString(), Name = name };
        return teamRepository.AddAsync(team);
    }

    public Task<bool> DeleteAsync(string id) => teamRepository.DeleteAsync(id);
}
