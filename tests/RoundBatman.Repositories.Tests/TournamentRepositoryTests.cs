using FluentAssertions;
using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Repositories;
using Xunit;

namespace RoundBatman.Repositories.Tests;

public class TournamentRepositoryTests
{
    private static TournamentRepository CreateSut() => new(new InMemoryDataStore());

    private static Tournament MakeTournament(string id = "tour1") => new()
    {
        Id = id,
        Name = "Copa Arkham",
        Format = new TournamentFormat { Type = TournamentType.ROUND_ROBIN, NumberOfGroups = 1, MaxTeamsPerGroup = 4 },
    };

    [Fact]
    public async Task AddAsync_StoresTournament_AndReturnsIt()
    {
        var sut = CreateSut();
        var tournament = MakeTournament();

        var result = await sut.AddAsync(tournament);

        result.Should().Be(tournament);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllStoredTournaments()
    {
        var sut = CreateSut();
        await sut.AddAsync(MakeTournament("t1"));
        await sut.AddAsync(MakeTournament("t2"));

        var result = await sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingId_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.GetByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_RemovesTournament()
    {
        var sut = CreateSut();
        await sut.AddAsync(MakeTournament("t1"));

        var deleted = await sut.DeleteAsync("t1");

        deleted.Should().BeTrue();
    }
}
