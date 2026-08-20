using ClosedXML.Excel;
using ProspeccaoLeads.Application.DTOs.Lead;
using ProspeccaoLeads.Application.Interfaces;

namespace ProspeccaoLeads.Infrastructure.Export;

public class ExportService : IExportService
{
    public Task<byte[]> ExportarParaCsvAsync(IEnumerable<LeadDto> leads, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var bytes = CsvExporter.Exportar(leads);
        return Task.FromResult(bytes);
    }

    public Task<byte[]> ExportarParaExcelAsync(IEnumerable<LeadDto> leads, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Meus Leads");

        // Cabeçalhos
        var headers = new[]
        {
            "Nome", "Categoria", "Telefone", "WhatsApp", "E-mail",
            "Endereço", "Cidade", "UF", "CEP", "Website",
            "Instagram", "Avaliação", "Qtd Avaliações", "Status",
            "Fonte", "Observações", "Data Cadastro"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5"); // Indigo 600
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 2;
        foreach (var l in leads)
        {
            worksheet.Cell(row, 1).Value = l.Nome;
            worksheet.Cell(row, 2).Value = l.Categoria ?? "";
            worksheet.Cell(row, 3).Value = l.Telefone ?? "";
            worksheet.Cell(row, 4).Value = l.WhatsApp ?? "";
            worksheet.Cell(row, 5).Value = l.Email ?? "";
            worksheet.Cell(row, 6).Value = l.Endereco ?? "";
            worksheet.Cell(row, 7).Value = l.Cidade ?? "";
            worksheet.Cell(row, 8).Value = l.Estado ?? "";
            worksheet.Cell(row, 9).Value = l.CEP ?? "";
            worksheet.Cell(row, 10).Value = l.Website ?? "";
            worksheet.Cell(row, 11).Value = l.Instagram ?? "";
            worksheet.Cell(row, 12).Value = l.Avaliacao.HasValue ? (double)l.Avaliacao.Value : "";
            worksheet.Cell(row, 13).Value = l.QuantidadeAvaliacoes.HasValue ? l.QuantidadeAvaliacoes.Value : "";
            worksheet.Cell(row, 14).Value = l.StatusFormatado;
            worksheet.Cell(row, 15).Value = l.Fonte ?? "";
            worksheet.Cell(row, 16).Value = l.Observacoes ?? "";
            worksheet.Cell(row, 17).Value = l.CreatedAt.ToString("dd/MM/yyyy HH:mm");

            // Zebra striping
            if (row % 2 == 0)
            {
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }

            row++;
        }

        worksheet.Columns().AdjustToContents(10.0, 50.0);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
