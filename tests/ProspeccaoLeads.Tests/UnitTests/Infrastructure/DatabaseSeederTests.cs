using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProspeccaoLeads.Infrastructure.Data;

namespace ProspeccaoLeads.Tests.UnitTests.Infrastructure;

public class DatabaseSeederTests
{
    [Fact]
    public async Task InitializeAsync_EmBancoExistenteSemHistorico_DeveCriarTabelaSearches()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE existing_table (id INTEGER PRIMARY KEY);";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await DatabaseSeeder.InitializeAsync(context, NullLogger.Instance);

        await using var verificationCommand = connection.CreateCommand();
        verificationCommand.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'searches';";
        var tableCount = Convert.ToInt32(await verificationCommand.ExecuteScalarAsync());

        Assert.Equal(1, tableCount);
    }
}
