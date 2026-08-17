using ProspeccaoLeads.Application.Common;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;

namespace ProspeccaoLeads.Application.Interfaces;

public interface IEstabelecimentoService
{
    Task<Result<List<EstabelecimentoDto>>> BuscarAsync(
        BuscaEstabelecimentoDto dto,
        Guid userId,
        CancellationToken ct = default);
}
