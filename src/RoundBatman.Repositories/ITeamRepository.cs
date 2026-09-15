using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public interface ITeamRepository : IRepository<Team, string>
{
    Task<List<Team>> GetAllAsync();
}
