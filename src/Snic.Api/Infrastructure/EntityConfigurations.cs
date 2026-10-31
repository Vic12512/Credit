using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Snic.Api.Domain;

namespace Snic.Api.Infrastructure;

internal static class ConfigurationHelpers
{
    public static void ConfigureAuditFields<T>(EntityTypeBuilder<T> b) where T : class
    {
        b.Property("CreatedAt").HasColumnName("created_at").HasDefaultValueSql("SYSDATETIME()");
        b.Property("UpdatedAt").HasColumnName("updated_at");
        b.Property("DeactivatedAt").HasColumnName("deactivated_at");
        b.Property("DeactivationReason").HasColumnName("deactivation_reason").HasMaxLength(300);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsUnicode(false); b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(80); b.Property(x => x.Description).HasColumnName("description").HasMaxLength(300);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.HasData(new Role { Id = 1, Code = "ADMINISTRATOR", Name = "Administrador" }, new Role { Id = 2, Code = "CONSUMER", Name = "Consumidor" }, new Role { Id = 3, Code = "DATA_ENTRY", Name = "Registrador" });
    }
}

public sealed class InstitutionTypeConfiguration : IEntityTypeConfiguration<InstitutionType>
{
    public void Configure(EntityTypeBuilder<InstitutionType> b)
    {
        b.ToTable("institution_types"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsUnicode(false); b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100); b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.HasData(new InstitutionType { Id = 1, Code = "BANK", Name = "Banco" }, new InstitutionType { Id = 2, Code = "COOPERATIVE", Name = "Cooperativa" }, new InstitutionType { Id = 3, Code = "FINANCIAL_ENTITY", Name = "Entidad financiera" }, new InstitutionType { Id = 4, Code = "COMMERCIAL_COMPANY", Name = "Empresa comercial" });
    }
}

public sealed class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> b)
    {
        b.ToTable("institutions"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.InstitutionTypeId).HasColumnName("institution_type_id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsUnicode(false); b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Rnc).HasColumnName("rnc").HasMaxLength(11).IsUnicode(false); b.HasIndex(x => x.Rnc).IsUnique(); b.Property(x => x.LegalName).HasColumnName("legal_name").HasMaxLength(180); b.Property(x => x.CommercialName).HasColumnName("commercial_name").HasMaxLength(180);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true); ConfigurationHelpers.ConfigureAuditFields(b); b.HasIndex(x => new { x.InstitutionTypeId, x.IsActive }).HasDatabaseName("IX_institutions_type_active");
        b.HasOne(x => x.InstitutionType).WithMany().HasForeignKey(x => x.InstitutionTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> b)
    {
        b.ToTable("user_accounts"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.InstitutionId).HasColumnName("institution_id"); b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(100); b.HasIndex(x => x.Username).IsUnique(); b.Property(x => x.Email).HasColumnName("email").HasMaxLength(180); b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(180); b.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500); b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue((short)0); b.Property(x => x.LockedUntil).HasColumnName("locked_until"); b.Property(x => x.LastLoginAt).HasColumnName("last_login_at"); ConfigurationHelpers.ConfigureAuditFields(b);
        b.HasIndex(x => new { x.InstitutionId, x.IsActive }).HasDatabaseName("IX_user_accounts_institution_active"); b.HasIndex(x => new { x.RoleId, x.IsActive }).HasDatabaseName("IX_user_accounts_role_active");
        b.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_user_accounts_role_institution", "([role_id] = 1 AND [institution_id] IS NULL) OR ([role_id] <> 1 AND [institution_id] IS NOT NULL)"));
    }
}

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("persons"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.NationalId).HasColumnName("national_id").HasColumnType("char(11)"); b.HasIndex(x => x.NationalId).IsUnique();
        b.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(80); b.Property(x => x.MiddleName).HasColumnName("middle_name").HasMaxLength(80); b.Property(x => x.FirstSurname).HasColumnName("first_surname").HasMaxLength(80); b.Property(x => x.SecondSurname).HasColumnName("second_surname").HasMaxLength(80); b.Property(x => x.BirthDate).HasColumnName("birth_date"); b.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id"); b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true); ConfigurationHelpers.ConfigureAuditFields(b);
        b.HasIndex(x => new { x.FirstSurname, x.FirstName }).HasDatabaseName("IX_persons_name"); b.HasIndex(x => x.IsActive).HasDatabaseName("IX_persons_active"); b.HasOne(x => x.CreatedByAccount).WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public abstract class CodeCatalogConfiguration<T> : IEntityTypeConfiguration<T> where T : class
{
    protected abstract string TableName { get; }
    public void Configure(EntityTypeBuilder<T> b)
    {
        b.ToTable(TableName); b.HasKey("Id"); b.Property("Id").HasColumnName("id"); b.Property("Code").HasColumnName("code").HasMaxLength(40).IsUnicode(false); b.HasIndex("Code").IsUnique(); b.Property("Name").HasColumnName("name").HasMaxLength(100); b.Property("IsActive").HasColumnName("is_active").HasDefaultValue(true); ConfigureMore(b);
    }
    protected virtual void ConfigureMore(EntityTypeBuilder<T> b) { }
}
public sealed class ProductTypeConfiguration : CodeCatalogConfiguration<ProductType> { protected override string TableName => "product_types"; protected override void ConfigureMore(EntityTypeBuilder<ProductType> b) => b.HasData(new ProductType { Id = 1, Code = "CREDIT_CARD", Name = "Tarjeta de crédito" }, new ProductType { Id = 2, Code = "LOAN", Name = "Préstamo" }, new ProductType { Id = 3, Code = "MORTGAGE", Name = "Hipoteca" }); }
public sealed class ProductStatusConfiguration : CodeCatalogConfiguration<ProductStatus> { protected override string TableName => "product_statuses"; protected override void ConfigureMore(EntityTypeBuilder<ProductStatus> b) => b.HasData(new ProductStatus { Id = 1, Code = "OPEN", Name = "Abierto" }, new ProductStatus { Id = 2, Code = "CLOSED", Name = "Cerrado" }, new ProductStatus { Id = 3, Code = "CANCELLED", Name = "Cancelado" }); }
public sealed class CreditEventTypeConfiguration : CodeCatalogConfiguration<CreditEventType> { protected override string TableName => "credit_event_types"; protected override void ConfigureMore(EntityTypeBuilder<CreditEventType> b) { b.Property(x => x.Description).HasColumnName("description").HasMaxLength(300); b.HasData(new CreditEventType { Id = 1, Code = "PRODUCT_OPENED", Name = "Producto abierto" }, new CreditEventType { Id = 2, Code = "PERIOD_REPORTED", Name = "Período reportado" }, new CreditEventType { Id = 3, Code = "BALANCE_UPDATED", Name = "Saldo actualizado" }, new CreditEventType { Id = 4, Code = "PAYMENT_REPORTED", Name = "Pago reportado" }, new CreditEventType { Id = 5, Code = "DELINQUENCY_REPORTED", Name = "Mora reportada" }, new CreditEventType { Id = 6, Code = "PRODUCT_CLOSED", Name = "Producto cerrado" }, new CreditEventType { Id = 7, Code = "PRODUCT_CANCELLED", Name = "Producto cancelado" }); } }
public sealed class PaymentStatusConfiguration : CodeCatalogConfiguration<PaymentStatus> { protected override string TableName => "payment_statuses"; protected override void ConfigureMore(EntityTypeBuilder<PaymentStatus> b) => b.HasData(new PaymentStatus { Id = 1, Code = "PENDING", Name = "Pendiente" }, new PaymentStatus { Id = 2, Code = "PARTIAL", Name = "Parcial" }, new PaymentStatus { Id = 3, Code = "PAID_ON_TIME", Name = "Pagado a tiempo" }, new PaymentStatus { Id = 4, Code = "PAID_LATE", Name = "Pagado tarde" }, new PaymentStatus { Id = 5, Code = "OVERDUE", Name = "Vencido" }); }

public sealed class CreditProductConfiguration : IEntityTypeConfiguration<CreditProduct>
{
    public void Configure(EntityTypeBuilder<CreditProduct> b)
    {
        b.ToTable("credit_products"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.PersonId).HasColumnName("person_id"); b.Property(x => x.InstitutionId).HasColumnName("institution_id"); b.Property(x => x.ProductTypeId).HasColumnName("product_type_id"); b.Property(x => x.ProductStatusId).HasColumnName("product_status_id"); b.Property(x => x.ExternalProductIdentifier).HasColumnName("external_product_identifier").HasMaxLength(100); b.Property(x => x.ApprovedAmount).HasColumnName("approved_amount").HasPrecision(18, 2); b.Property(x => x.CurrentBalance).HasColumnName("current_balance").HasPrecision(18, 2).HasDefaultValue(0m); b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasDefaultValue("DOP"); b.Property(x => x.OpeningDate).HasColumnName("opening_date"); b.Property(x => x.ClosingDate).HasColumnName("closing_date"); b.Property(x => x.LatestReportDate).HasColumnName("latest_report_date"); b.Property(x => x.LatestDaysPastDue).HasColumnName("latest_days_past_due").HasDefaultValue(0); b.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id"); b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true); ConfigurationHelpers.ConfigureAuditFields(b);
        b.HasIndex(x => new { x.InstitutionId, x.ExternalProductIdentifier }).IsUnique().HasDatabaseName("UQ_credit_products_institution_external_id"); b.HasIndex(x => new { x.PersonId, x.IsActive }).HasDatabaseName("IX_credit_products_person_active"); b.HasIndex(x => new { x.InstitutionId, x.ProductStatusId }).HasDatabaseName("IX_credit_products_institution_status"); b.HasIndex(x => new { x.PersonId, x.InstitutionId, x.ProductTypeId }).HasDatabaseName("IX_credit_products_person_institution_type");
        b.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.ProductType).WithMany().HasForeignKey(x => x.ProductTypeId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.ProductStatus).WithMany().HasForeignKey(x => x.ProductStatusId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.CreatedByAccount).WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CreditHistoryEventConfiguration : IEntityTypeConfiguration<CreditHistoryEvent>
{
    public void Configure(EntityTypeBuilder<CreditHistoryEvent> b)
    {
        b.ToTable("credit_history_events"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.CreditProductId).HasColumnName("credit_product_id"); b.Property(x => x.CreditEventTypeId).HasColumnName("credit_event_type_id"); b.Property(x => x.PaymentStatusId).HasColumnName("payment_status_id"); b.Property(x => x.ProductStatusAfterId).HasColumnName("product_status_after_id"); b.Property(x => x.EventDate).HasColumnName("event_date"); b.Property(x => x.PeriodStart).HasColumnName("period_start"); b.Property(x => x.PeriodEnd).HasColumnName("period_end"); b.Property(x => x.DueDate).HasColumnName("due_date"); b.Property(x => x.AmountDue).HasColumnName("amount_due").HasPrecision(18, 2); b.Property(x => x.AmountPaid).HasColumnName("amount_paid").HasPrecision(18, 2); b.Property(x => x.BalanceAfter).HasColumnName("balance_after").HasPrecision(18, 2); b.Property(x => x.DaysPastDue).HasColumnName("days_past_due").HasDefaultValue(0); b.Property(x => x.SourceReference).HasColumnName("source_reference").HasMaxLength(100); b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500); b.Property(x => x.ReportedByAccountId).HasColumnName("reported_by_account_id"); b.Property(x => x.ReportedAt).HasColumnName("reported_at").HasDefaultValueSql("SYSDATETIME()"); b.Property(x => x.SupersedesEventId).HasColumnName("supersedes_event_id"); b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true); b.Property(x => x.DeactivatedAt).HasColumnName("deactivated_at"); b.Property(x => x.DeactivationReason).HasColumnName("deactivation_reason").HasMaxLength(300);
        b.HasIndex(x => new { x.CreditProductId, x.EventDate }).HasDatabaseName("IX_credit_history_product_date"); b.HasIndex(x => new { x.EventDate, x.DaysPastDue }).HasDatabaseName("IX_credit_history_score_window"); b.HasIndex(x => new { x.ReportedByAccountId, x.ReportedAt }).HasDatabaseName("IX_credit_history_reporter_date"); b.HasIndex(x => new { x.CreditProductId, x.SourceReference }).HasDatabaseName("IX_credit_history_product_source_reference");
        b.HasOne(x => x.CreditProduct).WithMany().HasForeignKey(x => x.CreditProductId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.CreditEventType).WithMany().HasForeignKey(x => x.CreditEventTypeId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.PaymentStatus).WithMany().HasForeignKey(x => x.PaymentStatusId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.ProductStatusAfter).WithMany().HasForeignKey(x => x.ProductStatusAfterId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.ReportedByAccount).WithMany().HasForeignKey(x => x.ReportedByAccountId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.SupersedesEvent).WithMany().HasForeignKey(x => x.SupersedesEventId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ScoreRuleConfiguration : IEntityTypeConfiguration<ScoreRule>
{
    public void Configure(EntityTypeBuilder<ScoreRule> b)
    {
        b.ToTable("score_rules"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.MinDaysPastDue).HasColumnName("min_days_past_due"); b.Property(x => x.MaxDaysPastDue).HasColumnName("max_days_past_due"); b.Property(x => x.Score).HasColumnName("score"); b.Property(x => x.RiskLevel).HasColumnName("risk_level").HasMaxLength(20).IsUnicode(false); b.Property(x => x.EstimatedDefaultPercentage).HasColumnName("estimated_default_percentage").HasPrecision(5, 2); b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true); b.HasIndex(x => new { x.MinDaysPastDue, x.MaxDaysPastDue }).IsUnique().HasDatabaseName("UQ_score_rules_range");
        b.HasData(new ScoreRule { Id = 1, MinDaysPastDue = 0, MaxDaysPastDue = 0, Score = 100, RiskLevel = "LOW", EstimatedDefaultPercentage = 0 }, new ScoreRule { Id = 2, MinDaysPastDue = 1, MaxDaysPastDue = 30, Score = 80, RiskLevel = "LOW", EstimatedDefaultPercentage = 20 }, new ScoreRule { Id = 3, MinDaysPastDue = 31, MaxDaysPastDue = 60, Score = 60, RiskLevel = "MEDIUM", EstimatedDefaultPercentage = 40 }, new ScoreRule { Id = 4, MinDaysPastDue = 61, MaxDaysPastDue = 90, Score = 40, RiskLevel = "HIGH", EstimatedDefaultPercentage = 60 }, new ScoreRule { Id = 5, MinDaysPastDue = 91, MaxDaysPastDue = null, Score = 20, RiskLevel = "HIGH", EstimatedDefaultPercentage = 80 });
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.AccountId).HasColumnName("account_id"); b.Property(x => x.InstitutionId).HasColumnName("institution_id"); b.Property(x => x.PersonId).HasColumnName("person_id"); b.Property(x => x.Operation).HasColumnName("operation").HasMaxLength(60).IsUnicode(false); b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(60).IsUnicode(false); b.Property(x => x.EntityId).HasColumnName("entity_id"); b.Property(x => x.Result).HasColumnName("result").HasMaxLength(20).IsUnicode(false); b.Property(x => x.AttemptedUsername).HasColumnName("attempted_username").HasMaxLength(100); b.Property(x => x.OldValues).HasColumnName("old_values").HasColumnType("nvarchar(max)"); b.Property(x => x.NewValues).HasColumnName("new_values").HasColumnType("nvarchar(max)"); b.Property(x => x.Details).HasColumnName("details").HasMaxLength(1000); b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45).IsUnicode(false); b.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasDefaultValueSql("SYSDATETIME()");
        b.HasIndex(x => x.OccurredAt).HasDatabaseName("IX_audit_logs_date"); b.HasIndex(x => new { x.AccountId, x.OccurredAt }).HasDatabaseName("IX_audit_logs_account_date"); b.HasIndex(x => new { x.InstitutionId, x.OccurredAt }).HasDatabaseName("IX_audit_logs_institution_date"); b.HasIndex(x => new { x.PersonId, x.OccurredAt }).HasDatabaseName("IX_audit_logs_person_date"); b.HasIndex(x => new { x.Operation, x.Result, x.OccurredAt }).HasDatabaseName("IX_audit_logs_operation_result_date");
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}
