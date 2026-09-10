using RoundBatman.Api.Dtos;
using RoundBatman.Delegates;
using RoundBatman.Domain.Exceptions;

namespace RoundBatman.Api.Routes;

public static class GroupRoutes
{
    public static void MapGroupRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/tournaments/{tournamentId}/groups");

        group.MapGet("/", async (string tournamentId, IGroupDelegate groupDelegate) =>
            Results.Ok((await groupDelegate.GetAllAsync(tournamentId)).Select(g => g.ToDto())));

        group.MapPost("/", async (string tournamentId, CreateGroupDto dto, IGroupDelegate groupDelegate) =>
        {
            var created = await groupDelegate.CreateAsync(tournamentId, dto.Name);
            return Results.Created($"/tournaments/{tournamentId}/groups/{created.Id}", created.ToDto());
        });

        group.MapDelete("/{groupId}", async (string tournamentId, string groupId, IGroupDelegate groupDelegate) =>
            await groupDelegate.DeleteAsync(tournamentId, groupId) ? Results.NoContent() : Results.NotFound());

        group.MapPatch("/{groupId}/teams", async (
            string tournamentId, string groupId, AssignTeamsDto dto, IGroupDelegate groupDelegate) =>
        {
            try
            {
                var updated = await groupDelegate.AssignTeamsAsync(tournamentId, groupId, dto.TeamIds);
                return Results.Ok(updated.ToDto());
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        group.MapPost("/{groupId}/generate-matches", async (
            string tournamentId, string groupId, IMatchDelegate matchDelegate) =>
        {
            try
            {
                var matches = await matchDelegate.GenerateRoundRobinMatchesAsync(tournamentId, groupId);
                return Results.Created($"/tournaments/{tournamentId}/matches", matches.Select(m => m.ToDto()));
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (BusinessRuleException ex)
            {
                return Results.UnprocessableEntity(new { error = ex.Message });
            }
        });
    }
}
