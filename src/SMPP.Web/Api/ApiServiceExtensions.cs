using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SMPP.Web.Auth;

namespace SMPP.Web.Api;

/// <summary>
/// Everything that turns this app into a documented, publicly reachable REST API: bearer-token
/// authentication alongside the existing sign-in cookie, CORS, the shared JSON conventions, and
/// the OpenAPI document behind the Swagger page.
/// </summary>
public static class ApiServiceExtensions
{
    public const string CorsPolicyName = "SmppApi";

    public static IServiceCollection AddSmppApi(this IServiceCollection services, IConfiguration configuration)
    {
        ApiAuth.AssertSchemeNamesMatchFramework();

        services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<ITokenService, JwtTokenService>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.Key.Length < 32)
        {
            // Refused at startup rather than at the first login: a short or missing key would
            // otherwise mean tokens anyone can forge, discovered only in production.
            throw new InvalidOperationException(
                "Jwt:Key must be set to at least 32 characters. Generate one with: openssl rand -base64 48");
        }

        // Identity has already made the sign-in cookie the default scheme, and that stays true so
        // the MVC screens behave exactly as before. This only adds bearer as a second way in,
        // which the API controllers ask for by name.
        services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                // Claims are consumed exactly as issued (see JwtTokenService); the inbound
                // short-name mapping would otherwise rewrite them behind our back.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
        {
            var origins = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>()?.AllowedCorsOrigins
                ?? Array.Empty<string>();

            if (origins.Length == 0 || origins.Contains("*"))
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins(origins).AllowCredentials();
            }

            policy.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition");
        }));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => ConfigureSwagger(options, configuration));

        return services;
    }

    /// <summary>
    /// API conventions that have to be set where the MVC services are built: enums as names on
    /// the wire, and validation failures in the same error shape as every other failure.
    /// </summary>
    public static IMvcBuilder AddSmppApiConventions(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return new BadRequestObjectResult(
                    new ApiErrorResponse("The request is not valid.") { Errors = errors });
            };
        });

        return builder;
    }

    public static WebApplication UseSmppApi(this WebApplication app)
    {
        app.UseCors(CorsPolicyName);

        var options = app.Services.GetRequiredService<IOptions<ApiOptions>>().Value;
        if (options.EnableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(ui =>
            {
                ui.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                ui.DocumentTitle = options.Title;
                ui.DisplayRequestDuration();
                // Collapsed by default: the surface is large enough that a fully expanded page
                // is hard to scan.
                ui.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            });
        }

        return app;
    }

    private static void ConfigureSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options, IConfiguration configuration)
    {
        var api = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new ApiOptions();

        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = api.Title,
            Version = "v1",
            Description = Description,
            Contact = string.IsNullOrWhiteSpace(api.ContactEmail)
                ? null
                : new OpenApiContact { Email = api.ContactEmail },
        });

        if (!string.IsNullOrWhiteSpace(api.PublicBaseUrl))
        {
            options.AddServer(new OpenApiServer { Url = api.PublicBaseUrl.TrimEnd('/'), Description = "Public" });
        }

        options.AddSecurityDefinition(ApiSecurityOperationFilter.BearerScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the accessToken returned by /api/v1/auth/login or /api/v1/auth/token.",
        });

        options.AddSecurityDefinition(ApiSecurityOperationFilter.TokenIdScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiSecurityOperationFilter.TokenIdScheme,
            Description = "Legacy send endpoint only: the account's API token.",
        });

        options.AddSecurityDefinition(ApiSecurityOperationFilter.SecretKeyScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiSecurityOperationFilter.SecretKeyScheme,
            Description = "Legacy send endpoint only: the account's API secret.",
        });

        options.OperationFilter<ApiSecurityOperationFilter>();

        // Only the REST API is documented - the MVC screens are HTML, and an OpenAPI entry for
        // each of them would bury the endpoints an integrator actually wants.
        options.DocInclusionPredicate((_, description) =>
            description.RelativePath?.StartsWith("api", StringComparison.OrdinalIgnoreCase) == true);

        options.TagActionsBy(description =>
        {
            var tags = description.ActionDescriptor.EndpointMetadata.OfType<ITagsMetadata>().LastOrDefault();
            return tags is not null
                ? tags.Tags.ToList()
                : new List<string> { description.ActionDescriptor.RouteValues["controller"] ?? "API" };
        });

        options.CustomSchemaIds(SchemaId);

        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }

    /// <summary>
    /// Short, readable schema names - "PagedResultOfAccountListItemDto" rather than the full
    /// CLR name. Generic arguments are spelled out because the paged results would otherwise
    /// all collide on one name.
    /// </summary>
    private static string SchemaId(Type type)
    {
        var name = type.Name;
        var nested = type.DeclaringType is null ? name : $"{type.DeclaringType.Name}{name}";

        if (!type.IsGenericType)
        {
            return nested;
        }

        var tick = nested.IndexOf('`');
        var stem = tick < 0 ? nested : nested[..tick];
        return $"{stem}Of{string.Join("And", type.GetGenericArguments().Select(SchemaId))}";
    }

    private const string Description = """
        REST API for the SMS platform. Everything the web portal can do is available here:
        sending, recipient lists, history, reporting, credit top-ups, account administration,
        and the content filter.

        **Authenticating**

        1. `POST /api/v1/auth/login` with a username/email and password, or
           `POST /api/v1/auth/token` with an account's API token/secret pair for a
           server-to-server integration.
        2. Send the returned token on every other call: `Authorization: Bearer <accessToken>`.

        **Sending**

        `POST /api/v1/messages/quick-send` (numbers inline) or `/bulk-send` (a saved list)
        answers `202` with a `batchId` once the batch is accepted and charged. Delivery happens
        asynchronously - poll `GET /api/v1/history?campaignBatchId=<batchId>` for each
        recipient's outcome.

        **Errors**

        Every failure returns `{ "message": "...", "errors": { ... } }`. A `422` from a send
        endpoint means the content filter blocked the message and lists the offending terms in
        `blockedTerms`; nothing was charged or queued.

        The endpoints under **Legacy API** keep the older contract (token-id/secret-key headers,
        `status`/`message` response body) for integrations already built against it.
        """;
}
