using Snic.Api.Domain;

namespace Snic.Api.Repositories;

public interface IPersonRepository
{
    Task<IReadOnlyList<Person>> ListAsync(CancellationToken cancellationToken);
    Task<Person?> FindByIdAsync(long id, bool tracked, CancellationToken cancellationToken);
    Task<Person?> FindByNationalIdAsync(string nationalId, bool tracked, CancellationToken cancellationToken);
    Task<bool> NationalIdExistsAsync(string nationalId, long? excludingId, CancellationToken cancellationToken);
    void Add(Person person);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
