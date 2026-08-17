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
    try
    {
        // Cria automaticamente as tabelas 'users', 'leads' e 'searches' no PostgreSQL/Supabase ou SQLite se não existirem
        dbContext.Database.EnsureCreated();

        var demoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        if (!dbContext.Users.Any(u => u.Id == demoUserId))
        {
            dbContext.Users.Add(new UserProfile
            {
                Id = demoUserId,
                Name = "Administrador",
                Email = "admin@prospeccaoleads.com",
                CreatedAt = DateTime.UtcNow
            });
            dbContext.SaveChanges();
        }

        // Seed inicial de dados demonstrativos apenas se o banco estiver vazio
        if (dbContext.Database.IsSqlite())
        {

            if (!dbContext.Leads.Any(l => l.UserId == demoUserId))
            {
            var seedLeads = new List<Lead>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = demoUserId,
                    Nome = "Clínica Odonto Sorriso & Arte",
                    Categoria = "Clínicas odontológicas",
                    Telefone = "(11) 99887-1122",
                    WhatsApp = "(11) 99887-1122",
                    Email = "contato@odontosorriso.com.br",
                    Endereco = "Av. Paulista, 1578",
                    Cidade = "São Paulo",
                    Estado = "SP",
                    CEP = "01310-200",
                    Website = "https://www.odontosorriso.com.br",
                    Instagram = "@odontosorriso.sp",
                    Avaliacao = 4.9m,
                    QuantidadeAvaliacoes = 342,
                    Status = StatusLead.Cliente,
                    Fonte = "OpenStreetMap",
                    Observacoes = "Cliente fechou contrato anual de software de gestão em 10/08.",
                    CreatedAt = DateTime.UtcNow.AddDays(-12),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = demoUserId,
                    Nome = "Academia Pulse Training & Fitness",
                    Categoria = "Academias",
                    Telefone = "(19) 98765-4321",
                    WhatsApp = "(19) 98765-4321",
                    Email = "gerencia@pulsetraining.com.br",
                    Endereco = "Av. José de Souza Campos, 450",
                    Cidade = "Campinas",
                    Estado = "SP",
                    CEP = "13025-320",
                    Website = "https://www.pulsetraining.com.br",
                    Instagram = "@pulsetraining",
                    Avaliacao = 4.7m,
                    QuantidadeAvaliacoes = 185,
                    Status = StatusLead.EmNegociacao,
                    Fonte = "OpenStreetMap",
                    Observacoes = "Proposta enviada para o sócio Marcos. Reunião agendada para sexta-feira.",
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = demoUserId,
                    Nome = "Restaurante Villa Nova Bistrô",
                    Categoria = "Restaurantes",
                    Telefone = "(21) 99123-8877",
                    WhatsApp = "(21) 99123-8877",
                    Email = "contato@villanovabistro.com.br",
                    Endereco = "Rua Visconde de Pirajá, 320",
                    Cidade = "Rio de Janeiro",
                    Estado = "RJ",
                    CEP = "22410-000",
                    Website = "https://www.villanovabistro.com.br",
                    Instagram = "@villanovabistro",
                    Avaliacao = 4.8m,
                    QuantidadeAvaliacoes = 512,
                    Status = StatusLead.Contatado,
                    Fonte = "Catálogo Comercial Nacional",
                    Observacoes = "Primeiro contato realizado via WhatsApp. Gerente demonstrou interesse na solução.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = demoUserId,
                    Nome = "Espaço Bella Mulher & Estética",
                    Categoria = "Salões de beleza",
                    Telefone = "(31) 98222-9900",
                    WhatsApp = "(31) 98222-9900",
                    Email = "atendimento@bellamulher.com.br",
                    Endereco = "Av. Afonso Pena, 1200",
                    Cidade = "Belo Horizonte",
                    Estado = "MG",
                    CEP = "30130-005",
                    Website = "https://www.bellamulher.com.br",
                    Instagram = "@bellamulherestetica",
                    Avaliacao = 4.6m,
                    QuantidadeAvaliacoes = 94,
                    Status = StatusLead.Interessado,
                    Fonte = "OpenStreetMap",
                    Observacoes = "Solicitou apresentação detalhada por e-mail.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = demoUserId,
                    Nome = "Auto Centro Precision & Mecânica",
                    Categoria = "Oficinas mecânicas",
                    Telefone = "(41) 99333-4455",
                    WhatsApp = "(41) 99333-4455",
                    Email = "contato@precisionautocenter.com.br",
                    Endereco = "Av. Sete de Setembro, 2800",
                    Cidade = "Curitiba",
                    Estado = "PR",
                    CEP = "80230-010",
                    Website = "https://www.precisionautocenter.com.br",
                    Instagram = "@precisionautocenter",
                    Avaliacao = 4.5m,
                    QuantidadeAvaliacoes = 78,
                    Status = StatusLead.Novo,
                    Fonte = "Catálogo Comercial Nacional",
                    Observacoes = "Lead recém importado da pesquisa.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            dbContext.Leads.AddRange(seedLeads);

            var seedSearches = new List<SearchHistory>
            {
                new() { Id = Guid.NewGuid(), UserId = demoUserId, Niche = "Clínicas odontológicas", Location = "São Paulo - SP", ResultCount = 30, CreatedAt = DateTime.UtcNow.AddDays(-12) },
                new() { Id = Guid.NewGuid(), UserId = demoUserId, Niche = "Academias", Location = "Campinas - SP", ResultCount = 20, CreatedAt = DateTime.UtcNow.AddDays(-8) },
                new() { Id = Guid.NewGuid(), UserId = demoUserId, Niche = "Restaurantes", Location = "Rio de Janeiro - RJ", ResultCount = 25, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new() { Id = Guid.NewGuid(), UserId = demoUserId, Niche = "Salões de beleza", Location = "Belo Horizonte - MG", ResultCount = 18, CreatedAt = DateTime.UtcNow.AddDays(-3) }
            };

            dbContext.Searches.AddRange(seedSearches);
            dbContext.SaveChanges();
        }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao inicializar banco de dados.");
    }
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
