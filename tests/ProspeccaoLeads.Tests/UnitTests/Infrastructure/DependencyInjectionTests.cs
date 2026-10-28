using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Infrastructure;
using ProspeccaoLeads.Infrastructure.Data;

namespace ProspeccaoLeads.Tests.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_NaoDeveRegistrarProvedoresSinteticos()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        var providerNames = services
            .Where(descriptor => descriptor.ServiceType == typeof(IEstabelecimentoProvider))
            .Select(descriptor => descriptor.ImplementationType?.Name)
            .ToList();
        providerNames.Should().BeEquivalentTo(
            "ApifyGoogleMapsProvider",
            "GooglePlacesProvider",
            "OpenStreetMapProvider");
    }

    [Fact]
    public void AddInfrastructure_ModoLocalDeveUsarSqliteMesmoComConexaoPostgresPresente()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Mode"] = "Local",
                ["ConnectionStrings:DefaultConnection"] = "Host=invalid;Database=test;Username=test;Password=test"
            })
            .Build();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
    }

    [Fact]
    public void AddInfrastructure_ModoPostgresSemConexaoDeveFalharComMensagemClara()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Mode"] = "Postgres"
            })
            .Build();

        var action = () => services.AddInfrastructure(configuration);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*nenhuma conexão válida*");
    }
}
