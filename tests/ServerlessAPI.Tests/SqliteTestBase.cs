using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using Xunit;

namespace ServerlessAPI.Tests;

/// <summary>
/// SQLite in memory rather than EF's InMemory provider: SQLite is relational, so it
/// enforces foreign keys and unique indexes. That is what proves the upserts do not
/// duplicate rows — InMemory would not catch it.
/// </summary>
public abstract class SqliteTestBase : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    protected SanLorenzoDbContext Db { get; private set; } = null!;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        // Closing the connection would drop the in-memory database, so it stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync(Ct);

        Db = new SanLorenzoDbContext(
            new DbContextOptionsBuilder<SanLorenzoDbContext>().UseSqlite(_connection).Options);

        await Db.Database.EnsureCreatedAsync(Ct);
        await SeedAsync();
        await Db.SaveChangesAsync(Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    protected abstract Task SeedAsync();
}
