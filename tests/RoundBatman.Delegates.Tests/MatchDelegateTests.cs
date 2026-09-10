using FluentAssertions;
using Moq;
using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;
using Xunit;
using DomainMatch = RoundBatman.Domain.Match;

namespace RoundBatman.Delegates.Tests;

public class MatchDelegateTests
{
    [Fact]
    public async Task CreateAsync_BuildsMatch_AndPersists()
    {
        var matchRepo = new Mock<IMatchRepository>();
        matchRepo.Setup(r => r.AddAsync("tour1", It.IsAny<DomainMatch>())).ReturnsAsync((string _, DomainMatch m) => m);
        var groupRepo = new Mock<IGroupRepository>();
        var sut = new MatchDelegate(matchRepo.Object, groupRepo.Object);

        var result = await sut.CreateAsync("tour1", "g1", "t1", "t2");

        result.HomeTeamId.Should().Be("t1");
        result.VisitorTeamId.Should().Be("t2");
    }

    [Fact]
    public async Task UpdateScoreAsync_WithExistingMatch_SetsScoreAndPersists()
    {
        var match = new DomainMatch { Id = "m1", HomeTeamId = "t1", VisitorTeamId = "t2" };
        var matchRepo = new Mock<IMatchRepository>();
        matchRepo.Setup(r => r.GetByIdAsync("tour1", "m1")).ReturnsAsync(match);
        var groupRepo = new Mock<IGroupRepository>();
        var sut = new MatchDelegate(matchRepo.Object, groupRepo.Object);

        var result = await sut.UpdateScoreAsync("tour1", "m1", 3, 1);

        result.Score.HomeTeamScore.Should().Be(3);
        matchRepo.Verify(r => r.UpdateAsync("tour1", match), Times.Once);
    }

    [Fact]
    public async Task UpdateScoreAsync_WithMissingMatch_ThrowsNotFoundException()
    {
        var matchRepo = new Mock<IMatchRepository>();
        matchRepo.Setup(r => r.GetByIdAsync("tour1", "missing")).ReturnsAsync((DomainMatch?)null);
        var groupRepo = new Mock<IGroupRepository>();
        var sut = new MatchDelegate(matchRepo.Object, groupRepo.Object);

        var act = async () => await sut.UpdateScoreAsync("tour1", "missing", 1, 0);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GenerateRoundRobinMatchesAsync_WithFourTeams_GeneratesSixMatches()
    {
        var group = new Group
        {
            Id = "g1",
            Teams =
            [
                new Team { Id = "t1", Name = "A" },
                new Team { Id = "t2", Name = "B" },
                new Team { Id = "t3", Name = "C" },
                new Team { Id = "t4", Name = "D" },
            ],
        };
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(group);

        var matchRepo = new Mock<IMatchRepository>();
        matchRepo.Setup(r => r.GetByGroupAsync("tour1", "g1")).ReturnsAsync(new List<DomainMatch>());
        matchRepo.Setup(r => r.AddRangeAsync("tour1", It.IsAny<List<DomainMatch>>()))
            .ReturnsAsync((string _, List<DomainMatch> m) => m);

        var sut = new MatchDelegate(matchRepo.Object, groupRepo.Object);

        var result = await sut.GenerateRoundRobinMatchesAsync("tour1", "g1");

        result.Should().HaveCount(6);
    }

    [Fact]
    public async Task GenerateRoundRobinMatchesAsync_WithLessThanTwoTeams_ThrowsBusinessRuleException()
    {
        var group = new Group { Id = "g1", Teams = [new Team { Id = "t1", Name = "A" }] };
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(group);
        var matchRepo = new Mock<IMatchRepository>();

        var sut = new MatchDelegate(matchRepo.Object, groupRepo.Object);

        var act = async () => await sut.GenerateRoundRobinMatchesAsync("tour1", "g1");

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task GenerateRoundRobinMatchesAsync_WhenMatchesAlreadyExist_ThrowsBusinessRuleException()
    {
        var group = new Group
        {
            Id = "g1",
            Teams = [new Team { Id = "t1", Name = "A" }, new Team { Id = "t2", Name = "B" }],
        };
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(group);

        var matchRepo = new Mock<IMatchRepository>();
        matchRepo.Setup(r => r.GetByGroupAsync("tour1", "g1"))
            .ReturnsAsync(new List<DomainMatch> { new() { Id = "m1", HomeTeamId = "t1", VisitorTeamId = "t2" } });

        var sut = new MatchDelegate(matchRepo.Object, groupRepo.Object);

        var act = async () => await sut.GenerateRoundRobinMatchesAsync("tour1", "g1");

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task GenerateRoundRobinMatchesAsync_WithMissingGroup_ThrowsNotFoundException()
    {
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "missing")).ReturnsAsync((Group?)null);
        var matchRepo = new Mock<IMatchRepository>();

        var sut = new MatchDelegate(matchRepo.Object, groupRepo.Object);

        var act = async () => await sut.GenerateRoundRobinMatchesAsync("tour1", "missing");

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
