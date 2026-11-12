using Snic.Api.Contracts;

namespace Snic.Api.Services;

public interface ICreditProductService
{
    Task<IReadOnlyList<CreditProductResponse>> ListAsync(CancellationToken cancellationToken);
    Task<CreditProductResponse?> FindAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<CreditProductResponse>> CreateAsync(CreateCreditProductRequest request, long accountId, CancellationToken cancellationToken);
    Task<ServiceResult<CreditProductResponse>> UpdateAsync(long id, UpdateCreditProductRequest request, long accountId, string? ipAddress, CancellationToken cancellationToken);
    Task<ServiceResult<ConsolidatedCreditReportResponse>> GetConsolidatedAsync(string nationalId, CancellationToken cancellationToken);
}
