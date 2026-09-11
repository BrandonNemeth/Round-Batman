using RoundBatman.Domain;

namespace RoundBatman.Delegates;

public interface ITeamDelegate
{
    Task<List<Team>> GetAllAsync();
    Task<Team?> GetByIdAsync(string id);
    Task<Team> CreateAsync(string name);
    Task<Team> UpdateAsync(string id, string name);
    Task<bool> DeleteAsync(string id);
}
