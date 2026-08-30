using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LifeTracker.Infrastructure;

// Register the Bearer header scheme so Scalar can properly attach bearer tokens 
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JWT"
        };

        return Task.CompletedTask;
    }
}

// OpenAPI does not see FallbackPolicy so set bearer on locked routes and remove it and mark route as properly unprotected in OpenAPI and Scalar UI on AllowAnonymous routes
internal sealed class BearerOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var anonymous = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();

        if (anonymous)
        {
            operation.Security = [];
            return Task.CompletedTask;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            }
        ];

        return Task.CompletedTask;
    }
}
