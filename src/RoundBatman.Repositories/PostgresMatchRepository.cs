using Microsoft.EntityFrameworkCore;
using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Domain.Exceptions;

namespace RoundBatman.Repositories;

public class PostgresMatchRepository(RoundBatmanDbContext db) : IMatchRepository
{
    public async Task<List<Match>> GetAllAsync(string tournamentId)
    {
        await EnsureTournamentExists(tournamentId);
        return await db.Matches.AsNoTracking()
            .Where(match => match.TournamentId == tournamentId)
            .ToListAsync();
    }

    public async Task<List<Match>> GetByGroupAsync(string tournamentId, string groupId)
    {
        await EnsureTournamentExists(tournamentId);
        return await db.Matches.AsNoTracking()
            .Where(match => match.TournamentId == tournamentId && match.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<List<Match>> GetByStageAndRoundAsync(string tournamentId, MatchStage stage, int round)
    {
        await EnsureTournamentExists(tournamentId);
        return await db.Matches.AsNoTracking()
            .Where(match => match.TournamentId == tournamentId &&
                           match.Stage == stage &&
                           match.Round == round)
            .ToListAsync();
    }

    public async Task<Match?> GetByIdAsync(string tournamentId, string matchId)
    {
        await EnsureTournamentExists(tournamentId);
        return await db.Matches.FirstOrDefaultAsync(match =>
            match.TournamentId == tournamentId && match.Id == matchId);
    }

    public async Task<Match> AddAsync(string tournamentId, Match match)
    {
        await EnsureTournamentExists(tournamentId);
        match.TournamentId = tournamentId;
        db.Matches.Add(match);
        await db.SaveChangesAsync();
        return match;
    }

    public async Task<List<Match>> AddRangeAsync(string tournamentId, List<Match> matches)
    {
        await EnsureTournamentExists(tournamentId);
        foreach (var match in matches) match.TournamentId = tournamentId;
        db.Matches.AddRange(matches);
        await db.SaveChangesAsync();
        return matches;
    }

    public async Task<bool> DeleteAsync(string tournamentId, string matchId)
    {
        var match = await GetByIdAsync(tournamentId, matchId);
        if (match is null) return false;

        db.Matches.Remove(match);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateAsync(string tournamentId, Match match)
    {
        var existing = await GetByIdAsync(tournamentId, match.Id);
        if (existing is null) return;

        existing.GroupId = match.GroupId;
        existing.HomeTeamId = match.HomeTeamId;
        existing.VisitorTeamId = match.VisitorTeamId;
        existing.Stage = match.Stage;
        existing.Round = match.Round;
        existing.IsBye = match.IsBye;
        existing.Score.SetResult(match.Score.HomeTeamScore, match.Score.VisitorTeamScore);
        await db.SaveChangesAsync();
    }

    private async Task EnsureTournamentExists(string tournamentId)
    {
        if (!await db.Tournaments.AnyAsync(tournament => tournament.Id == tournamentId))
            throw new NotFoundException($"Tournament {tournamentId} not found.");
    }
}
