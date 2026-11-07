using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRATOR")]
[Route("api/v1/accounts")]
public sealed class AccountsController(IAccountService accounts) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await accounts.ListAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Find(long id, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(id, cancellationToken);
        return account is null ? NotFound(new { message = "La cuenta no existe." }) : Ok(account);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await accounts.CreateAsync(request, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(Find), new { id = result.Value!.Id }, result.Value)
            : StatusCode(result.StatusCode, new { message = result.Error });
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, ChangeAccountStatusRequest request, CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentAccountId))
            return Unauthorized();
        var result = await accounts.ChangeStatusAsync(id, currentAccountId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : StatusCode(result.StatusCode, new { message = result.Error });
    }
}
