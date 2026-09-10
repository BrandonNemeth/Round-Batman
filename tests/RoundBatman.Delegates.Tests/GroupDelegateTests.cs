using FluentAssertions;
using Moq;
using RoundBatman.Domain;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;
using Xunit;

namespace RoundBatman.Delegates.Tests;

public class GroupDelegateTests
{
    [Fact]
    public async Task CreateAsync_BuildsGroup_AndPersists()
    {
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.AddAsync("tour1", It.IsAny<Group>())).ReturnsAsync((string _, Group g) => g);
        var teamRepo = new Mock<ITeamRepository>();
        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object);

        var result = await sut.CreateAsync("tour1", "Grupo A");

        result.Name.Should().Be("Grupo A");
        result.TournamentId.Should().Be("tour1");
    }

    [Fact]
    public async Task AssignTeamsAsync_WithValidTeams_UpdatesGroup()
    {
        var group = new Group { Id = "g1", Name = "Grupo A", TournamentId = "tour1" };
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(group);

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdAsync("t1")).ReturnsAsync(new Team { Id = "t1", Name = "A" });
        teamRepo.Setup(r => r.GetByIdAsync("t2")).ReturnsAsync(new Team { Id = "t2", Name = "B" });

        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object);

        var result = await sut.AssignTeamsAsync("tour1", "g1", ["t1", "t2"]);

        result.Teams.Should().HaveCount(2);
        groupRepo.Verify(r => r.UpdateAsync("tour1", It.IsAny<Group>()), Times.Once);
    }

    [Fact]
    public async Task AssignTeamsAsync_WithMissingGroup_ThrowsNotFoundException()
    {
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "missing")).ReturnsAsync((Group?)null);
        var teamRepo = new Mock<ITeamRepository>();
        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object);

        var act = async () => await sut.AssignTeamsAsync("tour1", "missing", ["t1"]);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignTeamsAsync_WithMissingTeam_ThrowsNotFoundException()
    {
        var group = new Group { Id = "g1", Name = "Grupo A" };
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(group);

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdAsync("missing-team")).ReturnsAsync((Team?)null);

        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object);

        var act = async () => await sut.AssignTeamsAsync("tour1", "g1", ["missing-team"]);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
