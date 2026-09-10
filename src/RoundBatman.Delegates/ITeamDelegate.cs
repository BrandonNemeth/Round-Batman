using RoundBatman.Domain;

namespace RoundBatman.Delegates;

public interface ITeamDelegate
{
    Task<List<Team>> GetAllAsync();
    Task<Team> CreateAsync(string name);
    Task<bool> DeleteAsync(string id);
}
