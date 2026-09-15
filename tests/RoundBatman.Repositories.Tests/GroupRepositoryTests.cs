using FluentAssertions;
using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;
using Xunit;

namespace RoundBatman.Repositories.Tests;

public class GroupRepositoryTests
{
    private static (GroupRepository sut, InMemoryDataStore store) CreateSut()
    {
        var store = new InMemoryDataStore();
        return (new GroupRepository(store), store);
    }

    private static Tournament MakeTournament(string id = "tour1") => new()
    {
        Id = id,
        Name = "Copa Arkham",
        Format = new TournamentFormat { Type = TournamentType.ROUND_ROBIN, NumberOfGroups = 1, MaxTeamsPerGroup = 4 },
    };

    [Fact]
    public async Task AddAsync_WithExistingTournament_AddsGroupToTournament()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        store.Tournaments[tournament.Id] = tournament;
        var group = new Group { Id = "g1", Name = "Grupo A" };

        group.TournamentId = tournament.Id;
        await sut.AddAsync(group);

        tournament.Groups.Should().ContainSingle(g => g.Id == "g1");
    }

    [Fact]
    public async Task AddAsync_WithMissingTournament_ThrowsNotFoundException()
    {
        var (sut, _) = CreateSut();
        var group = new Group { Id = "g1", Name = "Grupo A" };

        group.TournamentId = "missing-tournament";
        var act = async () => await sut.AddAsync(group);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingGroup_ReturnsGroup()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        tournament.Groups.Add(new Group { Id = "g1", Name = "Grupo A" });
        store.Tournaments[tournament.Id] = tournament;

        var result = await sut.GetByIdAsync(new GroupRepositoryKey(tournament.Id, "g1"));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingGroup_RemovesItAndReturnsTrue()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        tournament.Groups.Add(new Group { Id = "g1", Name = "Grupo A" });
        store.Tournaments[tournament.Id] = tournament;

        var deleted = await sut.DeleteAsync(new GroupRepositoryKey(tournament.Id, "g1"));

        deleted.Should().BeTrue();
        tournament.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingGroup_ReplacesIt()
    {
        var (sut, store) = CreateSut();
        var tournament = MakeTournament();
        var original = new Group { Id = "g1", Name = "Grupo A" };
        tournament.Groups.Add(original);
        store.Tournaments[tournament.Id] = tournament;

        var updated = new Group { Id = "g1", Name = "Grupo A", TournamentId = tournament.Id, Teams = [new Team { Id = "t1", Name = "X" }] };
        await sut.UpdateAsync(updated);

        tournament.Groups.Single().Teams.Should().HaveCount(1);
    }
}
