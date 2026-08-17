using Microsoft.EntityFrameworkCore;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<SearchHistory> Searches => Set<SearchHistory>();
    public DbSet<UserProfile> Users => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration for Lead
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("leads");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();

            entity.Property(e => e.Nome).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Categoria).HasColumnName("category").HasMaxLength(150);
            entity.Property(e => e.Telefone).HasColumnName("phone").HasMaxLength(50);
            entity.Property(e => e.WhatsApp).HasColumnName("whatsapp").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);

            entity.Property(e => e.Endereco).HasColumnName("address").HasMaxLength(500);
            entity.Property(e => e.Cidade).HasColumnName("city").HasMaxLength(150);
            entity.Property(e => e.Estado).HasColumnName("state").HasMaxLength(50);
            entity.Property(e => e.CEP).HasColumnName("cep").HasMaxLength(20);

            entity.Property(e => e.Website).HasColumnName("website").HasMaxLength(500);
            entity.Property(e => e.Instagram).HasColumnName("instagram").HasMaxLength(150);

            entity.Property(e => e.Avaliacao).HasColumnName("rating").HasColumnType("decimal(3,2)");
            entity.Property(e => e.QuantidadeAvaliacoes).HasColumnName("reviews_count");

            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");

            entity.Property(e => e.Observacoes).HasColumnName("notes");
            entity.Property(e => e.Status)
                  .HasColumnName("status")
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.Property(e => e.Fonte).HasColumnName("source").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
        });

        // Configuration for SearchHistory
        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.ToTable("searches");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Niche).HasColumnName("niche").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ResultCount).HasColumnName("result_count");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
        });

        // Configuration for UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
