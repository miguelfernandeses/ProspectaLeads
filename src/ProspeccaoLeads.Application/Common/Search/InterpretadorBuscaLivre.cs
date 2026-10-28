using System.Globalization;
using System.Text;

namespace ProspeccaoLeads.Application.Common.Search;

public sealed record ConsultaProspeccao(string Nicho, string Localizacao, bool LocalizacaoInferida);

public static class InterpretadorBuscaLivre
{
    public static ConsultaProspeccao Interpretar(
        string? consulta,
        string? localizacao,
        IEnumerable<string>? cidadesConhecidas = null)
    {
        var nicho = consulta?.Trim() ?? string.Empty;
        var local = localizacao?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(local) || string.IsNullOrWhiteSpace(nicho))
        {
            return new(nicho, local, false);
        }

        var consultaNormalizada = Normalizar(nicho);
        var cidades = cidadesConhecidas ?? CatalogoBuscaProspeccao.Cidades;

        foreach (var cidadeCompleta in cidades.OrderByDescending(cidade => ObterNomeCidade(cidade).Length))
        {
            var nomeCidade = ObterNomeCidade(cidadeCompleta);
            var cidadeNormalizada = Normalizar(nomeCidade);
            if (!TerminaComTermo(consultaNormalizada, cidadeNormalizada))
            {
                continue;
            }

            var ramo = nicho[..Math.Max(0, nicho.Length - nomeCidade.Length)]
                .Trim(' ', ',', '-', '/');
            if (Normalizar(ramo).EndsWith(" em", StringComparison.Ordinal))
            {
                ramo = ramo[..^3].TrimEnd();
            }

            if (ramo.Length >= 2)
            {
                return new(ramo, cidadeCompleta, true);
            }
        }

        return new(nicho, local, false);
    }

    public static string Normalizar(string valor)
    {
        var decomposed = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var caractere in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(caractere);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ObterNomeCidade(string cidadeCompleta)
    {
        var separador = cidadeCompleta.LastIndexOf(" - ", StringComparison.Ordinal);
        return separador > 0 ? cidadeCompleta[..separador].Trim() : cidadeCompleta.Trim();
    }

    private static bool TerminaComTermo(string consulta, string termo)
    {
        return consulta.Equals(termo, StringComparison.Ordinal) ||
               consulta.EndsWith($" {termo}", StringComparison.Ordinal);
    }
}
