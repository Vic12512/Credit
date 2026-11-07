using Snic.Api.Domain;

namespace Snic.Api.Repositories;

public interface IUserAccountRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken);
    Task<UserAccount?> FindForAuthenticationAsync(string username, CancellationToken cancellationToken);
    Task<UserAccount?> FindByIdAsync(long id, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken);
    Task<Role?> FindRoleByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> UsernameOrEmailExistsAsync(string username, string email, CancellationToken cancellationToken);
    void Add(UserAccount account);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
