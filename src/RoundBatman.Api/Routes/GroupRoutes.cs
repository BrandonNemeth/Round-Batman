using RoundBatman.Api.Dtos;
using RoundBatman.Api.Extensions;
using RoundBatman.Delegates;

namespace RoundBatman.Api.Routes;

public static class GroupRoutes
{
    public static void MapGroupRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/tournaments/{tournamentId}/groups");

        group.MapGet("/", async (string tournamentId, IGroupDelegate groupDelegate) =>
            Results.Ok((await groupDelegate.GetAllAsync(tournamentId)).Select(g => g.ToDto())));

        group.MapGet("/{groupId}", async (string tournamentId, string groupId, IGroupDelegate groupDelegate) =>
        {
            var found = await groupDelegate.GetByIdAsync(tournamentId, groupId);
            return found is null ? Results.NotFound() : Results.Ok(found.ToDto());
        });

        group.MapPost("/", async (string tournamentId, CreateGroupDto dto, IGroupDelegate groupDelegate) =>
        {
            var created = await groupDelegate.CreateAsync(tournamentId, dto.Name);
            return Results.Created($"/tournaments/{tournamentId}/groups/{created.Id}", created.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<CreateGroupDto>>();

        group.MapPut("/{groupId}", async (
            string tournamentId, string groupId, UpdateGroupDto dto, IGroupDelegate groupDelegate) =>
        {
            var updated = await groupDelegate.UpdateAsync(tournamentId, groupId, dto.Name);
            return Results.Ok(updated.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<UpdateGroupDto>>();

        group.MapDelete("/{groupId}", async (string tournamentId, string groupId, IGroupDelegate groupDelegate) =>
            await groupDelegate.DeleteAsync(tournamentId, groupId) ? Results.NoContent() : Results.NotFound());

        group.MapPatch("/{groupId}/teams", async (
            string tournamentId, string groupId, AssignTeamsDto dto, IGroupDelegate groupDelegate) =>
        {
            await groupDelegate.AssignTeamsAsync(tournamentId, groupId, dto.TeamIds);
            return Results.NoContent();
        })
        .AddEndpointFilter<ValidationFilter<AssignTeamsDto>>();

        group.MapPost("/{groupId}/generate-matches", async (
            string tournamentId, string groupId, IMatchDelegate matchDelegate) =>
        {
            var matches = await matchDelegate.GenerateRoundRobinMatchesAsync(tournamentId, groupId);
            return Results.Created($"/tournaments/{tournamentId}/matches", matches.Select(m => m.ToDto()));
        });
    }
}
