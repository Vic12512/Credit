using System.ComponentModel.DataAnnotations;

namespace Snic.Api.Contracts;

public sealed record AcademicIndicatorsResponse(
    bool IsSimulated, string Status, string Message, int? MaximumDaysPastDue,
    int? Score, string? RiskLevel, decimal? EstimatedDefaultPercentage);

public sealed record CatalogItemResponse(short Id, string Code, string Name);
public sealed record ScoreRuleResponse(short Id, int MinDaysPastDue, int? MaxDaysPastDue,
    short Score, string RiskLevel, decimal EstimatedDefaultPercentage, bool IsSimulated = true);

public sealed class AuditQuery
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    [Range(1, long.MaxValue)] public long? AccountId { get; set; }
    [Range(1, int.MaxValue)] public int? InstitutionId { get; set; }
    [Range(1, long.MaxValue)] public long? PersonId { get; set; }
    [StringLength(60)] public string? Operation { get; set; }
    [StringLength(20)] public string? Result { get; set; }
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public sealed record AuditResponse(long Id, DateTime OccurredAt, long? AccountId,
    string? Username, int? InstitutionId, long? PersonId, string Operation,
    string? EntityType, long? EntityId, string Result, string? AttemptedUsername,
    string? OldValues, string? NewValues, string? Details, string? IpAddress);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ApiError(string Code, string Message, string TraceId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
