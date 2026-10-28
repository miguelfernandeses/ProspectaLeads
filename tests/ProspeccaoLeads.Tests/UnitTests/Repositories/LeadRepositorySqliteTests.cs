using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Infrastructure.Data;
using ProspeccaoLeads.Infrastructure.Repositories;

namespace ProspeccaoLeads.Tests.UnitTests.Repositories;

public class LeadRepositorySqliteTests
{
    [Fact]
    public async Task GetFilteredAsync_DevePesquisarSemDiferenciarMaiusculasNoSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        context.Leads.Add(new Lead(userId, "Clínica Sorriso", cidade: "Campinas"));
        await context.SaveChangesAsync();

        var repository = new LeadRepository(context);
        var results = await repository.GetFilteredAsync(
            userId,
            "sorriso",
            null,
            "CAMPINAS",
            null,
            null,
            null,
            null,
            "Nome",
            false,
            1,
            20);

        results.Should().ContainSingle();
    }
}
