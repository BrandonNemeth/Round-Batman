using FluentAssertions;
using RoundBatman.Domain;
using RoundBatman.Repositories;
using Xunit;

namespace RoundBatman.Repositories.Tests;

public class TeamRepositoryTests
{
    private static TeamRepository CreateSut(out InMemoryDataStore store)
    {
        store = new InMemoryDataStore();
        return new TeamRepository(store);
    }

    [Fact]
    public async Task AddAsync_StoresTeam_AndReturnsIt()
    {
        var sut = CreateSut(out _);
        var team = new Team { Id = "t1", Name = "Gothica FC" };

        var result = await sut.AddAsync(team);

        result.Should().Be(team);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllStoredTeams()
    {
        var sut = CreateSut(out _);
        await sut.AddAsync(new Team { Id = "t1", Name = "A" });
        await sut.AddAsync(new Team { Id = "t2", Name = "B" });

        var result = await sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsTeam()
    {
        var sut = CreateSut(out _);
        await sut.AddAsync(new Team { Id = "t1", Name = "A" });

        var result = await sut.GetByIdAsync("t1");

        result.Should().NotBeNull();
        result!.Name.Should().Be("A");
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingId_ReturnsNull()
    {
        var sut = CreateSut(out _);

        var result = await sut.GetByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_RemovesTeamAndReturnsTrue()
    {
        var sut = CreateSut(out _);
        await sut.AddAsync(new Team { Id = "t1", Name = "A" });

        var deleted = await sut.DeleteAsync("t1");

        deleted.Should().BeTrue();
        (await sut.GetByIdAsync("t1")).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithMissingId_ReturnsFalse()
    {
        var sut = CreateSut(out _);

        var deleted = await sut.DeleteAsync("missing");

        deleted.Should().BeFalse();
    }
}
