using System.Data;
using Microsoft.EntityFrameworkCore;
using Snic.Api.Contracts;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;
using Snic.Api.Repositories;

namespace Snic.Api.Services;

public sealed class AuthenticationService(
    SnicDbContext dbContext,
    IUserAccountRepository accounts,
    IPasswordService passwords,
    ITokenService tokens) : IAuthenticationService
{
    public async Task<ServiceResult<AccountResponse>> SetupAsync(SetupRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (await accounts.AnyAsync(cancellationToken))
            return ServiceResult<AccountResponse>.Fail("El sistema ya fue inicializado.", StatusCodes.Status409Conflict);

        var role = await accounts.FindRoleByCodeAsync("ADMINISTRATOR", cancellationToken);
        if (role is null)
            return ServiceResult<AccountResponse>.Fail("No existe el rol de administrador.", StatusCodes.Status500InternalServerError);

        var account = new UserAccount
        {
            RoleId = role.Id,
            Role = role,
            InstitutionId = null,
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwords.Hash(request.Password),
            IsActive = true
        };
        accounts.Add(account);
        await accounts.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ServiceResult<AccountResponse>.Ok(Map(account), StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var account = await accounts.FindForAuthenticationAsync(request.Username.Trim(), cancellationToken);
        if (account is null || !account.IsActive || account.Role is null || !account.Role.IsActive)
            return InvalidCredentials();

        if (account.LockedUntil is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
            return ServiceResult<LoginResponse>.Fail("La cuenta está temporalmente bloqueada.", StatusCodes.Status401Unauthorized);

        if (!passwords.Verify(request.Password, account.PasswordHash))
        {
            account.FailedLoginAttempts++;
            if (account.FailedLoginAttempts >= 5)
            {
                account.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                account.FailedLoginAttempts = 0;
            }
            await accounts.SaveChangesAsync(cancellationToken);
            return InvalidCredentials();
        }

        account.FailedLoginAttempts = 0;
        account.LockedUntil = null;
        account.LastLoginAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;
        await accounts.SaveChangesAsync(cancellationToken);
        var token = tokens.Create(account);
        return ServiceResult<LoginResponse>.Ok(new LoginResponse(token.Value, token.ExpiresAtUtc, Map(account)));
    }

    private static ServiceResult<LoginResponse> InvalidCredentials() =>
        ServiceResult<LoginResponse>.Fail("Usuario o contraseña incorrectos.", StatusCodes.Status401Unauthorized);

    internal static AccountResponse Map(UserAccount account) => new(
        account.Id, account.InstitutionId, account.Institution?.LegalName, account.Role.Code,
        account.Username, account.Email, account.DisplayName, account.IsActive,
        account.LastLoginAt, account.CreatedAt);
}
