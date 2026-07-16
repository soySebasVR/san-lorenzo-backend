using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServerlessAPI.Data;

/// <summary>Fábrica para migraciones de EF Core.</summary>
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
