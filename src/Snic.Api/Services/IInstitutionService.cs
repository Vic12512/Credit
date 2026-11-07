using Snic.Api.Contracts;

namespace Snic.Api.Services;

public interface IInstitutionService
{
    Task<IReadOnlyList<InstitutionResponse>> ListAsync(CancellationToken cancellationToken);
    Task<InstitutionResponse?> FindAsync(int id, CancellationToken cancellationToken);
    Task<ServiceResult<InstitutionResponse>> CreateAsync(CreateInstitutionRequest request, CancellationToken cancellationToken);
}
