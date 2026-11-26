using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRATOR")]
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController(BackendQueryService queries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditResponse>>> List([FromQuery] AuditQuery filter, CancellationToken ct)
    {
        if (filter.From > filter.To)
            return BadRequest(new { message = "from no puede ser posterior a to." });
        return Ok(await queries.AuditAsync(filter, ct));
    }
}
