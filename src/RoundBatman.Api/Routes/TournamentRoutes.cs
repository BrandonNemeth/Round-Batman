using RoundBatman.Api.Dtos;
using RoundBatman.Api.Extensions;
using RoundBatman.Delegates;

namespace RoundBatman.Api.Routes;

public static class TournamentRoutes
{
    public static void MapTournamentRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/tournaments");

        group.MapGet("/", async (ITournamentDelegate tournamentDelegate) =>
            Results.Ok((await tournamentDelegate.GetAllAsync()).Select(t => t.ToDto())));

        group.MapGet("/{id}", async (string id, ITournamentDelegate tournamentDelegate) =>
        {
            var tournament = await tournamentDelegate.GetByIdAsync(id);
            return tournament is null ? Results.NotFound() : Results.Ok(tournament.ToDto());
        });

        group.MapPost("/", async (CreateTournamentDto dto, ITournamentDelegate tournamentDelegate) =>
        {
            var tournament = await tournamentDelegate.CreateAsync(
                dto.Name, dto.Format.Type, dto.Format.NumberOfGroups, dto.Format.MaxTeamsPerGroup);
            return Results.Created($"/tournaments/{tournament.Id}", tournament.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<CreateTournamentDto>>();

        group.MapPut("/{id}", async (string id, UpdateTournamentDto dto, ITournamentDelegate tournamentDelegate) =>
        {
            var updated = await tournamentDelegate.UpdateAsync(
                id, dto.Name, dto.Format.Type, dto.Format.NumberOfGroups, dto.Format.MaxTeamsPerGroup);
            return Results.Ok(updated.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<UpdateTournamentDto>>();

        group.MapPatch("/{id}", async (string id, PatchTournamentDto dto, ITournamentDelegate tournamentDelegate) =>
        {
            var updated = await tournamentDelegate.PatchAsync(
                id, dto.Name, dto.Format?.Type, dto.Format?.NumberOfGroups, dto.Format?.MaxTeamsPerGroup);
            return Results.Ok(updated.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<PatchTournamentDto>>();

        group.MapDelete("/{id}", async (string id, ITournamentDelegate tournamentDelegate) =>
            await tournamentDelegate.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());
    }
}
