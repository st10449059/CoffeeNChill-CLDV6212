using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CoffeeNChill.Gateway.Swagger;

/// <summary>
/// Documents JWT requirements for protected API operations.
/// </summary>
public sealed class JwtSecurityOperationFilter : IOperationFilter
{
    /// <summary>
    /// Adds Bearer authentication requirements to secured operations.
    /// </summary>
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var controller = method.DeclaringType;

        var anonymous =
            method.IsDefined(typeof(AllowAnonymousAttribute), true) ||
            (controller?.IsDefined(
                typeof(AllowAnonymousAttribute), true) ?? false);

        if (anonymous)
        {
            return;
        }

        var requiresAuthorization =
            method.IsDefined(typeof(AuthorizeAttribute), true) ||
            (controller?.IsDefined(
                typeof(AuthorizeAttribute), true) ?? false);

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "bearer", context.Document)] = []
        });

        operation.Responses ??= new OpenApiResponses();

        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add(
                "401",
                new OpenApiResponse
                {
                    Description = "Authentication required."
                });
        }

        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add(
                "403",
                new OpenApiResponse
                {
                    Description = "Insufficient permissions."
                });
        }
    }
}
