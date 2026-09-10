using FluentAssertions;
using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;
using Xunit;

namespace RoundBatman.Repositories.Tests;

public class MatchRepositoryTests
{
    private static (MatchRepository sut, InMemoryDataStore store) CreateSut()
    {
        var store = new InMemoryDataStore();
        return (new MatchRepository(store), store);
    }

    private static Tournament MakeTournament(string id = "tour1") => new()
    {
        Id = id,
        Name = "Copa Arkham",
        Format = new TournamentFormat { Type = TournamentType.ROUND_ROBIN, NumberOfGroups = 1, MaxTeamsPerGroup = 4 },
    };

    [Fact]
    public async Task AddAsync_WithExistingTournament_AddsMatch()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        store.Tournaments[tournament.Id] = tournament;
        var match = new Match { Id = "m1", HomeTeamId = "t1", VisitorTeamId = "t2" };

        await sut.AddAsync(tournament.Id, match);

        tournament.Matches.Should().ContainSingle(m => m.Id == "m1");
    }

    [Fact]
    public async Task AddAsync_WithMissingTournament_ThrowsNotFoundException()
    {
        var (sut, _) = CreateSut();
        var match = new Match { Id = "m1", HomeTeamId = "t1", VisitorTeamId = "t2" };

        var act = async () => await sut.AddAsync("missing", match);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddRangeAsync_AddsAllMatches()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        store.Tournaments[tournament.Id] = tournament;
        var matches = new List<Match>
        {
            new() { Id = "m1", HomeTeamId = "t1", VisitorTeamId = "t2" },
            new() { Id = "m2", HomeTeamId = "t3", VisitorTeamId = "t4" },
        };

        await sut.AddRangeAsync(tournament.Id, matches);

        tournament.Matches.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByGroupAsync_ReturnsOnlyMatchesInThatGroup()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        tournament.Matches.Add(new Match { Id = "m1", GroupId = "g1", HomeTeamId = "t1", VisitorTeamId = "t2" });
        tournament.Matches.Add(new Match { Id = "m2", GroupId = "g2", HomeTeamId = "t3", VisitorTeamId = "t4" });
        store.Tournaments[tournament.Id] = tournament;

        var result = await sut.GetByGroupAsync(tournament.Id, "g1");

        result.Should().ContainSingle(m => m.Id == "m1");
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingMatch()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        var match = new Match { Id = "m1", HomeTeamId = "t1", VisitorTeamId = "t2" };
        tournament.Matches.Add(match);
        store.Tournaments[tournament.Id] = tournament;

        match.Score.SetResult(3, 1);
        await sut.UpdateAsync(tournament.Id, match);

        tournament.Matches.Single().Score.HomeTeamScore.Should().Be(3);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingMatch_RemovesItAndReturnsTrue()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        tournament.Matches.Add(new Match { Id = "m1", HomeTeamId = "t1", VisitorTeamId = "t2" });
        store.Tournaments[tournament.Id] = tournament;

        var deleted = await sut.DeleteAsync(tournament.Id, "m1");

        deleted.Should().BeTrue();
    }
}
