using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Snic.Api.Domain;

namespace Snic.Api.Infrastructure;

public sealed class SnicDbContext(DbContextOptions<SnicDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<InstitutionType> InstitutionTypes => Set<InstitutionType>();
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<ProductStatus> ProductStatuses => Set<ProductStatus>();
    public DbSet<CreditProduct> CreditProducts => Set<CreditProduct>();
    public DbSet<CreditEventType> CreditEventTypes => Set<CreditEventType>();
    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
    public DbSet<CreditHistoryEvent> CreditHistoryEvents => Set<CreditHistoryEvent>();
    public DbSet<ScoreRule> ScoreRules => Set<ScoreRule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SnicDbContext).Assembly);
    }
}
