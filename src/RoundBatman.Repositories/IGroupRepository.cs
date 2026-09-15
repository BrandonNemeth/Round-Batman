using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public interface IGroupRepository : IRepository<Group, GroupRepositoryKey>
{
    Task<List<Group>> GetAllAsync(string tournamentId);
}
