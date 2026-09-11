using System.Text.Json.Serialization;
using FluentValidation;
using RoundBatman.Api.Dtos;
using RoundBatman.Api.Extensions;
using RoundBatman.Api.Routes;
using RoundBatman.Api.Validators;
using RoundBatman.Delegates;
using RoundBatman.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Serializar/deserializar enums como texto (ej. "ROUND_ROBIN") en vez de números
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ProblemDetails enriquecido (RFC 7807) para respuestas de error
builder.Services.AddProblemDetails();

// CORS — necesario para que client/ (servido en otro origen) pueda llamar a esta API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Almacén en memoria (Singleton: un solo estado compartido durante la vida de la app)
builder.Services.AddSingleton<InMemoryDataStore>();

// Repositories
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();

// Delegates
builder.Services.AddScoped<ITeamDelegate, TeamDelegate>();
builder.Services.AddScoped<ITournamentDelegate, TournamentDelegate>();
builder.Services.AddScoped<IGroupDelegate, GroupDelegate>();
builder.Services.AddScoped<IMatchDelegate, MatchDelegate>();

// Validadores (FluentValidation)
builder.Services.AddScoped<IValidator<CreateTeamDto>, CreateTeamDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateTeamDto>, UpdateTeamDtoValidator>();
builder.Services.AddScoped<IValidator<CreateTournamentDto>, CreateTournamentDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateTournamentDto>, UpdateTournamentDtoValidator>();
builder.Services.AddScoped<IValidator<PatchTournamentDto>, PatchTournamentDtoValidator>();
builder.Services.AddScoped<IValidator<CreateGroupDto>, CreateGroupDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateGroupDto>, UpdateGroupDtoValidator>();
builder.Services.AddScoped<IValidator<AssignTeamsDto>, AssignTeamsDtoValidator>();
builder.Services.AddScoped<IValidator<CreateMatchDto>, CreateMatchDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateScoreDto>, UpdateScoreDtoValidator>();

var app = builder.Build();

// Manejo global de excepciones → respuestas ProblemDetails consistentes
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowFrontend");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapTeamRoutes();
app.MapTournamentRoutes();
app.MapGroupRoutes();
app.MapMatchRoutes();

app.Run();

public partial class Program { }
