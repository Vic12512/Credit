using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Route("api/v1/setup")]
public sealed class SetupController(IAuthenticationService authentication) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Setup(SetupRequest request, CancellationToken cancellationToken)
    {
        var result = await authentication.SetupAsync(request, cancellationToken);
        return result.Succeeded
            ? StatusCode(result.StatusCode, result.Value)
            : StatusCode(result.StatusCode, new { message = result.Error });
    }
}
