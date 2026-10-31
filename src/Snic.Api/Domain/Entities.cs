namespace Snic.Api.Domain;

public sealed class Role
{
    public short Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class InstitutionType
{
    public short Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Institution
{
    public int Id { get; set; }
    public short InstitutionTypeId { get; set; }
    public required string Code { get; set; }
    public required string Rnc { get; set; }
    public required string LegalName { get; set; }
    public string? CommercialName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public InstitutionType InstitutionType { get; set; } = null!;
}

public sealed class UserAccount
{
    public long Id { get; set; }
    public int? InstitutionId { get; set; }
    public short RoleId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public short FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public Institution? Institution { get; set; }
    public Role Role { get; set; } = null!;
}

public sealed class Person
{
    public long Id { get; set; }
    public required string NationalId { get; set; }
    public required string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public required string FirstSurname { get; set; }
    public string? SecondSurname { get; set; }
    public DateOnly? BirthDate { get; set; }
    public long CreatedByAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public UserAccount CreatedByAccount { get; set; } = null!;
}

public sealed class ProductType
{
    public short Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductStatus
{
    public short Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreditProduct
{
    public long Id { get; set; }
    public long PersonId { get; set; }
    public int InstitutionId { get; set; }
    public short ProductTypeId { get; set; }
    public short ProductStatusId { get; set; }
    public required string ExternalProductIdentifier { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public string CurrencyCode { get; set; } = "DOP";
    public DateOnly OpeningDate { get; set; }
    public DateOnly? ClosingDate { get; set; }
    public DateOnly? LatestReportDate { get; set; }
    public int LatestDaysPastDue { get; set; }
    public long CreatedByAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public Person Person { get; set; } = null!;
    public Institution Institution { get; set; } = null!;
    public ProductType ProductType { get; set; } = null!;
    public ProductStatus ProductStatus { get; set; } = null!;
    public UserAccount CreatedByAccount { get; set; } = null!;
}

public sealed class CreditEventType
{
    public short Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PaymentStatus
{
    public short Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreditHistoryEvent
{
    public long Id { get; set; }
    public long CreditProductId { get; set; }
    public short CreditEventTypeId { get; set; }
    public short? PaymentStatusId { get; set; }
    public short? ProductStatusAfterId { get; set; }
    public DateOnly EventDate { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal? AmountDue { get; set; }
    public decimal? AmountPaid { get; set; }
    public decimal? BalanceAfter { get; set; }
    public int DaysPastDue { get; set; }
    public string? SourceReference { get; set; }
    public string? Description { get; set; }
    public long ReportedByAccountId { get; set; }
    public DateTime ReportedAt { get; set; }
    public long? SupersedesEventId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public CreditProduct CreditProduct { get; set; } = null!;
    public CreditEventType CreditEventType { get; set; } = null!;
    public PaymentStatus? PaymentStatus { get; set; }
    public ProductStatus? ProductStatusAfter { get; set; }
    public UserAccount ReportedByAccount { get; set; } = null!;
    public CreditHistoryEvent? SupersedesEvent { get; set; }
}

public sealed class ScoreRule
{
    public short Id { get; set; }
    public int MinDaysPastDue { get; set; }
    public int? MaxDaysPastDue { get; set; }
    public short Score { get; set; }
    public required string RiskLevel { get; set; }
    public decimal EstimatedDefaultPercentage { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AuditLog
{
    public long Id { get; set; }
    public long? AccountId { get; set; }
    public int? InstitutionId { get; set; }
    public long? PersonId { get; set; }
    public required string Operation { get; set; }
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public required string Result { get; set; }
    public string? AttemptedUsername { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; }
    public UserAccount? Account { get; set; }
    public Institution? Institution { get; set; }
    public Person? Person { get; set; }
}
