namespace SMPP.Web.Api;

/// <summary>
/// How the REST API presents itself once the product is online, bound from the "Api" section.
/// </summary>
public class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>Title shown at the top of the Swagger page.</summary>
    public string Title { get; set; } = "Nexora SMS Platform API";

    /// <summary>
    /// The public URL the API is reached at (e.g. https://smpp.example.com). Listed as the
    /// server in the OpenAPI document so "Try it out" targets the real host rather than the
    /// internal one Kestrel sees behind the reverse proxy. Optional - relative paths are used
    /// when it is not set.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Contact email published in the OpenAPI document.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Serves the Swagger page and the OpenAPI document. On by default.</summary>
    public bool EnableSwagger { get; set; } = true;

    /// <summary>
    /// Browser origins allowed to call the API cross-site. Empty (or a single "*") allows any
    /// origin, which is safe here because the API authenticates with bearer tokens rather than
    /// cookies - no credentials ride along on a cross-site request.
    /// </summary>
    public string[] AllowedCorsOrigins { get; set; } = Array.Empty<string>();
}
