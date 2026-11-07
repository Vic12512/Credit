using System.Text.RegularExpressions;
using Snic.Api.Contracts;

namespace Snic.Api.Services;

public sealed partial class DevelopmentAdminBootstrapper(
    IConfiguration configuration,
    IAuthenticationService authentication,
    ILogger<DevelopmentAdminBootstrapper> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var username = configuration["BootstrapAdmin:Username"]?.Trim();
        var password = configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Administrador inicial omitido: no se configuraron ambas variables BootstrapAdmin.");
            return;
        }

        if (username.Length is < 3 or > 64 || !ValidUsername().IsMatch(username))
            throw new InvalidOperationException("BootstrapAdmin__Username debe tener entre 3 y 64 caracteres y usar solo letras, números, punto, guion o guion bajo.");

        if (password.Length is < 12 or > 128)
            throw new InvalidOperationException("BootstrapAdmin__Password debe tener entre 12 y 128 caracteres.");

        var result = await authentication.SetupAsync(new SetupRequest(
            username,
            $"{username.ToLowerInvariant()}@snic.local",
            "Administrador SNIC",
            password), cancellationToken);

        if (result.StatusCode == StatusCodes.Status409Conflict)
        {
            logger.LogInformation("Administrador inicial omitido: la base de datos ya contiene cuentas.");
            return;
        }

        if (!result.Succeeded)
            throw new InvalidOperationException(result.Error ?? "No fue posible crear el administrador inicial.");

        logger.LogInformation("Administrador inicial '{Username}' creado correctamente.", username);
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex ValidUsername();
}
