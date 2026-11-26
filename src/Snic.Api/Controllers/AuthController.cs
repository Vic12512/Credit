using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Infrastructure;
using Snic.Api.Repositories;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthenticationService authentication, IUserAccountRepository accounts, RequestAuditState audit) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        audit.AttemptedUsername = request.Username.Trim()[..Math.Min(request.Username.Trim().Length, 100)];
        var result = await authentication.LoginAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : StatusCode(result.StatusCode, new { message = result.Error, code = "INVALID_CREDENTIALS" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Unauthorized();
        var account = await accounts.FindByIdAsync(id, false, cancellationToken);
        return account is null ? Unauthorized() : Ok(AuthenticationService.Map(account));
    }
}
