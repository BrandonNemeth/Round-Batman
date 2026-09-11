using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Domain.Exceptions;
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

    public async Task<Tournament> UpdateAsync(string id, string name, TournamentType type, int numberOfGroups, int maxTeamsPerGroup)
    {
        var tournament = await tournamentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tournament {id} not found.");

        tournament.Name = name;
        tournament.Format = new TournamentFormat
        {
            Type = type,
            NumberOfGroups = numberOfGroups,
            MaxTeamsPerGroup = maxTeamsPerGroup,
        };

        await tournamentRepository.UpdateAsync(tournament);
        return tournament;
    }

    public async Task<Tournament> PatchAsync(string id, string? name, TournamentType? type, int? numberOfGroups, int? maxTeamsPerGroup)
    {
        var tournament = await tournamentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tournament {id} not found.");

        if (name is not null)
            tournament.Name = name;

        if (type is not null)
            tournament.Format.Type = type.Value;

        if (numberOfGroups is not null)
            tournament.Format.NumberOfGroups = numberOfGroups.Value;

        if (maxTeamsPerGroup is not null)
            tournament.Format.MaxTeamsPerGroup = maxTeamsPerGroup.Value;

        await tournamentRepository.UpdateAsync(tournament);
        return tournament;
    }

    public Task<bool> DeleteAsync(string id) => tournamentRepository.DeleteAsync(id);
}
