using FluentAssertions;
using Moq;
using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Repositories;
using Xunit;

namespace RoundBatman.Delegates.Tests;

public class TournamentDelegateTests
{
    [Fact]
    public async Task CreateAsync_BuildsCorrectFormat_AndPersists()
    {
        var repoMock = new Mock<ITournamentRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Tournament>())).ReturnsAsync((Tournament t) => t);
        var sut = new TournamentDelegate(repoMock.Object);

        var result = await sut.CreateAsync("Copa Arkham", TournamentType.ROUND_ROBIN, 2, 4);

        result.Name.Should().Be("Copa Arkham");
        result.Format.Type.Should().Be(TournamentType.ROUND_ROBIN);
        result.Format.NumberOfGroups.Should().Be(2);
        result.Format.MaxTeamsPerGroup.Should().Be(4);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingId_ReturnsNull()
    {
        var repoMock = new Mock<ITournamentRepository>();
        repoMock.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((Tournament?)null);
        var sut = new TournamentDelegate(repoMock.Object);

        var result = await sut.GetByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        var repoMock = new Mock<ITournamentRepository>();
        repoMock.Setup(r => r.DeleteAsync("t1")).ReturnsAsync(true);
        var sut = new TournamentDelegate(repoMock.Object);

        var result = await sut.DeleteAsync("t1");

        result.Should().BeTrue();
    }
}
