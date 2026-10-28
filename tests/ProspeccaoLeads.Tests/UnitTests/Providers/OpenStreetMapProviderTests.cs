using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProspeccaoLeads.Infrastructure.ExternalApis.Providers;

namespace ProspeccaoLeads.Tests.UnitTests.Providers;

public class OpenStreetMapProviderTests
{
    [Fact]
    public async Task BuscarAsync_NaoDeveInventarAvaliacaoOuQuantidadeDeAvaliacoes()
    {
        var handler = new StubHandler(request => request.RequestUri!.Host.Contains("nominatim")
            ? Json("[{\"lat\":\"-22.90\",\"lon\":\"-47.06\"}]")
            : Json("""
                {"elements":[{"type":"node","lat":-22.9,"lon":-47.06,"tags":{
                  "name":"Academia Real","leisure":"fitness_centre","phone":"(19) 3333-4444"
                }}]}
                """));
        var provider = new OpenStreetMapProvider(
            new HttpClient(handler),
            Mock.Of<ILogger<OpenStreetMapProvider>>());

        var results = await provider.BuscarAsync("Academia", "Campinas - SP", 10);

        results.Should().ContainSingle();
        results[0].Avaliacao.Should().BeNull();
        results[0].QuantidadeAvaliacoes.Should().BeNull();
        results[0].Email.Should().BeNull();
    }

    [Fact]
    public async Task BuscarAsync_SemGeocodificacao_NaoDeveUsarOutraCidadeComoFallback()
    {
        var handler = new StubHandler(_ => Json("[]"));
        var provider = new OpenStreetMapProvider(
            new HttpClient(handler),
            Mock.Of<ILogger<OpenStreetMapProvider>>());

        var results = await provider.BuscarAsync("Academia", "Cidade Inexistente - SP", 10);

        results.Should().BeEmpty();
        handler.RequestCount.Should().Be(1);
    }

    [Theory]
    [InlineData("mecanica", "car_repair")]
    [InlineData("reparação automotiva", "car_repair")]
    [InlineData("floricultura", "florist")]
    public async Task BuscarAsync_DeveNormalizarTextoLivreParaFiltroOsm(string nicho, string filtroEsperado)
    {
        var handler = new StubHandler(request => request.RequestUri!.Host.Contains("nominatim")
            ? Json("[{\"lat\":\"-22.56\",\"lon\":\"-47.40\"}]")
            : Json("{\"elements\":[]}"));
        var provider = new OpenStreetMapProvider(
            new HttpClient(handler),
            Mock.Of<ILogger<OpenStreetMapProvider>>());

        await provider.BuscarAsync(nicho, "Limeira - SP", 10);

        handler.RequestBodies.Should().Contain(body => body.Contains(filtroEsperado));
    }
    private static HttpResponseMessage Json(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int RequestCount { get; private set; }
        public List<string> RequestBodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            if (request.Content != null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return _responseFactory(request);
        }
    }
}
