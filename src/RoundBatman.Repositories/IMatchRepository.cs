using RoundBatman.Domain;
using RoundBatman.Domain.Enums;

namespace RoundBatman.Repositories;

public interface IMatchRepository
{
    Task<List<Match>> GetAllAsync(string tournamentId);
    Task<List<Match>> GetByGroupAsync(string tournamentId, string groupId);
    Task<List<Match>> GetByStageAndRoundAsync(string tournamentId, MatchStage stage, int round);
    Task<Match?> GetByIdAsync(string tournamentId, string matchId);
    Task<Match> AddAsync(string tournamentId, Match match);
    Task<List<Match>> AddRangeAsync(string tournamentId, List<Match> matches);
    Task<bool> DeleteAsync(string tournamentId, string matchId);
    Task UpdateAsync(string tournamentId, Match match);
}
