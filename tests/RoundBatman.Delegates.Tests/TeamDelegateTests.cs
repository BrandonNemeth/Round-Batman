using FluentAssertions;
using Moq;
using RoundBatman.Domain;
using RoundBatman.Repositories;
using Xunit;

namespace RoundBatman.Delegates.Tests;

public class TeamDelegateTests
{
    [Fact]
    public async Task CreateAsync_GeneratesId_AndCallsRepositoryAdd()
    {
        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Team>()))
            .ReturnsAsync((Team t) => t);
        var sut = new TeamDelegate(repoMock.Object);

        var result = await sut.CreateAsync("Gothica FC");

        result.Name.Should().Be("Gothica FC");
        result.Id.Should().NotBeNullOrWhiteSpace();
        repoMock.Verify(r => r.AddAsync(It.Is<Team>(t => t.Name == "Gothica FC")), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Team { Id = "t1", Name = "A" }]);
        var sut = new TeamDelegate(repoMock.Object);

        var result = await sut.GetAllAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository_AndReturnsResult()
    {
        var repoMock = new Mock<ITeamRepository>();
        repoMock.Setup(r => r.DeleteAsync("t1")).ReturnsAsync(true);
        var sut = new TeamDelegate(repoMock.Object);

        var result = await sut.DeleteAsync("t1");

        result.Should().BeTrue();
    }
}
