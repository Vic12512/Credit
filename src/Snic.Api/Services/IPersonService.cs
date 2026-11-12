using Snic.Api.Contracts;

namespace Snic.Api.Services;

public interface IPersonService
{
    Task<IReadOnlyList<PersonResponse>> ListAsync(CancellationToken cancellationToken);
    Task<PersonResponse?> FindAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<PersonResponse>> CreateAsync(CreatePersonRequest request, long accountId, CancellationToken cancellationToken);
    Task<ServiceResult<PersonResponse>> UpdateAsync(long id, UpdatePersonRequest request, long accountId, string? ipAddress, CancellationToken cancellationToken);
}
