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

    public Task<Group?> GetByIdAsync(string tournamentId, string groupId) =>
        Task.FromResult(GetTournamentOrThrow(tournamentId).Groups
            .FirstOrDefault(g => g.Id == groupId));

    public Task<Group> AddAsync(string tournamentId, Group group)
    {
        var tournament = GetTournamentOrThrow(tournamentId);
        group.TournamentId = tournamentId;
        tournament.Groups.Add(group);
        return Task.FromResult(group);
    }

    public Task<bool> DeleteAsync(string tournamentId, string groupId)
    {
        var tournament = GetTournamentOrThrow(tournamentId);
        var removed = tournament.Groups.RemoveAll(g => g.Id == groupId) > 0;
        return Task.FromResult(removed);
    }

    public Task UpdateAsync(string tournamentId, Group group)
    {
        var tournament = GetTournamentOrThrow(tournamentId);
        var index = tournament.Groups.FindIndex(g => g.Id == group.Id);
        if (index >= 0) tournament.Groups[index] = group;
        return Task.CompletedTask;
    }
}
