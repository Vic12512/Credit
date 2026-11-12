using Snic.Api.Domain;

namespace Snic.Api.Repositories;

public interface ICreditProductRepository
{
    Task<IReadOnlyList<CreditProduct>> ListAsync(CancellationToken cancellationToken);
    Task<CreditProduct?> FindByIdAsync(long id, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreditProduct>> ListByPersonIdAsync(long personId, CancellationToken cancellationToken);
    Task<ProductType?> FindTypeByCodeAsync(string code, CancellationToken cancellationToken);
    Task<ProductStatus?> FindStatusByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> ExternalIdentifierExistsAsync(int institutionId, string identifier, long? excludingId, CancellationToken cancellationToken);
    void Add(CreditProduct product);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
