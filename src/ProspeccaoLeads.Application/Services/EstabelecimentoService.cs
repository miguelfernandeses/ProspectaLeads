using Microsoft.Extensions.Logging;
using ProspeccaoLeads.Application.Common;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Domain.Interfaces;

namespace ProspeccaoLeads.Application.Services;

public class EstabelecimentoService : IEstabelecimentoService
{
    private readonly IEnumerable<IEstabelecimentoProvider> _providers;
    private readonly ILeadRepository _leadRepository;
    private readonly ISearchHistoryService _historyService;
    private readonly ILogger<EstabelecimentoService> _logger;

    public EstabelecimentoService(
        IEnumerable<IEstabelecimentoProvider> providers,
        ILeadRepository leadRepository,
        ISearchHistoryService historyService,
        ILogger<EstabelecimentoService> logger)
    {
        _providers = providers.OrderBy(p => p.Prioridade);
        _leadRepository = leadRepository;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<Result<List<EstabelecimentoDto>>> BuscarAsync(
        BuscaEstabelecimentoDto dto,
        Guid userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nicho))
        {
            return Result<List<EstabelecimentoDto>>.Failure("O nicho de pesquisa é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.Localizacao))
        {
            return Result<List<EstabelecimentoDto>>.Failure("A localização da pesquisa é obrigatória.");
        }

        var nicho = dto.Nicho.Trim();
        var localizacao = dto.Localizacao.Trim();
        var maxResultados = dto.MaxResultados > 0 ? dto.MaxResultados : 30;

        List<EstabelecimentoDto> resultados = new();
        string? provedorUtilizado = null;

        foreach (var provider in _providers)
        {
            try
            {
                if (await provider.DisponivelAsync(ct))
                {
                    _logger.LogInformation("Tentando buscar estabelecimentos via provedor {ProviderName} para Nicho='{Nicho}', Localizacao='{Loc}'",
                        provider.NomeProvedor, nicho, localizacao);

                    var itens = await provider.BuscarAsync(nicho, localizacao, maxResultados, ct);
                    if (itens != null && itens.Count > 0)
                    {
                        resultados = itens;
                        provedorUtilizado = provider.NomeProvedor;
                        _logger.LogInformation("Provedor {ProviderName} retornou {Count} estabelecimentos.", provider.NomeProvedor, itens.Count);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar estabelecimentos no provedor {ProviderName}. Tentando próximo provedor...", provider.NomeProvedor);
            }
        }

        if (resultados.Count == 0)
        {
            await _historyService.RegistrarBuscaAsync(userId, nicho, localizacao, 0, ct);
            return Result<List<EstabelecimentoDto>>.Failure("Nenhum estabelecimento foi encontrado para essa pesquisa. Tente modificar o nicho ou a localização.");
        }

        // Cruzar com os leads já salvos pelo usuário para marcar duplicatas
        var leadsUsuario = await _leadRepository.GetAllAsync(userId, ct);
        var leadsMap = leadsUsuario.ToDictionary(
            l => $"{l.Nome.ToLowerInvariant()}|{l.Cidade?.ToLowerInvariant()}",
            l => l.Id);

        foreach (var item in resultados)
        {
            var key = $"{item.Nome.ToLowerInvariant()}|{item.Cidade?.ToLowerInvariant()}";
            if (leadsMap.TryGetValue(key, out var existingId))
            {
                item.JaSalvoComoLead = true;
                item.LeadId = existingId;
            }
            else
            {
                // Busca parcial por nome se exato não encontrar
                var match = leadsUsuario.FirstOrDefault(l =>
                    string.Equals(l.Nome, item.Nome, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    item.JaSalvoComoLead = true;
                    item.LeadId = match.Id;
                }
            }
        }

        // Salvar histórico de pesquisa
        await _historyService.RegistrarBuscaAsync(userId, nicho, localizacao, resultados.Count, ct);

        return Result<List<EstabelecimentoDto>>.Success(resultados);
    }
}
