using RoundBatman.Api.Dtos;
using RoundBatman.Api.Extensions;
using RoundBatman.Delegates;
using RoundBatman.Domain;

namespace RoundBatman.Api.Routes;

public static class MatchRoutes
{
    public static void MapMatchRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/tournaments/{tournamentId}/matches");

        group.MapGet("/", async (string tournamentId, IMatchDelegate matchDelegate, ITournamentDelegate tournamentDelegate) =>
        {
            var matches = await matchDelegate.GetAllAsync(tournamentId);
            var teamsById = await BuildTeamsByIdAsync(tournamentId, tournamentDelegate);

            return Results.Ok(matches.Select(m => m.ToDto(
                teamsById.GetValueOrDefault(m.HomeTeamId),
                teamsById.GetValueOrDefault(m.VisitorTeamId))));
        });

        group.MapGet("/{matchId}", async (
            string tournamentId, string matchId, IMatchDelegate matchDelegate, ITournamentDelegate tournamentDelegate) =>
        {
            var match = await matchDelegate.GetByIdAsync(tournamentId, matchId);
            if (match is null)
                return Results.NotFound();

            var teamsById = await BuildTeamsByIdAsync(tournamentId, tournamentDelegate);
            return Results.Ok(match.ToDto(
                teamsById.GetValueOrDefault(match.HomeTeamId),
                teamsById.GetValueOrDefault(match.VisitorTeamId)));
        });

        group.MapPost("/", async (
            string tournamentId, CreateMatchDto dto, IMatchDelegate matchDelegate, ITournamentDelegate tournamentDelegate) =>
        {
            var match = await matchDelegate.CreateAsync(tournamentId, dto.GroupId, dto.HomeTeamId, dto.VisitorTeamId);
            var teamsById = await BuildTeamsByIdAsync(tournamentId, tournamentDelegate);

            return Results.Created(
                $"/tournaments/{tournamentId}/matches/{match.Id}",
                match.ToDto(teamsById.GetValueOrDefault(match.HomeTeamId), teamsById.GetValueOrDefault(match.VisitorTeamId)));
        })
        .AddEndpointFilter<ValidationFilter<CreateMatchDto>>();

        group.MapPatch("/{matchId}/score", async (
            string tournamentId, string matchId, UpdateScoreDto dto,
            IMatchDelegate matchDelegate, ITournamentDelegate tournamentDelegate) =>
        {
            var updated = await matchDelegate.UpdateScoreAsync(tournamentId, matchId, dto.HomeTeamScore, dto.VisitorTeamScore);
            var teamsById = await BuildTeamsByIdAsync(tournamentId, tournamentDelegate);

            return Results.Ok(updated.ToDto(
                teamsById.GetValueOrDefault(updated.HomeTeamId),
                teamsById.GetValueOrDefault(updated.VisitorTeamId)));
        })
        .AddEndpointFilter<ValidationFilter<UpdateScoreDto>>();

        group.MapDelete("/{matchId}", async (string tournamentId, string matchId, IMatchDelegate matchDelegate) =>
            await matchDelegate.DeleteAsync(tournamentId, matchId) ? Results.NoContent() : Results.NotFound());
    }


    private static async Task<Dictionary<string, Team>> BuildTeamsByIdAsync(
        string tournamentId, ITournamentDelegate tournamentDelegate)
    {
        var tournament = await tournamentDelegate.GetByIdAsync(tournamentId);
        if (tournament is null)
            return new Dictionary<string, Team>();

        return tournament.Groups
            .SelectMany(g => g.Teams)
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.First());
    }
}
