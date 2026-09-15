using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;

namespace RoundBatman.Repositories;

public class GroupRepository(InMemoryDataStore store) : IGroupRepository
{
    private Tournament GetTournamentOrThrow(string tournamentId) =>
        store.Tournaments.GetValueOrDefault(tournamentId)
            ?? throw new NotFoundException($"Tournament {tournamentId} not found.");

    public Task<List<Group>> GetAllAsync(string tournamentId) =>
        Task.FromResult(GetTournamentOrThrow(tournamentId).Groups);

    public Task<Group?> GetByIdAsync(GroupRepositoryKey key) =>
        Task.FromResult(GetTournamentOrThrow(key.TournamentId).Groups
            .FirstOrDefault(g => g.Id == key.GroupId));

    public Task<Group> AddAsync(Group group)
    {
        var tournament = GetTournamentOrThrow(group.TournamentId);
        tournament.Groups.Add(group);
        return Task.FromResult(group);
    }

    public Task<bool> DeleteAsync(GroupRepositoryKey key)
    {
        var tournament = GetTournamentOrThrow(key.TournamentId);
        var removed = tournament.Groups.RemoveAll(g => g.Id == key.GroupId) > 0;
        return Task.FromResult(removed);
    }

    public Task UpdateAsync(Group group)
    {
        var tournament = GetTournamentOrThrow(group.TournamentId);
        var index = tournament.Groups.FindIndex(g => g.Id == group.Id);
        if (index >= 0) tournament.Groups[index] = group;
        return Task.CompletedTask;
    }
}
