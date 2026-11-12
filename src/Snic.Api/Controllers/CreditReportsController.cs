using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRATOR,CONSUMER,DATA_ENTRY")]
[Route("api/v1/credit-reports")]
public sealed class CreditReportsController(ICreditProductService products) : ControllerBase
{
    [HttpGet("{nationalId}")]
    [ProducesResponseType<ConsolidatedCreditReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNationalId(string nationalId, CancellationToken cancellationToken)
    {
        var result = await products.GetConsolidatedAsync(nationalId.Trim(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : StatusCode(result.StatusCode, new { message = result.Error });
    }
}
