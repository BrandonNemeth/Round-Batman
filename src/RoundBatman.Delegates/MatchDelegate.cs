using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Domain.Scheduling;
using RoundBatman.Repositories;

namespace RoundBatman.Delegates;

public class MatchDelegate(
    IMatchRepository matchRepository,
    IGroupRepository groupRepository,
    ITournamentRepository? tournamentRepository = null) : IMatchDelegate
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

        if (match.Stage == MatchStage.KNOCKOUT && homeScore == visitorScore)
            throw new BusinessRuleException("Knockout matches cannot end in a draw.");

        match.Score.SetResult(homeScore, visitorScore);
        await matchRepository.UpdateAsync(tournamentId, match);
        await AdvanceTournamentAsync(tournamentId);
        return match;
    }

    public async Task<List<Match>> SimulatePendingResultsAsync(
        string tournamentId, int? seed = null, int maxGoals = 5)
    {
        if (maxGoals is < 1 or > 20)
            throw new BusinessRuleException("Max goals must be between 1 and 20.");

        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var processed = new HashSet<string>();

        while (true)
        {
            var pending = (await matchRepository.GetAllAsync(tournamentId))
                .Where(match => !match.IsCompleted && !match.IsBye && processed.Add(match.Id))
                .ToList();
            if (pending.Count == 0)
                break;

            foreach (var match in pending)
            {
                var homeScore = random.Next(maxGoals + 1);
                var visitorScore = random.Next(maxGoals + 1);
                if (match.Stage == MatchStage.KNOCKOUT && homeScore == visitorScore)
                    visitorScore = (visitorScore + 1) % (maxGoals + 1);

                await UpdateScoreAsync(tournamentId, match.Id, homeScore, visitorScore);
            }
        }

        return await matchRepository.GetAllAsync(tournamentId);
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
            Stage = MatchStage.GROUP,
            Round = 1,
        }).ToList();

        return await matchRepository.AddRangeAsync(tournamentId, matches);
    }

    private async Task AdvanceTournamentAsync(string tournamentId)
    {
        if (tournamentRepository is null)
            return;

        var tournament = await tournamentRepository.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Tournament {tournamentId} not found.");
        var matches = await matchRepository.GetAllAsync(tournamentId);
        var groupMatches = matches.Where(match => match.Stage == MatchStage.GROUP).ToList();

        if (tournament.Groups.Count != tournament.Format.NumberOfGroups ||
            groupMatches.Count == 0 ||
            groupMatches.Any(match => !match.IsCompleted) ||
            tournament.Groups.Any(group => !groupMatches.Any(match => match.GroupId == group.Id)))
            return;

        var knockoutMatches = matches.Where(match => match.Stage == MatchStage.KNOCKOUT).ToList();
        if (knockoutMatches.Count == 0)
        {
            var qualifiers = GetQualifiers(tournament.Groups, groupMatches);
            if (qualifiers.Count <= 1)
            {
                tournament.ChampionTeamId = qualifiers.SingleOrDefault()?.Id;
                await tournamentRepository.UpdateAsync(tournament);
                return;
            }

            await CreateKnockoutRoundAsync(tournamentId, qualifiers.Select(team => team.Id).ToList(), 1);
            return;
        }

        var currentRound = knockoutMatches.Max(match => match.Round);
        var currentMatches = knockoutMatches.Where(match => match.Round == currentRound).ToList();
        if (currentMatches.Any(match => !match.IsCompleted))
            return;

        var winners = currentMatches
            .Select(match => match.Winner is Winner.HOME ? match.HomeTeamId : match.VisitorTeamId)
            .ToList();

        if (winners.Count == 1)
        {
            tournament.ChampionTeamId = winners[0];
            await tournamentRepository.UpdateAsync(tournament);
            return;
        }

        await CreateKnockoutRoundAsync(tournamentId, winners, currentRound + 1);
    }

    private async Task CreateKnockoutRoundAsync(string tournamentId, List<string> teamIds, int round)
    {
        var matches = new List<Match>();
        for (var index = 0; index + 1 < teamIds.Count; index += 2)
        {
            matches.Add(new Match
            {
                Id = Guid.NewGuid().ToString(),
                TournamentId = tournamentId,
                HomeTeamId = teamIds[index],
                VisitorTeamId = teamIds[index + 1],
                Stage = MatchStage.KNOCKOUT,
                Round = round,
            });
        }

        if (teamIds.Count % 2 == 1)
        {
            var byeTeamId = teamIds[^1];
            var bye = new Match
            {
                Id = Guid.NewGuid().ToString(),
                TournamentId = tournamentId,
                HomeTeamId = byeTeamId,
                VisitorTeamId = byeTeamId,
                Stage = MatchStage.KNOCKOUT,
                Round = round,
                IsBye = true,
            };
            bye.Score.SetResult(1, 0);
            matches.Add(bye);
        }

        if (matches.Count > 0)
            await matchRepository.AddRangeAsync(tournamentId, matches);
    }

    private static List<Team> GetQualifiers(List<Group> groups, List<Match> matches)
    {
        var qualifiers = new List<Team>();
        foreach (var group in groups)
        {
            var stats = group.Teams.Select(team =>
            {
                var teamMatches = matches.Where(match =>
                    match.GroupId == group.Id &&
                    (match.HomeTeamId == team.Id || match.VisitorTeamId == team.Id));
                var points = 0;
                var goalsFor = 0;
                var goalsAgainst = 0;

                foreach (var match in teamMatches)
                {
                    var isHome = match.HomeTeamId == team.Id;
                    var scored = isHome ? match.Score.HomeTeamScore : match.Score.VisitorTeamScore;
                    var conceded = isHome ? match.Score.VisitorTeamScore : match.Score.HomeTeamScore;
                    goalsFor += scored;
                    goalsAgainst += conceded;
                    points += scored == conceded ? 1 : scored > conceded ? 3 : 0;
                }

                return new { Team = team, Points = points, GoalDifference = goalsFor - goalsAgainst, GoalsFor = goalsFor };
            })
            .OrderByDescending(stat => stat.Points)
            .ThenByDescending(stat => stat.GoalDifference)
            .ThenByDescending(stat => stat.GoalsFor)
            .ThenBy(stat => stat.Team.Id)
            .Take(2)
            .Select(stat => stat.Team);

            qualifiers.AddRange(stats);
        }

        return qualifiers;
    }
}
