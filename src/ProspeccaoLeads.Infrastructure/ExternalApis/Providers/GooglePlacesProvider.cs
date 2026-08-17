using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.Interfaces;

namespace ProspeccaoLeads.Infrastructure.ExternalApis.Providers;

public class GooglePlacesProvider : IEstabelecimentoProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GooglePlacesProvider> _logger;

    public string NomeProvedor => "Google Places API";
    public int Prioridade => 2;

    public GooglePlacesProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GooglePlacesProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> DisponivelAsync(CancellationToken ct = default)
    {
        var apiKey = _configuration["GooglePlaces:ApiKey"];
        return Task.FromResult(!string.IsNullOrWhiteSpace(apiKey) && !apiKey.Contains("SUA_CHAVE"));
    }

    public async Task<List<EstabelecimentoDto>> BuscarAsync(
        string nicho,
        string localizacao,
        int maxResultados = 30,
        CancellationToken ct = default)
    {
        var apiKey = _configuration["GooglePlaces:ApiKey"];
        var resultados = new List<EstabelecimentoDto>();

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("SUA_CHAVE"))
        {
            return resultados;
        }

        try
        {
            var query = Uri.EscapeDataString($"{nicho} em {localizacao}");
            var url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query={query}&language=pt-BR&key={apiKey}";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Places API retornou erro HTTP: {StatusCode}", response.StatusCode);
                return resultados;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("results", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (resultados.Count >= maxResultados) break;

                    var nome = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (string.IsNullOrWhiteSpace(nome)) continue;

                    var endereco = item.TryGetProperty("formatted_address", out var addr) ? addr.GetString() : null;
                    var rating = item.TryGetProperty("rating", out var r) ? (decimal?)r.GetDecimal() : null;
                    var reviews = item.TryGetProperty("user_ratings_total", out var rev) ? (int?)rev.GetInt32() : null;

                    double? lat = null;
                    double? lon = null;

                    if (item.TryGetProperty("geometry", out var geom) &&
                        geom.TryGetProperty("location", out var loc))
                    {
                        if (loc.TryGetProperty("lat", out var latProp)) lat = latProp.GetDouble();
                        if (loc.TryGetProperty("lng", out var lngProp)) lon = lngProp.GetDouble();
                    }

                    resultados.Add(new EstabelecimentoDto
                    {
                        Nome = nome,
                        Categoria = nicho,
                        Endereco = endereco,
                        Cidade = ExtrairCidade(localizacao),
                        Estado = ExtrairEstado(localizacao),
                        Avaliacao = rating,
                        QuantidadeAvaliacoes = reviews,
                        Latitude = lat,
                        Longitude = lon,
                        Fonte = "Google Places API",
                        Observacoes = "Coletado via Google Places"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar Google Places API.");
        }

        return resultados;
    }

    private static string ExtrairCidade(string localizacao)
    {
        var parts = localizacao.Split(new[] { '-', ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0].Trim() : localizacao.Trim();
    }

    private static string ExtrairEstado(string localizacao)
    {
        var parts = localizacao.Split(new[] { '-', ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1].Trim() : "SP";
    }
}
