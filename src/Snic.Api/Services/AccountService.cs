using Snic.Api.Contracts;
using Snic.Api.Domain;
using Snic.Api.Repositories;

namespace Snic.Api.Services;

public sealed class AccountService(
    IUserAccountRepository accounts,
    IInstitutionRepository institutions,
    IPasswordService passwords) : IAccountService
{
    private static readonly HashSet<string> AllowedRoles = ["ADMINISTRATOR", "CONSUMER", "DATA_ENTRY"];

    public async Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await accounts.ListAsync(cancellationToken)).Select(AuthenticationService.Map).ToList();

    public async Task<AccountResponse?> FindAsync(long id, CancellationToken cancellationToken)
    {
        var account = await accounts.FindByIdAsync(id, false, cancellationToken);
        return account is null ? null : AuthenticationService.Map(account);
    }

    public async Task<ServiceResult<AccountResponse>> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var roleCode = request.RoleCode.Trim().ToUpperInvariant();
        if (!AllowedRoles.Contains(roleCode))
            return ServiceResult<AccountResponse>.Fail("El rol indicado no es válido.", StatusCodes.Status400BadRequest);

        var role = await accounts.FindRoleByCodeAsync(roleCode, cancellationToken);
        if (role is null)
            return ServiceResult<AccountResponse>.Fail("El rol no existe o está inactivo.", StatusCodes.Status400BadRequest);

        if (roleCode == "ADMINISTRATOR" && request.InstitutionId is not null)
            return ServiceResult<AccountResponse>.Fail("Una cuenta administradora no pertenece a una institución.", StatusCodes.Status400BadRequest);
        if (roleCode != "ADMINISTRATOR" && request.InstitutionId is null)
            return ServiceResult<AccountResponse>.Fail("La institución es obligatoria para esta cuenta.", StatusCodes.Status400BadRequest);
        if (request.InstitutionId is int institutionId && !await institutions.ExistsAsync(institutionId, cancellationToken))
            return ServiceResult<AccountResponse>.Fail("La institución no existe o está inactiva.", StatusCodes.Status400BadRequest);

        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await accounts.UsernameOrEmailExistsAsync(username, email, cancellationToken))
            return ServiceResult<AccountResponse>.Fail("El usuario o correo ya está registrado.", StatusCodes.Status409Conflict);

        var account = new UserAccount
        {
            InstitutionId = request.InstitutionId,
            RoleId = role.Id,
            Role = role,
            Username = username,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwords.Hash(request.Password),
            IsActive = true
        };
        accounts.Add(account);
        await accounts.SaveChangesAsync(cancellationToken);
        var created = await accounts.FindByIdAsync(account.Id, false, cancellationToken);
        return ServiceResult<AccountResponse>.Ok(AuthenticationService.Map(created!), StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<AccountResponse>> ChangeStatusAsync(long id, long currentAccountId, ChangeAccountStatusRequest request, CancellationToken cancellationToken)
    {
        if (id == currentAccountId && !request.IsActive)
            return ServiceResult<AccountResponse>.Fail("No puedes desactivar tu propia cuenta.", StatusCodes.Status400BadRequest);

        var account = await accounts.FindByIdAsync(id, true, cancellationToken);
        if (account is null)
            return ServiceResult<AccountResponse>.Fail("La cuenta no existe.", StatusCodes.Status404NotFound);

        account.IsActive = request.IsActive;
        account.UpdatedAt = DateTime.UtcNow;
        account.DeactivatedAt = request.IsActive ? null : DateTime.UtcNow;
        account.DeactivationReason = request.IsActive ? null : request.Reason?.Trim() ?? "Desactivada por un administrador.";
        await accounts.SaveChangesAsync(cancellationToken);
        return ServiceResult<AccountResponse>.Ok(AuthenticationService.Map(account));
    }
}
