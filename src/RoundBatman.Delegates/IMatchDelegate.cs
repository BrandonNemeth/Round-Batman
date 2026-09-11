using RoundBatman.Domain;

namespace RoundBatman.Delegates;

public interface IMatchDelegate
{
    Task<List<Match>> GetAllAsync(string tournamentId);
    Task<Match?> GetByIdAsync(string tournamentId, string matchId);
    Task<Match> CreateAsync(string tournamentId, string? groupId, string homeTeamId, string visitorTeamId);
    Task<bool> DeleteAsync(string tournamentId, string matchId);
    Task<Match> UpdateScoreAsync(string tournamentId, string matchId, int homeScore, int visitorScore);
    Task<List<Match>> GenerateRoundRobinMatchesAsync(string tournamentId, string groupId);
}
