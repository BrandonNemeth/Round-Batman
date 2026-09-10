using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public interface IMatchRepository
{
    Task<List<Match>> GetAllAsync(string tournamentId);
    Task<List<Match>> GetByGroupAsync(string tournamentId, string groupId);
    Task<Match?> GetByIdAsync(string tournamentId, string matchId);
    Task<Match> AddAsync(string tournamentId, Match match);
    Task<List<Match>> AddRangeAsync(string tournamentId, List<Match> matches);
    Task<bool> DeleteAsync(string tournamentId, string matchId);
    Task UpdateAsync(string tournamentId, Match match);
}
