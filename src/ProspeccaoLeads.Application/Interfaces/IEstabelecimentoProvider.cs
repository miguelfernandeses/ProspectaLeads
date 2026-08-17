using ProspeccaoLeads.Application.DTOs.Estabelecimento;

namespace ProspeccaoLeads.Application.Interfaces;

public interface IEstabelecimentoProvider
{
    string NomeProvedor { get; }
    int Prioridade { get; }
    Task<bool> DisponivelAsync(CancellationToken ct = default);
    Task<List<EstabelecimentoDto>> BuscarAsync(
        string nicho,
        string localizacao,
        int maxResultados = 30,
        CancellationToken ct = default);
}
