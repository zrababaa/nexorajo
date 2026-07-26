namespace SMPP.Infrastructure.SmsGateway;

/// <summary>
/// Bound from configuration key "SmsGateway". BaseUrl corresponds to legacy's
/// SMPP_API_URL env var - the only gateway integration point that is actually live
/// in the legacy app.
/// </summary>
public class GatewayOptions
{
    public const string SectionName = "SmsGateway";

    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
