using RoundBatman.Api.Dtos;
using RoundBatman.Api.Extensions;
using RoundBatman.Delegates;

namespace RoundBatman.Api.Routes;

public static class MatchRoutes
{
    public static void MapMatchRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/tournaments/{tournamentId}/matches");

        group.MapGet("/", async (string tournamentId, IMatchDelegate matchDelegate) =>
            Results.Ok((await matchDelegate.GetAllAsync(tournamentId)).Select(m => m.ToDto())));

        group.MapGet("/{matchId}", async (string tournamentId, string matchId, IMatchDelegate matchDelegate) =>
        {
            var match = await matchDelegate.GetByIdAsync(tournamentId, matchId);
            return match is null ? Results.NotFound() : Results.Ok(match.ToDto());
        });

        group.MapPost("/", async (string tournamentId, CreateMatchDto dto, IMatchDelegate matchDelegate) =>
        {
            var match = await matchDelegate.CreateAsync(tournamentId, dto.GroupId, dto.HomeTeamId, dto.VisitorTeamId);
            return Results.Created($"/tournaments/{tournamentId}/matches/{match.Id}", match.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<CreateMatchDto>>();

        group.MapPatch("/{matchId}/score", async (
            string tournamentId, string matchId, UpdateScoreDto dto, IMatchDelegate matchDelegate) =>
        {
            var updated = await matchDelegate.UpdateScoreAsync(tournamentId, matchId, dto.HomeTeamScore, dto.VisitorTeamScore);
            return Results.Ok(updated.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<UpdateScoreDto>>();

        group.MapDelete("/{matchId}", async (string tournamentId, string matchId, IMatchDelegate matchDelegate) =>
            await matchDelegate.DeleteAsync(tournamentId, matchId) ? Results.NoContent() : Results.NotFound());
    }
}
