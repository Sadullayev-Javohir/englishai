using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can construct the context
/// without booting the whole Web host. Uses the local docker-compose PostgreSQL
/// (host port 5435, see docker-compose.yml). Override with the
/// <c>ENGLISHAI_DESIGN_CONNECTION</c> environment variable when needed.
/// </summary>
public sealed class EnglishAiDbContextFactory : IDesignTimeDbContextFactory<EnglishAiDbContext>
{
    public EnglishAiDbContext CreateDbContext(string[] args)
    {
        var designTimeConnection =
            Environment.GetEnvironmentVariable("ENGLISHAI_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5435;Database=englishai;Username=englishai;Password=englishai_dev";

        var options = new DbContextOptionsBuilder<EnglishAiDbContext>()
            .UseNpgsql(designTimeConnection)
            .Options;

        return new EnglishAiDbContext(options);
    }
}
