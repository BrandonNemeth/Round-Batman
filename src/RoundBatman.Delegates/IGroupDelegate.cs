using RoundBatman.Domain;

namespace RoundBatman.Delegates;

public interface IGroupDelegate
{
    Task<List<Group>> GetAllAsync(string tournamentId);
    Task<Group?> GetByIdAsync(string tournamentId, string groupId);
    Task<Group> CreateAsync(string tournamentId, string name);
    Task<Group> UpdateAsync(string tournamentId, string groupId, string name);
    Task<bool> DeleteAsync(string tournamentId, string groupId);
    Task<Group> AssignTeamsAsync(string tournamentId, string groupId, List<string> teamIds);
}
