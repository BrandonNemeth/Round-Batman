using RoundBatman.Api.Routes;
using RoundBatman.Delegates;
using RoundBatman.Repositories;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.UseCors("AllowFrontend");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapTeamRoutes();
app.MapTournamentRoutes();
app.MapGroupRoutes();
app.MapMatchRoutes();

app.Run();
