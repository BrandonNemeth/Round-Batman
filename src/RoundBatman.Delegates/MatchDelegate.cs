using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Domain.Scheduling;
using RoundBatman.Repositories;

namespace RoundBatman.Delegates;

public class MatchDelegate(IMatchRepository matchRepository, IGroupRepository groupRepository) : IMatchDelegate
{
    public Task<List<Match>> GetAllAsync(string tournamentId) => matchRepository.GetAllAsync(tournamentId);

    public Task<Match?> GetByIdAsync(string tournamentId, string matchId) =>
        matchRepository.GetByIdAsync(tournamentId, matchId);

    public async Task<Match> CreateAsync(string tournamentId, string? groupId, string homeTeamId, string visitorTeamId)
    {
        if (homeTeamId == visitorTeamId)
            throw new BusinessRuleException("A team cannot play against itself.");

        if (groupId is not null)
        {
            var group = await groupRepository.GetByIdAsync(new GroupRepositoryKey(tournamentId, groupId))
                ?? throw new NotFoundException($"Group {groupId} not found in tournament {tournamentId}.");

            var groupTeamIds = group.Teams.Select(t => t.Id).ToHashSet();
            if (!groupTeamIds.Contains(homeTeamId) || !groupTeamIds.Contains(visitorTeamId))
                throw new BusinessRuleException("Both teams must belong to the specified group.");
        }
        else
        {
            var allGroups = await groupRepository.GetAllAsync(tournamentId);
            var tournamentTeamIds = allGroups.SelectMany(g => g.Teams).Select(t => t.Id).ToHashSet();

            if (!tournamentTeamIds.Contains(homeTeamId) || !tournamentTeamIds.Contains(visitorTeamId))
                throw new BusinessRuleException("Both teams must belong to a group in this tournament.");
        }

        var match = new Match
        {
            Id = Guid.NewGuid().ToString(),
            TournamentId = tournamentId,
            GroupId = groupId,
            HomeTeamId = homeTeamId,
            VisitorTeamId = visitorTeamId,
        };
        return await matchRepository.AddAsync(tournamentId, match);
    }

    public Task<bool> DeleteAsync(string tournamentId, string matchId) =>
        matchRepository.DeleteAsync(tournamentId, matchId);

    public async Task<Match> UpdateScoreAsync(string tournamentId, string matchId, int homeScore, int visitorScore)
    {
        var match = await matchRepository.GetByIdAsync(tournamentId, matchId)
            ?? throw new NotFoundException($"Match {matchId} not found in tournament {tournamentId}.");

        match.Score.SetResult(homeScore, visitorScore);
        await matchRepository.UpdateAsync(tournamentId, match);
        return match;
    }

    public async Task<List<Match>> GenerateRoundRobinMatchesAsync(string tournamentId, string groupId)
    {
        var group = await groupRepository.GetByIdAsync(new GroupRepositoryKey(tournamentId, groupId))
            ?? throw new NotFoundException($"Group {groupId} not found in tournament {tournamentId}.");

        if (group.Teams.Count < 2)
            throw new BusinessRuleException("Group must have at least 2 teams to generate a schedule.");

        var existingMatches = await matchRepository.GetByGroupAsync(tournamentId, groupId);
        if (existingMatches.Count > 0)
            throw new BusinessRuleException("Matches have already been generated for this group.");

        var pairings = RoundRobinScheduler.GenerateSchedule(group.Teams);

        var matches = pairings.Select(p => new Match
        {
            Id = Guid.NewGuid().ToString(),
            TournamentId = tournamentId,
            GroupId = groupId,
            HomeTeamId = p.HomeTeamId,
            VisitorTeamId = p.VisitorTeamId,
        }).ToList();

        return await matchRepository.AddRangeAsync(tournamentId, matches);
    }
}
