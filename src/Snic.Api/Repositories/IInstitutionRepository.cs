using Snic.Api.Domain;

namespace Snic.Api.Repositories;

public interface IInstitutionRepository
{
    Task<IReadOnlyList<Institution>> ListAsync(CancellationToken cancellationToken);
    Task<Institution?> FindByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> CodeOrRncExistsAsync(string code, string rnc, CancellationToken cancellationToken);
    Task<bool> InstitutionTypeExistsAsync(short id, CancellationToken cancellationToken);
    void Add(Institution institution);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
