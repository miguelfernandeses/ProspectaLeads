using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static readonly Guid DemoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task InitializeAsync(AppDbContext dbContext, ILogger logger, CancellationToken ct = default)
    {
        try
        {
            // Cria tabelas no PostgreSQL/Supabase ou SQLite caso não existam
            await dbContext.Database.EnsureCreatedAsync(ct);

            // Garante existência do usuário administrador padrão
            if (!await dbContext.Users.AnyAsync(u => u.Id == DemoUserId, ct))
            {
                await dbContext.Users.AddAsync(new UserProfile
                {
                    Id = DemoUserId,
                    Name = "Administrador",
                    Email = "admin@prospeccaoleads.com",
                    CreatedAt = DateTime.UtcNow
                }, ct);
                await dbContext.SaveChangesAsync(ct);
            }

            // Seed demonstrativo apenas para SQLite local se a base de leads estiver vazia
            if (dbContext.Database.IsSqlite())
            {
                if (!await dbContext.Leads.AnyAsync(l => l.UserId == DemoUserId, ct))
                {
                    var seedLeads = ObterLeadsDemonstracao();
                    await dbContext.Leads.AddRangeAsync(seedLeads, ct);

                    var seedSearches = ObterHistoricoDemonstracao();
                    await dbContext.Searches.AddRangeAsync(seedSearches, ct);

                    await dbContext.SaveChangesAsync(ct);
                    logger.LogInformation("DatabaseSeeder: Dados de demonstração inseridos com sucesso no SQLite.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao inicializar e semear banco de dados.");
        }
    }

    private static List<Lead> ObterLeadsDemonstracao()
    {
        return new List<Lead>
        {
            new(
                userId: DemoUserId,
                nome: "Clínica Odonto Sorriso & Arte",
                categoria: "Clínicas odontológicas",
                telefone: "(11) 99887-1122",
                whatsApp: "(11) 99887-1122",
                email: "contato@odontosorriso.com.br",
                endereco: "Av. Paulista, 1578",
                cidade: "São Paulo",
                estado: "SP",
                cep: "01310-200",
                website: "https://www.odontosorriso.com.br",
                instagram: "@odontosorriso.sp",
                avaliacao: 4.9m,
                quantidadeAvaliacoes: 342,
                observacoes: "Cliente fechou contrato anual de software de gestão em 10/08.",
                status: StatusLead.Cliente,
                fonte: "OpenStreetMap"
            ),
            new(
                userId: DemoUserId,
                nome: "Academia Pulse Training & Fitness",
                categoria: "Academias",
                telefone: "(19) 98765-4321",
                whatsApp: "(19) 98765-4321",
                email: "gerencia@pulsetraining.com.br",
                endereco: "Av. José de Souza Campos, 450",
                cidade: "Campinas",
                estado: "SP",
                cep: "13025-320",
                website: "https://www.pulsetraining.com.br",
                instagram: "@pulsetraining",
                avaliacao: 4.7m,
                quantidadeAvaliacoes: 185,
                observacoes: "Proposta enviada para o sócio Marcos. Reunião agendada para sexta-feira.",
                status: StatusLead.EmNegociacao,
                fonte: "OpenStreetMap"
            ),
            new(
                userId: DemoUserId,
                nome: "Restaurante Villa Nova Bistrô",
                categoria: "Restaurantes",
                telefone: "(21) 99123-8877",
                whatsApp: "(21) 99123-8877",
                email: "contato@villanovabistro.com.br",
                endereco: "Rua Visconde de Pirajá, 320",
                cidade: "Rio de Janeiro",
                estado: "RJ",
                cep: "22410-000",
                website: "https://www.villanovabistro.com.br",
                instagram: "@villanovabistro",
                avaliacao: 4.8m,
                quantidadeAvaliacoes: 512,
                observacoes: "Primeiro contato realizado via WhatsApp. Gerente demonstrou interesse na solução.",
                status: StatusLead.Contatado,
                fonte: "Catálogo Comercial Nacional"
            ),
            new(
                userId: DemoUserId,
                nome: "Espaço Bella Mulher & Estética",
                categoria: "Salões de beleza",
                telefone: "(31) 98222-9900",
                whatsApp: "(31) 98222-9900",
                email: "atendimento@bellamulher.com.br",
                endereco: "Av. Afonso Pena, 1200",
                cidade: "Belo Horizonte",
                estado: "MG",
                cep: "30130-005",
                website: "https://www.bellamulher.com.br",
                instagram: "@bellamulherestetica",
                avaliacao: 4.6m,
                quantidadeAvaliacoes: 94,
                observacoes: "Solicitou apresentação detalhada por e-mail.",
                status: StatusLead.Interessado,
                fonte: "OpenStreetMap"
            ),
            new(
                userId: DemoUserId,
                nome: "Auto Centro Precision & Mecânica",
                categoria: "Oficinas mecânicas",
                telefone: "(41) 99333-4455",
                whatsApp: "(41) 99333-4455",
                email: "contato@precisionautocenter.com.br",
                endereco: "Av. Sete de Setembro, 2800",
                cidade: "Curitiba",
                estado: "PR",
                cep: "80230-010",
                website: "https://www.precisionautocenter.com.br",
                instagram: "@precisionautocenter",
                avaliacao: 4.5m,
                quantidadeAvaliacoes: 78,
                observacoes: "Lead recém importado da pesquisa.",
                status: StatusLead.Novo,
                fonte: "Catálogo Comercial Nacional"
            )
        };
    }

    private static List<SearchHistory> ObterHistoricoDemonstracao()
    {
        return new List<SearchHistory>
        {
            new() { Id = Guid.NewGuid(), UserId = DemoUserId, Niche = "Clínicas odontológicas", Location = "São Paulo - SP", ResultCount = 30, CreatedAt = DateTime.UtcNow.AddDays(-12) },
            new() { Id = Guid.NewGuid(), UserId = DemoUserId, Niche = "Academias", Location = "Campinas - SP", ResultCount = 20, CreatedAt = DateTime.UtcNow.AddDays(-8) },
            new() { Id = Guid.NewGuid(), UserId = DemoUserId, Niche = "Restaurantes", Location = "Rio de Janeiro - RJ", ResultCount = 25, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = Guid.NewGuid(), UserId = DemoUserId, Niche = "Salões de beleza", Location = "Belo Horizonte - MG", ResultCount = 18, CreatedAt = DateTime.UtcNow.AddDays(-3) }
        };
    }
}
