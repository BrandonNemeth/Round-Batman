using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public interface IGroupRepository
{
    Task<List<Group>> GetAllAsync(string tournamentId);
    Task<Group?> GetByIdAsync(string tournamentId, string groupId);
    Task<Group> AddAsync(string tournamentId, Group group);
    Task<bool> DeleteAsync(string tournamentId, string groupId);
    Task UpdateAsync(string tournamentId, Group group);
}
