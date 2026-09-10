using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Repositories;

namespace RoundBatman.Delegates;

public class TournamentDelegate(ITournamentRepository tournamentRepository) : ITournamentDelegate
{
    public Task<List<Tournament>> GetAllAsync() => tournamentRepository.GetAllAsync();

    public Task<Tournament?> GetByIdAsync(string id) => tournamentRepository.GetByIdAsync(id);

    public Task<Tournament> CreateAsync(string name, TournamentType type, int numberOfGroups, int maxTeamsPerGroup)
    {
        var tournament = new Tournament
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Format = new TournamentFormat
            {
                Type = type,
                NumberOfGroups = numberOfGroups,
                MaxTeamsPerGroup = maxTeamsPerGroup,
            },
        };
        return tournamentRepository.AddAsync(tournament);
    }

    public Task<bool> DeleteAsync(string id) => tournamentRepository.DeleteAsync(id);
}
