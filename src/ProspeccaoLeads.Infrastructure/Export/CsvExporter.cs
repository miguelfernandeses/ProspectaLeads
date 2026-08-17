using System.Text;
using ProspeccaoLeads.Application.DTOs.Lead;

namespace ProspeccaoLeads.Infrastructure.Export;

public static class CsvExporter
{
    public static byte[] Exportar(IEnumerable<LeadDto> leads)
    {
        var sb = new StringBuilder();

        // Cabeçalho CSV
        sb.AppendLine("ID;Nome;Categoria;Telefone;WhatsApp;E-mail;Endereço;Cidade;Estado;CEP;Website;Instagram;Avaliação;Qtd Avaliações;Status;Fonte;Observações;Data Cadastro");

        foreach (var l in leads)
        {
            sb.AppendLine(string.Join(";", new[]
            {
                EscapeCsv(l.Id.ToString()),
                EscapeCsv(l.Nome),
                EscapeCsv(l.Categoria),
                EscapeCsv(l.Telefone),
                EscapeCsv(l.WhatsApp),
                EscapeCsv(l.Email),
                EscapeCsv(l.Endereco),
                EscapeCsv(l.Cidade),
                EscapeCsv(l.Estado),
                EscapeCsv(l.CEP),
                EscapeCsv(l.Website),
                EscapeCsv(l.Instagram),
                EscapeCsv(l.Avaliacao?.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)),
                EscapeCsv(l.QuantidadeAvaliacoes?.ToString()),
                EscapeCsv(l.StatusFormatado),
                EscapeCsv(l.Fonte),
                EscapeCsv(l.Observacoes),
                EscapeCsv(l.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
            }));
        }

        // Adicionar UTF-8 BOM para garantir compatibilidade com Microsoft Excel
        var preamble = Encoding.UTF8.GetPreamble();
        var data = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + data.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(data, 0, result, preamble.Length, data.Length);

        return result;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var sanitized = value.Replace("\"", "\"\"");
        return $"\"{sanitized}\"";
    }
}
