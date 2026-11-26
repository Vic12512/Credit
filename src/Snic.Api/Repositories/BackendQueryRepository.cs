using Microsoft.EntityFrameworkCore;
using Snic.Api.Contracts;
using Snic.Api.Infrastructure;

namespace Snic.Api.Repositories;

public sealed class BackendQueryRepository(SnicDbContext db) : IBackendQueryRepository
{
    public async Task<IReadOnlyList<CatalogItemResponse>> CatalogAsync(string catalog, CancellationToken ct) => catalog switch
    {
        "roles" => await db.Roles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id).Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name)).ToListAsync(ct),
        "institution-types" => await db.InstitutionTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id).Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name)).ToListAsync(ct),
        "product-types" => await db.ProductTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id).Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name)).ToListAsync(ct),
        "product-statuses" => await db.ProductStatuses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id).Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name)).ToListAsync(ct),
        _ => throw new ArgumentOutOfRangeException(nameof(catalog))
    };

    public async Task<IReadOnlyList<ScoreRuleResponse>> RulesAsync(CancellationToken ct) =>
        await db.ScoreRules.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.MinDaysPastDue)
            .Select(x => new ScoreRuleResponse(x.Id, x.MinDaysPastDue, x.MaxDaysPastDue, x.Score, x.RiskLevel, x.EstimatedDefaultPercentage, true)).ToListAsync(ct);

    public async Task<PagedResponse<AuditResponse>> AuditAsync(AuditQuery filter, CancellationToken ct)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (filter.From is not null) query = query.Where(x => x.OccurredAt >= filter.From.Value.UtcDateTime);
        if (filter.To is not null) query = query.Where(x => x.OccurredAt <= filter.To.Value.UtcDateTime);
        if (filter.AccountId is not null) query = query.Where(x => x.AccountId == filter.AccountId);
        if (filter.InstitutionId is not null) query = query.Where(x => x.InstitutionId == filter.InstitutionId);
        if (filter.PersonId is not null) query = query.Where(x => x.PersonId == filter.PersonId);
        if (!string.IsNullOrWhiteSpace(filter.Operation)) query = query.Where(x => x.Operation == filter.Operation);
        if (!string.IsNullOrWhiteSpace(filter.Result)) query = query.Where(x => x.Result == filter.Result);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(x => new AuditResponse(x.Id, x.OccurredAt, x.AccountId, x.Account == null ? null : x.Account.Username,
                x.InstitutionId, x.PersonId, x.Operation, x.EntityType, x.EntityId, x.Result, x.AttemptedUsername,
                x.OldValues, x.NewValues, x.Details, x.IpAddress)).ToListAsync(ct);
        return new(items, count, filter.Page, filter.PageSize);
    }
}
