using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snic.Api.Contracts;
using Snic.Api.Services;

namespace Snic.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRATOR,CONSUMER,DATA_ENTRY")]
[Route("api/v1/catalogs")]
public sealed class CatalogsController(BackendQueryService queries) : ControllerBase
{
    [HttpGet("roles")]
    [Authorize(Roles = "ADMINISTRATOR")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemResponse>>> Roles(CancellationToken ct) => Ok(await queries.CatalogAsync("roles", ct));

    [HttpGet("institution-types")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemResponse>>> InstitutionTypes(CancellationToken ct) => Ok(await queries.CatalogAsync("institution-types", ct));

    [HttpGet("product-types")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemResponse>>> ProductTypes(CancellationToken ct) => Ok(await queries.CatalogAsync("product-types", ct));

    [HttpGet("product-statuses")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemResponse>>> ProductStatuses(CancellationToken ct) => Ok(await queries.CatalogAsync("product-statuses", ct));

    [HttpGet("score-rules")]
    public async Task<ActionResult<IReadOnlyList<ScoreRuleResponse>>> ScoreRules(CancellationToken ct) => Ok(await queries.RulesAsync(ct));
}
