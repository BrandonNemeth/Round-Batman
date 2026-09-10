using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;

namespace RoundBatman.Repositories;

public class MatchRepository(InMemoryDataStore store) : IMatchRepository
{
    private Tournament GetTournamentOrThrow(string tournamentId) =>
        store.Tournaments.GetValueOrDefault(tournamentId)
            ?? throw new NotFoundException($"Tournament {tournamentId} not found.");

    public Task<List<Match>> GetAllAsync(string tournamentId) =>
        Task.FromResult(GetTournamentOrThrow(tournamentId).Matches);

    public Task<List<Match>> GetByGroupAsync(string tournamentId, string groupId) =>
        Task.FromResult(GetTournamentOrThrow(tournamentId).Matches
            .Where(m => m.GroupId == groupId).ToList());

    public Task<Match?> GetByIdAsync(string tournamentId, string matchId) =>
        Task.FromResult(GetTournamentOrThrow(tournamentId).Matches
            .FirstOrDefault(m => m.Id == matchId));

    public Task<Match> AddAsync(string tournamentId, Match match)
    {
        var tournament = GetTournamentOrThrow(tournamentId);
        match.TournamentId = tournamentId;
        tournament.Matches.Add(match);
        return Task.FromResult(match);
    }

    public Task<List<Match>> AddRangeAsync(string tournamentId, List<Match> matches)
    {
        var tournament = GetTournamentOrThrow(tournamentId);
        foreach (var match in matches)
            match.TournamentId = tournamentId;
        tournament.Matches.AddRange(matches);
        return Task.FromResult(matches);
    }

    public Task<bool> DeleteAsync(string tournamentId, string matchId)
    {
        var tournament = GetTournamentOrThrow(tournamentId);
        var removed = tournament.Matches.RemoveAll(m => m.Id == matchId) > 0;
        return Task.FromResult(removed);
    }

    public Task UpdateAsync(string tournamentId, Match match)
    {
        var tournament = GetTournamentOrThrow(tournamentId);
        var index = tournament.Matches.FindIndex(m => m.Id == match.Id);
        if (index >= 0) tournament.Matches[index] = match;
        return Task.CompletedTask;
    }
}
