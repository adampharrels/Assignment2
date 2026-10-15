using JobTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.Data;

/// <summary>
/// A fresh SQLite database held in memory for one test. The connection must stay open,
/// otherwise the in-memory database disappears.
/// </summary>
public sealed class TestDatabase : IDbContextFactory<JobTrackerDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JobTrackerDbContext> _options;

    /// <summary>Opens the connection and creates the schema.</summary>
    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<JobTrackerDbContext>().UseSqlite(_connection).Options;
    }

    /// <summary>Creates a context that shares the open in-memory connection.</summary>
    public JobTrackerDbContext CreateDbContext()
    {
        return new JobTrackerDbContext(_options);
    }

    /// <summary>Closes the connection, deleting the database.</summary>
    public void Dispose()
    {
        _connection.Dispose();
    }
}
