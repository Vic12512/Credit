using Snic.Api.Contracts;

namespace Snic.Api.Services;

public interface IAuthenticationService
{
    Task<ServiceResult<AccountResponse>> SetupAsync(SetupRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
