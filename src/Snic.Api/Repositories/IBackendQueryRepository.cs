using Snic.Api.Contracts;

namespace Snic.Api.Repositories;

public interface IBackendQueryRepository
{
    Task<IReadOnlyList<CatalogItemResponse>> CatalogAsync(string catalog, CancellationToken ct);
    Task<IReadOnlyList<ScoreRuleResponse>> RulesAsync(CancellationToken ct);
    Task<PagedResponse<AuditResponse>> AuditAsync(AuditQuery filter, CancellationToken ct);
}
