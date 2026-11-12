using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Snic.Api.Infrastructure;

public sealed class SwaggerPermissionsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attributes = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
            .Concat(context.MethodInfo.GetCustomAttributes(true)).ToArray();
        if (attributes.OfType<AllowAnonymousAttribute>().Any())
        {
            operation.Security = [];
            operation.Description = "Acceso público. " + operation.Description;
            return;
        }

        var authorization = attributes.OfType<AuthorizeAttribute>().ToArray();
        if (authorization.Length == 0)
            return;

        var roles = new HashSet<string>(["ADMINISTRATOR", "CONSUMER", "DATA_ENTRY"]);
        foreach (var attribute in authorization.Where(x => !string.IsNullOrWhiteSpace(x.Roles)))
            roles.IntersectWith(attribute.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        operation.Description = $"Roles permitidos: {string.Join(", ", roles.Order())}. Requiere JWT de una cuenta activa. " + operation.Description;
        if (context.MethodInfo.DeclaringType.Name == "CreditProductsController" && context.MethodInfo.Name is "Create" or "Update")
            operation.Description += " La institución se obtiene de la cuenta autenticada. institutionId es opcional; si se envía, debe coincidir con la institución de la cuenta. Solo se pueden modificar productos propios de esa institución.";

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Token ausente, inválido, vencido o cuenta inactiva o bloqueada." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Rol no autorizado o producto de otra institución." });
    }
}
