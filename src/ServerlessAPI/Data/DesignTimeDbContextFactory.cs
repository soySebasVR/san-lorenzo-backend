using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServerlessAPI.Data;

/// <summary>
/// Used only by `dotnet ef`. Without it EF would boot Program.cs and hit Secrets Manager
/// just to scaffold a migration. Generating migrations never connects, so a placeholder
/// connection string is enough; pass a real one via --connection to apply them.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SanLorenzoDbContext>
{
    public SanLorenzoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SANLORENZO_DESIGNTIME_CONNECTION")
                               ?? "Server=localhost,1433;Database=SanLorenzo;User Id=sa;Password=placeholder;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<SanLorenzoDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SanLorenzoDbContext(options);
    }
}
