using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RoundBatman.Repositories;

namespace RoundBatman.Api;

/// <summary>
/// Lets `dotnet ef` create the context without booting the API host.
/// The connection string is only used to build the model/migration; the
/// migration bundle receives the real one at runtime.
/// </summary>
public class RoundBatmanDbContextFactory : IDesignTimeDbContextFactory<RoundBatmanDbContext>
{
    public RoundBatmanDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__RoundBatman")
            ?? "Host=localhost;Port=5432;Database=roundbatman;Username=roundbatman;Password=admin";

        var options = new DbContextOptionsBuilder<RoundBatmanDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new RoundBatmanDbContext(options);
    }
}
