using FluentAssertions;
using RoundBatman.Domain;
using RoundBatman.Domain.Scheduling;
using Xunit;

namespace RoundBatman.Domain.Tests;

public class RoundRobinSchedulerTests
{
    private static Team MakeTeam(string id) => new() { Id = id, Name = $"Team {id}" };

    [Fact]
    public void GenerateSchedule_WithFourTeams_ProducesSixUniquePairings()
    {
        var teams = new List<Team> { MakeTeam("t1"), MakeTeam("t2"), MakeTeam("t3"), MakeTeam("t4") };

        var schedule = RoundRobinScheduler.GenerateSchedule(teams);

        schedule.Should().HaveCount(6); // C(4,2)
    }

    [Fact]
    public void GenerateSchedule_WithFourTeams_EveryPairPlaysExactlyOnce()
    {
        var teams = new List<Team> { MakeTeam("t1"), MakeTeam("t2"), MakeTeam("t3"), MakeTeam("t4") };

        var schedule = RoundRobinScheduler.GenerateSchedule(teams);

        var pairs = schedule
            .Select(p => string.CompareOrdinal(p.HomeTeamId, p.VisitorTeamId) < 0
                ? (p.HomeTeamId, p.VisitorTeamId)
                : (p.VisitorTeamId, p.HomeTeamId))
            .ToList();

        pairs.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateSchedule_WithOddNumberOfTeams_HandlesByeCorrectly()
    {
        var teams = new List<Team> { MakeTeam("t1"), MakeTeam("t2"), MakeTeam("t3") };

        var schedule = RoundRobinScheduler.GenerateSchedule(teams);

        schedule.Should().HaveCount(3); // C(3,2)
    }

    [Fact]
    public void GenerateSchedule_WithNoTeams_ReturnsEmptyList()
    {
        var schedule = RoundRobinScheduler.GenerateSchedule([]);

        schedule.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSchedule_WithOneTeam_ReturnsEmptyList()
    {
        var teams = new List<Team> { MakeTeam("t1") };

        var schedule = RoundRobinScheduler.GenerateSchedule(teams);

        schedule.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSchedule_NoTeamPlaysItself()
    {
        var teams = new List<Team> { MakeTeam("t1"), MakeTeam("t2"), MakeTeam("t3"), MakeTeam("t4"), MakeTeam("t5") };

        var schedule = RoundRobinScheduler.GenerateSchedule(teams);

        schedule.Should().OnlyContain(p => p.HomeTeamId != p.VisitorTeamId);
    }
}
