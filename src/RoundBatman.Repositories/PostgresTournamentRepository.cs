using Microsoft.EntityFrameworkCore;
using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public class PostgresTournamentRepository(RoundBatmanDbContext db) : ITournamentRepository
{
    public Task<List<Tournament>> GetAllAsync() =>
        Query().AsNoTracking().OrderBy(tournament => tournament.Name).ToListAsync();

    public Task<Tournament?> GetByIdAsync(string id) =>
        Query().FirstOrDefaultAsync(tournament => tournament.Id == id);

    public async Task<Tournament> AddAsync(Tournament tournament)
    {
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        return tournament;
    }

    public async Task UpdateAsync(Tournament tournament)
    {
        db.Tournaments.Update(tournament);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var tournament = await db.Tournaments.FindAsync(id);
        if (tournament is null) return false;

        db.Tournaments.Remove(tournament);
        await db.SaveChangesAsync();
        return true;
    }

    private IQueryable<Tournament> Query() =>
        db.Tournaments
            .Include(tournament => tournament.Groups)
                .ThenInclude(group => group.Teams)
            .Include(tournament => tournament.Matches);
}
