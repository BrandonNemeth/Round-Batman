using RoundBatman.Domain;
using RoundBatman.Domain.Enums;

namespace RoundBatman.Api.Dtos;

public record ScoreDto(int HomeTeamScore, int VisitorTeamScore);
public record CreateMatchDto(string? GroupId, string HomeTeamId, string VisitorTeamId);
public record UpdateScoreDto(int HomeTeamScore, int VisitorTeamScore);

public record MatchDto(
    string Id,
    string? GroupId,
    string HomeTeamId,
    string VisitorTeamId,
    TeamDto? HomeTeam,
    TeamDto? VisitorTeam,
    ScoreDto Score,
    Winner? Winner);

public static class MatchMapper
{

    public static MatchDto ToDto(this Match match, Team? homeTeam = null, Team? visitorTeam = null) => new(
        match.Id,
        match.GroupId,
        match.HomeTeamId,
        match.VisitorTeamId,
        homeTeam?.ToDto(),
        visitorTeam?.ToDto(),
        new ScoreDto(match.Score.HomeTeamScore, match.Score.VisitorTeamScore),
        match.Winner);
}
