namespace ProspeccaoLeads.Web.Services;

public static class DotEnvLoader
{
    public static void Load()
    {
        try
        {
            var envPath = FindEnvFile();
            if (string.IsNullOrEmpty(envPath) || !File.Exists(envPath))
            {
                return;
            }

            var lines = File.ReadAllLines(envPath);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();

                // Remove aspas simples ou duplas ao redor do valor se existirem
                if ((value.StartsWith('"') && value.EndsWith('"')) ||
                    (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }

                // Define a variável de ambiente se ainda não estiver definida no sistema
                if (!string.IsNullOrEmpty(key))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
        catch
        {
            // Silencioso em caso de permissão de arquivo; continuará com appsettings padrão
        }
    }

    private static string? FindEnvFile()
    {
        // 1. Diretório atual de trabalho
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var path = CheckDirectoryAndParents(currentDir);
        if (path != null) return path;

        // 2. Diretório base da aplicação em execução
        var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        return CheckDirectoryAndParents(baseDir);
    }

    private static string? CheckDirectoryAndParents(DirectoryInfo? dir)
    {
        var depth = 0;
        while (dir != null && depth < 5)
        {
            var envCandidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(envCandidate))
            {
                return envCandidate;
            }

            dir = dir.Parent;
            depth++;
        }

        return null;
    }
}
