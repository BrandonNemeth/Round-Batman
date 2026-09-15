using FluentAssertions;
using RoundBatman.Domain;
using RoundBatman.Domain.Enums;
using Xunit;

namespace RoundBatman.Domain.Tests;

public class TournamentStatusTests
{
    private static Tournament MakeTournament()
    {
        var group = new Group { Id = "g1", Name = "Grupo A", TournamentId = "tour1" };
        return new Tournament
        {
            Id = "tour1",
            Name = "Copa Arkham",
            Format = new TournamentFormat
            {
                Type = TournamentType.ROUND_ROBIN,
                NumberOfGroups = 1,
                MaxTeamsPerGroup = 4,
            },
            Groups = [group],
        };
    }

    [Fact]
    public void Status_WithoutMatches_IsNotStarted()
    {
        var tournament = MakeTournament();

        tournament.Status.Should().Be(TournamentStatus.NOT_STARTED);
    }

    [Fact]
    public void Status_WithUnfinishedMatches_IsInProgress()
    {
        var tournament = MakeTournament();
        tournament.Matches.Add(new Match { Id = "m1", GroupId = "g1" });

        tournament.Status.Should().Be(TournamentStatus.IN_PROGRESS);
    }

    [Fact]
    public void Status_WhenEveryGroupMatchHasAResult_IsFinished()
    {
        var tournament = MakeTournament();
        var match = new Match { Id = "m1", GroupId = "g1" };
        match.Score.SetResult(2, 1);
        tournament.Matches.Add(match);

        tournament.Status.Should().Be(TournamentStatus.FINISHED);
    }

    [Fact]
    public void Status_WithRecordedDraw_IsFinished()
    {
        var tournament = MakeTournament();
        var match = new Match { Id = "m1", GroupId = "g1" };
        match.Score.SetResult(0, 0);
        tournament.Matches.Add(match);

        match.IsCompleted.Should().BeTrue();
        tournament.Status.Should().Be(TournamentStatus.FINISHED);
    }

    [Fact]
    public void Status_WhenConfiguredGroupHasNoMatches_IsInProgress()
    {
        var tournament = MakeTournament();
        tournament.Format.NumberOfGroups = 2;
        tournament.Groups.Add(new Group { Id = "g2", Name = "Grupo B", TournamentId = "tour1" });
        var match = new Match { Id = "m1", GroupId = "g1" };
        match.Score.SetResult(1, 0);
        tournament.Matches.Add(match);

        tournament.Status.Should().Be(TournamentStatus.IN_PROGRESS);
    }
}
