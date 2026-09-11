using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public interface ITeamRepository
{
    Task<List<Team>> GetAllAsync();
    Task<Team?> GetByIdAsync(string id);
    Task<Team> AddAsync(Team team);
    Task UpdateAsync(Team team);
    Task<bool> DeleteAsync(string id);
}
