using Snic.Api.Contracts;
using Snic.Api.Repositories;

namespace Snic.Api.Services;

public sealed class BackendQueryService(IBackendQueryRepository queries)
{
    public Task<IReadOnlyList<CatalogItemResponse>> CatalogAsync(string catalog, CancellationToken ct) => queries.CatalogAsync(catalog, ct);
    public Task<IReadOnlyList<ScoreRuleResponse>> RulesAsync(CancellationToken ct) => queries.RulesAsync(ct);
    public Task<PagedResponse<AuditResponse>> AuditAsync(AuditQuery filter, CancellationToken ct) => queries.AuditAsync(filter, ct);
}
