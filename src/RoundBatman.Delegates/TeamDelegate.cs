using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;

namespace RoundBatman.Delegates;

public class TeamDelegate(ITeamRepository teamRepository) : ITeamDelegate
{
    public Task<List<Team>> GetAllAsync() => teamRepository.GetAllAsync();

    public Task<Team?> GetByIdAsync(string id) => teamRepository.GetByIdAsync(id);

    public Task<Team> CreateAsync(string name)
    {
        var team = new Team { Id = Guid.NewGuid().ToString(), Name = name };
        return teamRepository.AddAsync(team);
    }

    public async Task<List<Team>> GenerateAsync(int count, int? seed = null)
    {
        if (count is < 1 or > 32)
            throw new BusinessRuleException("The generated team count must be between 1 and 32.");

        var existingNames = (await teamRepository.GetAllAsync())
            .Select(team => team.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var generated = new List<Team>(count);
        var names = new[]
        {
            "Halcones", "Cometas", "Titanes", "Dragones", "Guardianes", "Centellas",
            "Pioneros", "Forajidos", "Leones", "Cuervos", "Tormenta", "Auroras",
        };
        var places = new[]
        {
            "de Gotham", "de Arkham", "del Norte", "del Puerto", "de la Noche",
            "del Acero", "del Valle", "del Cosmos", "del Barrio", "del Trueno",
        };

        while (generated.Count < count)
        {
            var baseName = $"{names[random.Next(names.Length)]} {places[random.Next(places.Length)]}";
            var name = baseName;
            var suffix = 2;
            while (!existingNames.Add(name))
                name = $"{baseName} {suffix++}";

            var team = new Team { Id = Guid.NewGuid().ToString(), Name = name };
            await teamRepository.AddAsync(team);
            generated.Add(team);
        }

        return generated;
    }

    public async Task<Team> UpdateAsync(string id, string name)
    {
        var team = await teamRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Team {id} not found.");

        team.Name = name;
        await teamRepository.UpdateAsync(team);
        return team;
    }

    public Task<bool> DeleteAsync(string id) => teamRepository.DeleteAsync(id);
}
