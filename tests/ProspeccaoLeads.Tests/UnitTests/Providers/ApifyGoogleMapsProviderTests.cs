using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ProspeccaoLeads.Infrastructure.ExternalApis.Providers;
using Xunit;

namespace ProspeccaoLeads.Tests.UnitTests.Providers;

public class ApifyGoogleMapsProviderTests
{
    private readonly Mock<ILogger<ApifyGoogleMapsProvider>> _loggerMock = new();

    [Fact]
    public async Task DisponivelAsync_SemToken_DeveRetornarFalse()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Apify:ApiToken", ""}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var httpClient = new HttpClient();
        var provider = new ApifyGoogleMapsProvider(httpClient, configuration, _loggerMock.Object);

        // Act
        var disponivel = await provider.DisponivelAsync();

        // Assert
        disponivel.Should().BeFalse();
    }

    [Fact]
    public async Task DisponivelAsync_ComTokenValido_DeveRetornarTrue()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Apify:ApiToken", "apify_api_test_valid_token_12345"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var httpClient = new HttpClient();
        var provider = new ApifyGoogleMapsProvider(httpClient, configuration, _loggerMock.Object);

        // Act
        var disponivel = await provider.DisponivelAsync();

        // Assert
        disponivel.Should().BeTrue();
    }

    [Fact]
    public async Task BuscarAsync_ComRespostaValidaDoApify_DeveFazerParsingCompleto()
    {
        // Arrange
        var apifyJsonResponse = @"
        [
            {
                ""title"": ""Odonto Prime Araras"",
                ""categoryName"": ""Clínica Odontológica"",
                ""phone"": ""(19) 3541-9988"",
                ""address"": ""Rua Tiradentes, 450, Centro, Araras - SP"",
                ""city"": ""Araras"",
                ""state"": ""SP"",
                ""postalCode"": ""13600-000"",
                ""website"": ""https://odontoprime.com.br"",
                ""totalScore"": 4.9,
                ""reviewsCount"": 128,
                ""location"": {
                    ""lat"": -22.3564,
                    ""lng"": -47.3842
                },
                ""instagram"": ""@odontoprime_araras"",
                ""description"": ""Atendimento odontológico especializado.""
            }
        ]";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(apifyJsonResponse)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Apify:ApiToken", "apify_api_token_valido"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var provider = new ApifyGoogleMapsProvider(httpClient, configuration, _loggerMock.Object);

        // Act
        var resultados = await provider.BuscarAsync("Clínicas Odontológicas", "Araras - SP", 20);

        // Assert
        resultados.Should().NotBeNull();
        resultados.Should().HaveCount(1);

        var lead = resultados.First();
        lead.Nome.Should().Be("Odonto Prime Araras");
        lead.Categoria.Should().Be("Clínica Odontológica");
        lead.Telefone.Should().Be("(19) 3541-9988");
        lead.WhatsApp.Should().Be("(19) 3541-9988");
        lead.Cidade.Should().Be("Araras");
        lead.Estado.Should().Be("SP");
        lead.Website.Should().Be("https://odontoprime.com.br");
        lead.Avaliacao.Should().Be(4.9m);
        lead.QuantidadeAvaliacoes.Should().Be(128);
        lead.Latitude.Should().Be(-22.3564);
        lead.Longitude.Should().Be(-47.3842);
        lead.Fonte.Should().Be("Google Maps (Apify)");
    }

    [Fact]
    public async Task BuscarAsync_QuandoApiRetornaErroHttp_DeveRetornarListaVaziaSemLancarExcecao()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(@"{""error"": ""Invalid token""}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Apify:ApiToken", "invalid_token"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var provider = new ApifyGoogleMapsProvider(httpClient, configuration, _loggerMock.Object);

        // Act
        var resultados = await provider.BuscarAsync("Restaurantes", "Campinas - SP", 20);

        // Assert
        resultados.Should().NotBeNull();
        resultados.Should().BeEmpty();
    }
}
