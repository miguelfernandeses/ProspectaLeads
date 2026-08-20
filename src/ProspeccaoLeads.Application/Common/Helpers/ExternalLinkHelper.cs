namespace ProspeccaoLeads.Application.Common.Helpers;

public static class ExternalLinkHelper
{
    public static string GerarWhatsAppUrl(string? telefoneOuWhatsApp)
    {
        if (string.IsNullOrWhiteSpace(telefoneOuWhatsApp)) return string.Empty;
        var digits = new string(telefoneOuWhatsApp.Where(char.IsDigit).ToArray());
        if (digits.Length == 10 || digits.Length == 11)
        {
            digits = "55" + digits;
        }
        return $"https://wa.me/{digits}";
    }

    public static string GerarGoogleMapsUrl(string nome, string? endereco, string? cidade, string? estado)
    {
        var localParts = new[] { endereco, cidade, estado }
            .Where(s => !string.IsNullOrWhiteSpace(s) &&
                        !s.Equals("Centro", StringComparison.OrdinalIgnoreCase) &&
                        !s.Contains("Não informado", StringComparison.OrdinalIgnoreCase));

        var enderecoCompleto = string.Join(", ", localParts);

        var termo = string.IsNullOrWhiteSpace(enderecoCompleto)
            ? $"{nome}, {cidade} - {estado}".Trim()
            : $"{nome}, {enderecoCompleto}".Trim();

        var query = Uri.EscapeDataString(termo);
        return $"https://www.google.com/maps/search/?api=1&query={query}";
    }

    public static string GerarInstagramUrl(string? instagram)
    {
        if (string.IsNullOrWhiteSpace(instagram)) return string.Empty;
        var handle = instagram.Trim().TrimStart('@');
        if (handle.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return handle;
        return $"https://instagram.com/{handle}";
    }
}
