using Snic.Api.Contracts;

namespace Snic.Api.Services;

public interface IAccountService
{
    Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken);
    Task<AccountResponse?> FindAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<AccountResponse>> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<AccountResponse>> ChangeStatusAsync(long id, long currentAccountId, ChangeAccountStatusRequest request, CancellationToken cancellationToken);
}
