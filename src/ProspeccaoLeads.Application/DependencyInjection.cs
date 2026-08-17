using Microsoft.Extensions.DependencyInjection;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Application.Services;

namespace ProspeccaoLeads.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IEstabelecimentoService, EstabelecimentoService>();
        services.AddScoped<ISearchHistoryService, SearchHistoryService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
