using Microsoft.EntityFrameworkCore;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;

namespace Snic.Api.Repositories;

public interface IAcademicIndicatorRepository
{
    Task<int?> MaximumDaysPastDueAsync(long personId, CancellationToken ct);
    Task<IReadOnlyList<ScoreRule>> MatchingRulesAsync(int maximum, CancellationToken ct);
}

public sealed class AcademicIndicatorRepository(SnicDbContext db) : IAcademicIndicatorRepository
{
    public async Task<int?> MaximumDaysPastDueAsync(long personId, CancellationToken ct)
    {
        var productMaximum = await db.CreditProducts.Where(x => x.PersonId == personId && x.IsActive)
            .Select(x => (int?)x.LatestDaysPastDue).MaxAsync(ct);
        if (productMaximum is null) return null;
        var eventMaximum = await db.CreditHistoryEvents
            .Where(x => x.IsActive && x.CreditProduct.IsActive && x.CreditProduct.PersonId == personId)
            .Select(x => (int?)x.DaysPastDue).MaxAsync(ct);
        return Math.Max(productMaximum.Value, eventMaximum ?? 0);
    }

    public async Task<IReadOnlyList<ScoreRule>> MatchingRulesAsync(int maximum, CancellationToken ct) =>
        await db.ScoreRules.AsNoTracking().Where(x => x.IsActive && x.MinDaysPastDue <= maximum &&
            (x.MaxDaysPastDue == null || x.MaxDaysPastDue >= maximum)).ToListAsync(ct);
}
