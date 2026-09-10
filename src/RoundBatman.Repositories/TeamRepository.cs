using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public class TeamRepository(InMemoryDataStore store) : ITeamRepository
{
    public Task<List<Team>> GetAllAsync() =>
        Task.FromResult(store.Teams.Values.ToList());

    public Task<Team?> GetByIdAsync(string id) =>
        Task.FromResult(store.Teams.GetValueOrDefault(id));

    public Task<Team> AddAsync(Team team)
    {
        store.Teams[team.Id] = team;
        return Task.FromResult(team);
    }

    public Task<bool> DeleteAsync(string id) =>
        Task.FromResult(store.Teams.TryRemove(id, out _));
}
