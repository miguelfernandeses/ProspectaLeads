using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Infrastructure;

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
}
