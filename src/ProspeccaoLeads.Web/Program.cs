using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProspeccaoLeads.Application;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Infrastructure;
using ProspeccaoLeads.Infrastructure.Data;
using ProspeccaoLeads.Web.Components;
using ProspeccaoLeads.Web.Services;

// 0. Carregar variáveis de ambiente do arquivo .env (se existir)
DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// 1. Injeção de Dependências das Camadas de Aplicação e Infraestrutura
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Serviços da Camada Web e Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<FileDownloadService>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

var app = builder.Build();

// 3. Inicialização e Migração Automática do Banco de Dados
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseSeeder.InitializeAsync(dbContext, logger);
}


// 4. Pipeline de Requisição HTTP
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
