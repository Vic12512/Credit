using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRATOR,CONSUMER,DATA_ENTRY")]
[Route("api/v1/credit-products")]
public sealed class CreditProductsController(ICreditProductService products) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CreditProductResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await products.ListAsync(cancellationToken));

    [HttpGet("{id:long}")]
    [ProducesResponseType<CreditProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Find(long id, CancellationToken cancellationToken)
    {
        var product = await products.FindAsync(id, cancellationToken);
        return product is null ? NotFound(new { message = "El producto crediticio no existe." }) : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "DATA_ENTRY")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<CreditProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateCreditProductRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
            return Unauthorized();
        var result = await products.CreateAsync(request, accountId, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(Find), new { id = result.Value!.Id }, result.Value)
            : StatusCode(result.StatusCode, new { message = result.Error });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "DATA_ENTRY")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<CreditProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, UpdateCreditProductRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
            return Unauthorized();
        var result = await products.UpdateAsync(id, request, accountId, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : StatusCode(result.StatusCode, new { message = result.Error });
    }

    private bool TryGetAccountId(out long accountId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
}
