using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRATOR")]
[Route("api/v1/institutions")]
public sealed class InstitutionsController(IInstitutionService institutions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await institutions.ListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Find(int id, CancellationToken cancellationToken)
    {
        var institution = await institutions.FindAsync(id, cancellationToken);
        return institution is null ? NotFound(new { message = "La institución no existe." }) : Ok(institution);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInstitutionRequest request, CancellationToken cancellationToken)
    {
        var result = await institutions.CreateAsync(request, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(Find), new { id = result.Value!.Id }, result.Value)
            : StatusCode(result.StatusCode, new { message = result.Error });
    }
}
