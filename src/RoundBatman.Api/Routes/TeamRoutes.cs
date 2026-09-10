using RoundBatman.Api.Dtos;
using RoundBatman.Delegates;

namespace RoundBatman.Api.Routes;

public static class TeamRoutes
{
    public static void MapTeamRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/teams");

        group.MapGet("/", async (ITeamDelegate teamDelegate) =>
            Results.Ok((await teamDelegate.GetAllAsync()).Select(t => t.ToDto())));

        group.MapPost("/", async (CreateTeamDto dto, ITeamDelegate teamDelegate) =>
        {
            var team = await teamDelegate.CreateAsync(dto.Name);
            return Results.Created($"/teams/{team.Id}", team.ToDto());
        });

        group.MapDelete("/{id}", async (string id, ITeamDelegate teamDelegate) =>
            await teamDelegate.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());
    }
}
