using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRATOR,CONSUMER,DATA_ENTRY")]
[Route("api/v1/persons")]
public sealed class PersonsController(IPersonService persons) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PersonResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await persons.ListAsync(cancellationToken));

    [HttpGet("{id:long}")]
    [ProducesResponseType<PersonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Find(long id, CancellationToken cancellationToken)
    {
        var person = await persons.FindAsync(id, cancellationToken);
        return person is null ? NotFound(new { message = "La persona no existe." }) : Ok(person);
    }

    [HttpPost]
    [Authorize(Roles = "DATA_ENTRY")]
    [ProducesResponseType<PersonResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreatePersonRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
            return Unauthorized();
        var result = await persons.CreateAsync(request, accountId, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(Find), new { id = result.Value!.Id }, result.Value)
            : StatusCode(result.StatusCode, new { message = result.Error });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMINISTRATOR")]
    [ProducesResponseType<PersonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, UpdatePersonRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
            return Unauthorized();
        var result = await persons.UpdateAsync(id, request, accountId, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : StatusCode(result.StatusCode, new { message = result.Error });
    }

    private bool TryGetAccountId(out long accountId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
}
