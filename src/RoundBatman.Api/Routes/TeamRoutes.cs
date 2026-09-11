using RoundBatman.Api.Dtos;
using RoundBatman.Api.Extensions;
using RoundBatman.Delegates;

namespace RoundBatman.Api.Routes;

public static class TeamRoutes
{
    public static void MapTeamRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/teams");

        group.MapGet("/", async (ITeamDelegate teamDelegate) =>
            Results.Ok((await teamDelegate.GetAllAsync()).Select(t => t.ToDto())));

        group.MapGet("/{id}", async (string id, ITeamDelegate teamDelegate) =>
        {
            var team = await teamDelegate.GetByIdAsync(id);
            return team is null ? Results.NotFound() : Results.Ok(team.ToDto());
        });

        group.MapPost("/", async (CreateTeamDto dto, ITeamDelegate teamDelegate) =>
        {
            var team = await teamDelegate.CreateAsync(dto.Name);
            return Results.Created($"/teams/{team.Id}", team.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<CreateTeamDto>>();

        group.MapPut("/{id}", async (string id, UpdateTeamDto dto, ITeamDelegate teamDelegate) =>
        {
            var updated = await teamDelegate.UpdateAsync(id, dto.Name);
            return Results.Ok(updated.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<UpdateTeamDto>>();

        group.MapDelete("/{id}", async (string id, ITeamDelegate teamDelegate) =>
            await teamDelegate.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());
    }
}
