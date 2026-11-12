using System.ComponentModel.DataAnnotations;

namespace Snic.Api.Contracts;

public sealed record CreateCreditProductRequest(
    [Range(1, long.MaxValue)] long PersonId,
    [Range(1, int.MaxValue)] int? InstitutionId,
    [Required, StringLength(30)] string ProductTypeCode,
    [Required, StringLength(30)] string ProductStatusCode,
    [Required, StringLength(100)] string ExternalProductIdentifier,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal ApprovedAmount,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal CurrentBalance,
    [Required, RegularExpression("^[A-Za-z]{3}$")] string CurrencyCode,
    DateOnly OpeningDate,
    DateOnly? ClosingDate,
    DateOnly? LatestReportDate,
    [Range(0, int.MaxValue)] int LatestDaysPastDue);

public sealed record UpdateCreditProductRequest(
    [Range(1, long.MaxValue)] long PersonId,
    [Range(1, int.MaxValue)] int? InstitutionId,
    [Required, StringLength(30)] string ProductTypeCode,
    [Required, StringLength(30)] string ProductStatusCode,
    [Required, StringLength(100)] string ExternalProductIdentifier,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal ApprovedAmount,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal CurrentBalance,
    [Required, RegularExpression("^[A-Za-z]{3}$")] string CurrencyCode,
    DateOnly OpeningDate,
    DateOnly? ClosingDate,
    DateOnly? LatestReportDate,
    [Range(0, int.MaxValue)] int LatestDaysPastDue);

public sealed record CreditProductResponse(
    long Id,
    long PersonId,
    string PersonNationalId,
    int InstitutionId,
    string InstitutionName,
    string ProductTypeCode,
    string ProductTypeName,
    string ProductStatusCode,
    string ProductStatusName,
    string ExternalProductIdentifier,
    decimal ApprovedAmount,
    decimal CurrentBalance,
    string CurrencyCode,
    DateOnly OpeningDate,
    DateOnly? ClosingDate,
    DateOnly? LatestReportDate,
    int LatestDaysPastDue,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreditReportSummary(
    int ProductCount,
    decimal TotalApprovedAmount,
    decimal TotalOutstandingBalance);

public sealed record ConsolidatedCreditReportResponse(
    PersonResponse Person,
    bool HasCreditHistory,
    CreditReportSummary Summary,
    IReadOnlyList<CreditProductResponse> Products);
