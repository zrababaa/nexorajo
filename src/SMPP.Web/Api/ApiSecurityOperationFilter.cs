using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SMPP.Web.Api;

/// <summary>
/// Marks an endpoint that authenticates with the legacy token-id/secret-key header pair instead
/// of a bearer token, so Swagger documents it with the right credentials.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LegacyApiKeyAuthAttribute : Attribute
{
}

/// <summary>
/// Gets each operation's padlock right: bearer for the versioned API (the document-wide
/// default), the token-id/secret-key pair for the legacy send endpoint, and nothing at all for
/// endpoints that are genuinely open - sign-in, the health probe, and the daemon's delivery
/// callback.
/// </summary>
public class ApiSecurityOperationFilter : IOperationFilter
{
    public const string BearerScheme = "Bearer";
    public const string TokenIdScheme = "token-id";
    public const string SecretKeyScheme = "secret-key";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<LegacyApiKeyAuthAttribute>().Any())
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    [Reference(TokenIdScheme)] = Array.Empty<string>(),
                    [Reference(SecretKeyScheme)] = Array.Empty<string>(),
                },
            };
            return;
        }

        // Mirrors what AuthorizationMiddleware actually does: an [AllowAnonymous] anywhere on
        // the endpoint opens it, wherever it sits relative to an [Authorize].
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any()
            && !metadata.OfType<IAllowAnonymous>().Any();

        operation.Security = requiresAuth
            ? new List<OpenApiSecurityRequirement>
            {
                new() { [Reference(BearerScheme)] = Array.Empty<string>() },
            }
            : new List<OpenApiSecurityRequirement>();
    }

    private static OpenApiSecurityScheme Reference(string id) => new()
    {
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = id },
    };
}
