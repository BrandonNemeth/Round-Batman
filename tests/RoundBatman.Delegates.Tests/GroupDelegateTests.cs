using FluentAssertions;
using Moq;
using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using RoundBatman.Domain.Exceptions;
using RoundBatman.Repositories;
using Xunit;

namespace RoundBatman.Delegates.Tests;

public class GroupDelegateTests
{
    private static Tournament MakeTournament(int maxTeamsPerGroup = 10) => new()
    {
        Id = "tour1",
        Name = "Copa Arkham",
        Format = new TournamentFormat { Type = TournamentType.ROUND_ROBIN, NumberOfGroups = 1, MaxTeamsPerGroup = maxTeamsPerGroup },
    };

    [Fact]
    public async Task CreateAsync_BuildsGroup_AndPersists()
    {
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetAllAsync("tour1")).ReturnsAsync(new List<Group>());
        groupRepo.Setup(r => r.AddAsync("tour1", It.IsAny<Group>())).ReturnsAsync((string _, Group g) => g);
        var teamRepo = new Mock<ITeamRepository>();
        var tournamentRepo = new Mock<ITournamentRepository>();
        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object, tournamentRepo.Object);

        var result = await sut.CreateAsync("tour1", "Grupo A");

        result.Name.Should().Be("Grupo A");
        result.TournamentId.Should().Be("tour1");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsBusinessRuleException()
    {
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetAllAsync("tour1"))
            .ReturnsAsync(new List<Group> { new() { Id = "g1", Name = "Grupo A" } });
        var teamRepo = new Mock<ITeamRepository>();
        var tournamentRepo = new Mock<ITournamentRepository>();
        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object, tournamentRepo.Object);

        var act = async () => await sut.CreateAsync("tour1", "Grupo A");

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task AssignTeamsAsync_WithValidTeams_UpdatesGroup()
    {
        var group = new Group { Id = "g1", Name = "Grupo A", TournamentId = "tour1" };
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(group);
        groupRepo.Setup(r => r.GetAllAsync("tour1")).ReturnsAsync(new List<Group> { group });

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdAsync("t1")).ReturnsAsync(new Team { Id = "t1", Name = "A" });
        teamRepo.Setup(r => r.GetByIdAsync("t2")).ReturnsAsync(new Team { Id = "t2", Name = "B" });

        var tournamentRepo = new Mock<ITournamentRepository>();
        tournamentRepo.Setup(r => r.GetByIdAsync("tour1")).ReturnsAsync(MakeTournament());

        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object, tournamentRepo.Object);

        var result = await sut.AssignTeamsAsync("tour1", "g1", new List<string> { "t1", "t2" });

        result.Teams.Should().HaveCount(2);
        groupRepo.Verify(r => r.UpdateAsync("tour1", It.IsAny<Group>()), Times.Once);
    }

    [Fact]
    public async Task AssignTeamsAsync_WithMissingGroup_ThrowsNotFoundException()
    {
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "missing")).ReturnsAsync((Group?)null);
        var teamRepo = new Mock<ITeamRepository>();
        var tournamentRepo = new Mock<ITournamentRepository>();
        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object, tournamentRepo.Object);

        var act = async () => await sut.AssignTeamsAsync("tour1", "missing", new List<string> { "t1" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignTeamsAsync_WithMissingTeam_ThrowsNotFoundException()
    {
        var group = new Group { Id = "g1", Name = "Grupo A" };
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(group);
        groupRepo.Setup(r => r.GetAllAsync("tour1")).ReturnsAsync(new List<Group> { group });

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdAsync("missing-team")).ReturnsAsync((Team?)null);

        var tournamentRepo = new Mock<ITournamentRepository>();
        tournamentRepo.Setup(r => r.GetByIdAsync("tour1")).ReturnsAsync(MakeTournament());

        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object, tournamentRepo.Object);

        var act = async () => await sut.AssignTeamsAsync("tour1", "g1", new List<string> { "missing-team" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignTeamsAsync_ExceedingMaxTeamsPerGroup_ThrowsBusinessRuleException()
    {
        var group = new Group { Id = "g1", Name = "Grupo A" };
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(group);

        var teamRepo = new Mock<ITeamRepository>();
        var tournamentRepo = new Mock<ITournamentRepository>();
        tournamentRepo.Setup(r => r.GetByIdAsync("tour1")).ReturnsAsync(MakeTournament(maxTeamsPerGroup: 1));

        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object, tournamentRepo.Object);

        var act = async () => await sut.AssignTeamsAsync("tour1", "g1", new List<string> { "t1", "t2" });

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task AssignTeamsAsync_TeamAlreadyInAnotherGroup_ThrowsBusinessRuleException()
    {
        var targetGroup = new Group { Id = "g1", Name = "Grupo A" };
        var otherGroup = new Group
        {
            Id = "g2",
            Name = "Grupo B",
            Teams = new List<Team> { new() { Id = "t1", Name = "A" } },
        };

        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetByIdAsync("tour1", "g1")).ReturnsAsync(targetGroup);
        groupRepo.Setup(r => r.GetAllAsync("tour1")).ReturnsAsync(new List<Group> { targetGroup, otherGroup });

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdAsync("t1")).ReturnsAsync(new Team { Id = "t1", Name = "A" });

        var tournamentRepo = new Mock<ITournamentRepository>();
        tournamentRepo.Setup(r => r.GetByIdAsync("tour1")).ReturnsAsync(MakeTournament());

        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object, tournamentRepo.Object);

        var act = async () => await sut.AssignTeamsAsync("tour1", "g1", new List<string> { "t1" });

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task AssignTeamsAsync_WithDuplicateTeamIdsInRequest_ThrowsBusinessRuleException()
    {
        var groupRepo = new Mock<IGroupRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var tournamentRepo = new Mock<ITournamentRepository>();
        var sut = new GroupDelegate(groupRepo.Object, teamRepo.Object, tournamentRepo.Object);

        var act = async () => await sut.AssignTeamsAsync("tour1", "g1", new List<string> { "t1", "t1" });

        await act.Should().ThrowAsync<BusinessRuleException>();
    }
}
