using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Snic.Api.Domain;

namespace Snic.Api.Infrastructure;

public sealed class RequestAuditState
{
    public long? AccountId { get; set; }
    public int? InstitutionId { get; set; }
    public long? PersonId { get; set; }
    public long? EntityId { get; set; }
    public string? AttemptedUsername { get; set; }
    public bool DetailedAuditRecorded { get; set; }
}

public sealed class RequestAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RequestAuditState state, IServiceScopeFactory scopeFactory)
    {
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is null || !context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await next(context);
            return;
        }

        var original = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            try { await next(context); }
            catch (Exception exception)
            {
                context.Response.StatusCode = ApiErrors.StatusForException(exception);
                await PersistAsync(context, state, action, scopeFactory);
                throw;
            }
            if (!(state.DetailedAuditRecorded && context.Response.StatusCode < 400))
                await PersistAsync(context, state, action, scopeFactory);
            buffer.Position = 0;
            context.Response.Body = original;
            await buffer.CopyToAsync(original, context.RequestAborted);
        }
        finally { context.Response.Body = original; }
    }

    private static async Task PersistAsync(HttpContext context, RequestAuditState state,
        ControllerActionDescriptor action, IServiceScopeFactory scopeFactory)
    {
        // Contexto independiente: un error de escritura no debe volver a guardar entidades fallidas.
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SnicDbContext>();
        long? accountId = state.AccountId;
        if (accountId is null && context.User.Identity?.IsAuthenticated == true &&
            long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var authenticatedId))
            accountId = authenticatedId;
        var actor = accountId is null ? null : await db.UserAccounts.AsNoTracking()
            .Where(x => x.Id == accountId).Select(x => new { x.Id, x.InstitutionId }).SingleOrDefaultAsync();
        var institutionId = state.InstitutionId ?? actor?.InstitutionId;
        long? entityId = state.EntityId;
        if (entityId is null && long.TryParse(context.Request.RouteValues["id"]?.ToString(), out var routeId) && routeId > 0)
            entityId = routeId;
        long? personId = state.PersonId;
        if (action.ControllerName == "Persons") personId ??= entityId;
        if (action.ControllerName == "CreditProducts" && entityId is not null)
        {
            var product = await db.CreditProducts.AsNoTracking().Where(x => x.Id == entityId)
                .Select(x => new { x.PersonId, x.InstitutionId }).SingleOrDefaultAsync();
            if (product is not null) { personId ??= product.PersonId; institutionId = product.InstitutionId; }
        }
        if (action.ControllerName == "Accounts" && entityId is not null)
            institutionId ??= await db.UserAccounts.Where(x => x.Id == entityId).Select(x => x.InstitutionId).SingleOrDefaultAsync();
        if (action.ControllerName == "Institutions" && entityId is > 0 and <= int.MaxValue)
            institutionId ??= (int)entityId.Value;
        if (personId is not null && !await db.Persons.AnyAsync(x => x.Id == personId)) personId = null;
        if (institutionId is not null && !await db.Institutions.AnyAsync(x => x.Id == institutionId)) institutionId = null;

        db.AuditLogs.Add(new AuditLog
        {
            AccountId = actor?.Id,
            InstitutionId = institutionId,
            PersonId = personId,
            EntityType = action.ControllerName,
            EntityId = entityId,
            Operation = $"{action.ControllerName.ToUpperInvariant()}_{action.ActionName.ToUpperInvariant()}",
            Result = context.Response.StatusCode < 400 ? "SUCCESS" : context.Response.StatusCode is 401 or 403 ? "DENIED" : "FAILURE",
            AttemptedUsername = state.AttemptedUsername,
            Details = $"HTTP {context.Response.StatusCode}; {context.Request.Method}",
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
