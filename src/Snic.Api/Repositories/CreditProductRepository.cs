using Microsoft.EntityFrameworkCore;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;

namespace Snic.Api.Repositories;

public sealed class CreditProductRepository(SnicDbContext dbContext) : ICreditProductRepository
{
    public async Task<IReadOnlyList<CreditProduct>> ListAsync(CancellationToken cancellationToken) =>
        await FullQuery(false).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public Task<CreditProduct?> FindByIdAsync(long id, bool tracked, CancellationToken cancellationToken) =>
        FullQuery(tracked).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CreditProduct>> ListByPersonIdAsync(long personId, CancellationToken cancellationToken) =>
        await FullQuery(false).Where(x => x.PersonId == personId && x.IsActive).OrderByDescending(x => x.OpeningDate).ToListAsync(cancellationToken);

    public Task<ProductType?> FindTypeByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.ProductTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

    public Task<ProductStatus?> FindStatusByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.ProductStatuses.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

    public Task<bool> ExternalIdentifierExistsAsync(int institutionId, string identifier, long? excludingId, CancellationToken cancellationToken) =>
        dbContext.CreditProducts.AnyAsync(x => x.InstitutionId == institutionId && x.ExternalProductIdentifier == identifier && (!excludingId.HasValue || x.Id != excludingId), cancellationToken);

    public void Add(CreditProduct product) => dbContext.CreditProducts.Add(product);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<CreditProduct> FullQuery(bool tracked)
    {
        var query = dbContext.CreditProducts
            .Include(x => x.Person)
            .Include(x => x.Institution)
            .Include(x => x.ProductType)
            .Include(x => x.ProductStatus);
        return tracked ? query : query.AsNoTracking();
    }
}
