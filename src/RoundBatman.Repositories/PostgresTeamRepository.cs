using Microsoft.EntityFrameworkCore;
using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public class PostgresTeamRepository(RoundBatmanDbContext db) : ITeamRepository
{
    public Task<List<Team>> GetAllAsync() =>
        db.Teams.AsNoTracking().OrderBy(team => team.Name).ToListAsync();

    public Task<Team?> GetByIdAsync(string id) =>
        db.Teams.FindAsync(id).AsTask();

    public async Task<Team> AddAsync(Team team)
    {
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    public async Task UpdateAsync(Team team)
    {
        db.Teams.Update(team);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var team = await db.Teams.FindAsync(id);
        if (team is null) return false;

        db.Teams.Remove(team);
        await db.SaveChangesAsync();
        return true;
    }
}
