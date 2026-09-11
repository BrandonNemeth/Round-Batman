using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public interface ITournamentRepository
{
    Task<List<Tournament>> GetAllAsync();
    Task<Tournament?> GetByIdAsync(string id);
    Task<Tournament> AddAsync(Tournament tournament);
    Task UpdateAsync(Tournament tournament);
    Task<bool> DeleteAsync(string id);
}
