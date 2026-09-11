using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public class TournamentRepository(InMemoryDataStore store) : ITournamentRepository
{
    public Task<List<Tournament>> GetAllAsync() =>
        Task.FromResult(store.Tournaments.Values.ToList());

    public Task<Tournament?> GetByIdAsync(string id) =>
        Task.FromResult(store.Tournaments.GetValueOrDefault(id));

    public Task<Tournament> AddAsync(Tournament tournament)
    {
        store.Tournaments[tournament.Id] = tournament;
        return Task.FromResult(tournament);
    }

    public Task UpdateAsync(Tournament tournament)
    {
        store.Tournaments[tournament.Id] = tournament;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id) =>
        Task.FromResult(store.Tournaments.TryRemove(id, out _));
}
