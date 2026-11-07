using Microsoft.EntityFrameworkCore;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;

namespace Snic.Api.Repositories;

public sealed class UserAccountRepository(SnicDbContext dbContext) : IUserAccountRepository
{
    public Task<bool> AnyAsync(CancellationToken cancellationToken) => dbContext.UserAccounts.AnyAsync(cancellationToken);

    public Task<UserAccount?> FindForAuthenticationAsync(string username, CancellationToken cancellationToken) =>
        dbContext.UserAccounts.Include(x => x.Role).Include(x => x.Institution)
            .SingleOrDefaultAsync(x => x.Username == username, cancellationToken);

    public Task<UserAccount?> FindByIdAsync(long id, bool tracked, CancellationToken cancellationToken)
    {
        var query = dbContext.UserAccounts.Include(x => x.Role).Include(x => x.Institution).AsQueryable();
        if (!tracked) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.UserAccounts.AsNoTracking().Include(x => x.Role).Include(x => x.Institution)
            .OrderBy(x => x.Username).ToListAsync(cancellationToken);

    public Task<Role?> FindRoleByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Roles.SingleOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

    public Task<bool> UsernameOrEmailExistsAsync(string username, string email, CancellationToken cancellationToken) =>
        dbContext.UserAccounts.AnyAsync(x => x.Username == username || x.Email == email, cancellationToken);

    public void Add(UserAccount account) => dbContext.UserAccounts.Add(account);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
