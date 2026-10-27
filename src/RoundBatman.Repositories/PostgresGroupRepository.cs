using Microsoft.EntityFrameworkCore;
using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;

namespace RoundBatman.Repositories;

public class PostgresGroupRepository(RoundBatmanDbContext db) : IGroupRepository
{
    public async Task<List<Group>> GetAllAsync(string tournamentId)
    {
        await EnsureTournamentExists(tournamentId);
        return await Query().Where(group => group.TournamentId == tournamentId).ToListAsync();
    }

    public async Task<Group?> GetByIdAsync(GroupRepositoryKey key)
    {
        await EnsureTournamentExists(key.TournamentId);
        return await Query().FirstOrDefaultAsync(group =>
            group.TournamentId == key.TournamentId && group.Id == key.GroupId);
    }

    public async Task<Group> AddAsync(Group group)
    {
        await EnsureTournamentExists(group.TournamentId);
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    public async Task<bool> DeleteAsync(GroupRepositoryKey key)
    {
        var group = await GetByIdAsync(key);
        if (group is null) return false;

        db.Groups.Remove(group);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateAsync(Group group)
    {
        var existing = await db.Groups
            .Include(value => value.Teams)
            .FirstOrDefaultAsync(value => value.Id == group.Id && value.TournamentId == group.TournamentId);

        if (existing is null) return;

        existing.Name = group.Name;
        existing.Teams = await db.Teams
            .Where(team => group.Teams.Select(value => value.Id).Contains(team.Id))
            .ToListAsync();
        await db.SaveChangesAsync();
    }

    private async Task EnsureTournamentExists(string tournamentId)
    {
        if (!await db.Tournaments.AnyAsync(tournament => tournament.Id == tournamentId))
            throw new NotFoundException($"Tournament {tournamentId} not found.");
    }

    private IQueryable<Group> Query() => db.Groups.Include(group => group.Teams);
}
