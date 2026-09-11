using RoundBatman.Domain;
using RoundBatman.Domain.Enums;

namespace RoundBatman.Delegates;

public interface ITournamentDelegate
{
    Task<List<Tournament>> GetAllAsync();
    Task<Tournament?> GetByIdAsync(string id);
    Task<Tournament> CreateAsync(string name, TournamentType type, int numberOfGroups, int maxTeamsPerGroup);
    Task<Tournament> UpdateAsync(string id, string name, TournamentType type, int numberOfGroups, int maxTeamsPerGroup);
    Task<Tournament> PatchAsync(string id, string? name, TournamentType? type, int? numberOfGroups, int? maxTeamsPerGroup);
    Task<bool> DeleteAsync(string id);
}
