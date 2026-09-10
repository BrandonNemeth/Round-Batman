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
    ScoreDto Score,
    Winner? Winner);

public static class MatchMapper
{
    public static MatchDto ToDto(this RoundBatman.Domain.Match match) => new(
        match.Id,
        match.GroupId,
        match.HomeTeamId,
        match.VisitorTeamId,
        new ScoreDto(match.Score.HomeTeamScore, match.Score.VisitorTeamScore),
        match.Winner);
}
