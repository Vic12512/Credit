using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Snic.Api.Contracts;

namespace Snic.Api.Infrastructure;

public static class ApiErrors
{
    public static ApiError Create(HttpContext context, int status, string? message = null, string? code = null,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        code ?? status switch
        {
            400 => "INVALID_DATA",
            401 => "AUTHENTICATION_REQUIRED",
            403 => "INSUFFICIENT_PERMISSIONS",
            404 when context.Request.Path.StartsWithSegments("/api/v1/credit-reports") || context.Request.Path.StartsWithSegments("/api/v1/persons") => "PERSON_NOT_FOUND",
            404 => "RESOURCE_NOT_FOUND",
            409 => "CONFLICT",
            405 => "METHOD_NOT_ALLOWED",
            415 => "UNSUPPORTED_MEDIA_TYPE",
            _ => "INTERNAL_ERROR"
        },
        message ?? status switch
        {
            400 => "Los datos enviados no son válidos.",
            401 => "Se requiere un token válido para acceder.",
            403 => "No tienes permisos para realizar esta operación.",
            404 => "El recurso solicitado no existe.",
            409 => "El registro entra en conflicto con datos existentes.",
            405 => "El método HTTP no está permitido para esta ruta.",
            415 => "El formato del contenido no es compatible; utiliza application/json.",
            _ => "No fue posible completar la operación."
        }, Activity.Current?.Id ?? context.TraceIdentifier, errors);

    public static int StatusForException(Exception exception)
    {
        var sql = (exception as DbUpdateException)?.InnerException as SqlException;
        return sql?.Number switch { 2601 or 2627 => 409, 547 or 2628 => 400, _ => 500 };
    }
}

public sealed class ApiResponseFilter(RequestAuditState audit) : IAsyncAlwaysRunResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult result)
        {
            switch (result.Value)
            {
                case LoginResponse login:
                    audit.AccountId = login.Account.Id;
                    audit.InstitutionId = login.Account.InstitutionId;
                    break;
                case AccountResponse account:
                    audit.EntityId = account.Id;
                    audit.InstitutionId = account.InstitutionId;
                    break;
                case PersonResponse person:
                    audit.EntityId = person.Id;
                    audit.PersonId = person.Id;
                    break;
                case InstitutionResponse institution:
                    audit.EntityId = institution.Id;
                    audit.InstitutionId = institution.Id;
                    break;
                case CreditProductResponse product:
                    audit.EntityId = product.Id;
                    audit.PersonId = product.PersonId;
                    audit.InstitutionId = product.InstitutionId;
                    break;
                case ConsolidatedCreditReportResponse report:
                    audit.PersonId = report.Person.Id;
                    audit.EntityId = report.Person.Id;
                    break;
            }
            if (result.StatusCode is >= 400 && result.Value is not ApiError)
            {
                var message = result.Value?.GetType().GetProperty("message")?.GetValue(result.Value) as string;
                var code = result.Value?.GetType().GetProperty("code")?.GetValue(result.Value) as string;
                result.Value = ApiErrors.Create(context.HttpContext, result.StatusCode.Value, message, code);
                result.DeclaredType = typeof(ApiError);
            }
        }
        await next();
    }
}

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var status = ApiErrors.StatusForException(exception);
            // No se registran cuerpos, cadenas de conexión ni mensajes SQL con datos del usuario.
            logger.LogError("Fallo de API {ExceptionType}; traceId {TraceId}.", exception.GetType().Name, context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(ApiErrors.Create(context, status));
        }
    }
}
