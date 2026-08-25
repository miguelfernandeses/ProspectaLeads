using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.Interfaces;

namespace ProspeccaoLeads.Infrastructure.ExternalApis.Providers;

public class ApifyGoogleMapsProvider : IEstabelecimentoProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApifyGoogleMapsProvider> _logger;

    public string NomeProvedor => "Google Maps (Apify Scraper)";
    public int Prioridade => 0; // Prioridade máxima quando o token do Apify estiver configurado

    public ApifyGoogleMapsProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ApifyGoogleMapsProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> DisponivelAsync(CancellationToken ct = default)
    {
        var token = ObterApiToken();
        return Task.FromResult(!string.IsNullOrWhiteSpace(token) && !token.Contains("SEU_TOKEN") && !token.Contains("SUA_CHAVE"));
    }

    public async Task<List<EstabelecimentoDto>> BuscarAsync(
        string nicho,
        string localizacao,
        int maxResultados = 30,
        CancellationToken ct = default)
    {
        var token = ObterApiToken();
        var resultados = new List<EstabelecimentoDto>();

        if (string.IsNullOrWhiteSpace(token) || token.Contains("SEU_TOKEN"))
        {
            return resultados;
        }

        try
        {
            var termoBusca = $"{nicho} em {localizacao}";
            _logger.LogInformation("ApifyGoogleMapsProvider: Iniciando busca no Google Maps via Apify para '{Termo}' (Max: {Max})", termoBusca, maxResultados);

            // Payload para o Actor oficial do Google Maps no Apify (compass~crawler-google-places)
            var payload = new
            {
                searchStringsArray = new[] { termoBusca },
                maxCrawledPlacesPerSearch = maxResultados,
                language = "pt",
                countryCode = "br",
                skipClosedPlaces = false
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Chamada síncrona que executa e já retorna os dados do dataset
            var endpoint = $"https://api.apify.com/v2/acts/compass~crawler-google-places/run-sync-get-dataset-items?token={token}&timeout=90";
            
            var response = await _httpClient.PostAsync(endpoint, jsonContent, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Fallback para o actor alternativo apify~google-maps-scraper se necessário
                _logger.LogWarning("Apify compass retornou HTTP {StatusCode}. Tentando actor alternativo apify~google-maps-scraper...", response.StatusCode);
                var fallbackEndpoint = $"https://api.apify.com/v2/acts/apify~google-maps-scraper/run-sync-get-dataset-items?token={token}&timeout=90";
                response = await _httpClient.PostAsync(fallbackEndpoint, jsonContent, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Apify retornou erro HTTP: {StatusCode}", response.StatusCode);
                    return resultados;
                }
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(responseJson) || responseJson == "[]")
            {
                _logger.LogInformation("ApifyGoogleMapsProvider: Nenhum resultado retornado para '{Termo}'", termoBusca);
                return resultados;
            }

            var jsonArray = JsonNode.Parse(responseJson)?.AsArray();
            if (jsonArray == null || jsonArray.Count == 0)
            {
                return resultados;
            }

            foreach (var item in jsonArray)
            {
                if (item == null) continue;

                var nome = item["title"]?.ToString() ?? item["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(nome)) continue;

                var categoria = item["categoryName"]?.ToString() ??
                                item["category"]?.ToString() ??
                                item["primaryCategory"]?.ToString() ??
                                nicho;

                var telefone = item["phone"]?.ToString() ??
                               item["phoneUnformatted"]?.ToString() ??
                               item["phoneNumber"]?.ToString();

                var enderecoCompleto = item["address"]?.ToString() ??
                                       item["street"]?.ToString() ??
                                       "";

                var cidade = item["city"]?.ToString();
                var estado = item["state"]?.ToString();
                var cep = item["postalCode"]?.ToString();

                // Se cidade/estado não vierem separados, extrai da localização
                if (string.IsNullOrWhiteSpace(cidade) && localizacao.Contains('-'))
                {
                    var parts = localizacao.Split('-');
                    cidade = parts[0].Trim();
                    estado = parts[1].Trim();
                }
                else if (string.IsNullOrWhiteSpace(cidade))
                {
                    cidade = localizacao.Trim();
                    estado = "SP";
                }

                var website = item["website"]?.ToString() ?? item["url"]?.ToString();
                var instagram = ExtrairInstagram(item);

                decimal? avaliacao = null;
                if (item["totalScore"] != null && decimal.TryParse(item["totalScore"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var scoreVal))
                {
                    avaliacao = scoreVal;
                }
                else if (item["rating"] != null && decimal.TryParse(item["rating"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rVal))
                {
                    avaliacao = rVal;
                }

                int? reviewsCount = null;
                if (item["reviewsCount"] != null && int.TryParse(item["reviewsCount"]?.ToString(), out var rCount))
                {
                    reviewsCount = rCount;
                }

                double? latitude = null;
                double? longitude = null;
                if (item["location"]?["lat"] != null && double.TryParse(item["location"]?["lat"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                {
                    latitude = lat;
                }
                if (item["location"]?["lng"] != null && double.TryParse(item["location"]?["lng"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
                {
                    longitude = lng;
                }

                var observacoes = item["description"]?.ToString();
                if (string.IsNullOrWhiteSpace(observacoes) && item["openingHours"] != null)
                {
                    observacoes = "Horário de funcionamento verificado no Google Maps.";
                }

                resultados.Add(new EstabelecimentoDto
                {
                    Nome = nome.Trim(),
                    Categoria = categoria.Trim(),
                    Telefone = telefone?.Trim(),
                    WhatsApp = telefone?.Trim(),
                    Email = item["email"]?.ToString()?.Trim(),
                    Endereco = string.IsNullOrWhiteSpace(enderecoCompleto) ? "Endereço comercial verificado no Google Maps" : enderecoCompleto.Trim(),
                    Cidade = cidade?.Trim(),
                    Estado = estado?.Trim(),
                    CEP = cep?.Trim(),
                    Website = website?.Trim(),
                    Instagram = instagram?.Trim(),
                    Avaliacao = avaliacao,
                    QuantidadeAvaliacoes = reviewsCount,
                    Latitude = latitude,
                    Longitude = longitude,
                    Fonte = "Google Maps (Apify)",
                    Observacoes = observacoes
                });

                if (resultados.Count >= maxResultados)
                {
                    break;
                }
            }

            _logger.LogInformation("ApifyGoogleMapsProvider: {Count} empresas reais obtidas com sucesso do Google Maps!", resultados.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar Google Maps via Apify.");
        }

        return resultados;
    }

    private string? ObterApiToken()
    {
        return _configuration["Apify:ApiToken"] ??
               _configuration["APIFY_API_TOKEN"] ??
               Environment.GetEnvironmentVariable("APIFY_API_TOKEN") ??
               Environment.GetEnvironmentVariable("Apify__ApiToken");
    }

    private static string? ExtrairInstagram(JsonNode item)
    {
        if (item["instagram"] != null) return item["instagram"]?.ToString();
        if (item["socialMedias"]?["instagram"] != null) return item["socialMedias"]?["instagram"]?.ToString();

        // Se houver links em additionalInfo ou socialProfiles
        var socialProfiles = item["socialProfiles"]?.AsArray();
        if (socialProfiles != null)
        {
            foreach (var profile in socialProfiles)
            {
                var link = profile?.ToString() ?? "";
                if (link.Contains("instagram.com", StringComparison.OrdinalIgnoreCase))
                {
                    return link;
                }
            }
        }

        return null;
    }
}
