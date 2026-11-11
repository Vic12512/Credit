using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Snic.Api.Infrastructure;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var controller = method.DeclaringType;
        var methodAttributes = method.GetCustomAttributes(true);
        var controllerAttributes = controller?.GetCustomAttributes(true) ?? [];

        if (methodAttributes.OfType<IAllowAnonymous>().Any() ||
            controllerAttributes.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = [];
            return;
        }

        if (!methodAttributes.OfType<IAuthorizeData>().Any() &&
            !controllerAttributes.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });
    }
}
