using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Domain.Interfaces;
using ProspeccaoLeads.Infrastructure.Auth;
using ProspeccaoLeads.Infrastructure.Data;
using ProspeccaoLeads.Infrastructure.Export;
using ProspeccaoLeads.Infrastructure.ExternalApis.Providers;
using ProspeccaoLeads.Infrastructure.Repositories;

namespace ProspeccaoLeads.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configuração do Banco de Dados (PostgreSQL Supabase ou SQLite local)
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration["DATABASE_URL"] 
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        var supabaseDbUrl = configuration["Supabase:DatabaseUrl"] 
            ?? configuration["SUPABASE_DB_URL"] 
            ?? Environment.GetEnvironmentVariable("SUPABASE_DB_URL");

        var activeConnection = !string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("SEU_BANCO")
            ? connectionString
            : (!string.IsNullOrWhiteSpace(supabaseDbUrl) && !supabaseDbUrl.Contains("SEU_BANCO") ? supabaseDbUrl : null);

        if (!string.IsNullOrWhiteSpace(activeConnection))
        {
            var parsedConnection = FormatPostgreSqlConnectionString(activeConnection);
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(parsedConnection));
        }
        else
        {
            // Fallback para SQLite em desenvolvimento local caso as chaves não estejam configuradas ainda
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prospeccao_local.db");
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
        }

        // 2. Repositórios
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // 3. Provedores de Estabelecimentos Reais (Prioridade: Real Places / Google Places / OSM / Catálogo Comercial)
        services.AddHttpClient<RealWebPlacesProvider>();
        services.AddHttpClient<GooglePlacesProvider>();
        services.AddHttpClient<OpenStreetMapProvider>();
        services.AddHttpClient<SupabaseAuthService>();

        services.AddScoped<IEstabelecimentoProvider, RealWebPlacesProvider>();
        services.AddScoped<IEstabelecimentoProvider, GooglePlacesProvider>();
        services.AddScoped<IEstabelecimentoProvider, OpenStreetMapProvider>();
        services.AddScoped<IEstabelecimentoProvider, BrazilianDirectoryPlacesProvider>();

        // 4. Exportação & Autenticação
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IAuthService, SupabaseAuthService>();

        return services;
    }

    private static string FormatPostgreSqlConnectionString(string raw)
    {
        var conn = raw.Trim();
        if (conn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            conn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Limpar colchetes acidentais se o usuário manteve [senha]
                var cleanUri = conn.Replace("[", "").Replace("]", "");
                var uri = new Uri(cleanUri);

                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "postgres";
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');
                if (string.IsNullOrWhiteSpace(database)) database = "postgres";

                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true;";
            }
            catch
            {
                // Fallback para original
                return conn;
            }
        }

        // Se já for no formato Key-Value, apenas remove colchetes acidentais em Password=[...]
        if (conn.Contains("Password=[") && conn.Contains("]"))
        {
            conn = System.Text.RegularExpressions.Regex.Replace(conn, @"Password=\[([^\]]+)\]", "Password=$1");
        }

        return conn;
    }
}
