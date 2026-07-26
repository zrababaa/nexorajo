namespace SMPP.Infrastructure.WhatsAppGateway;

/// <summary>
/// Bound from configuration key "WhatsAppGateway". BaseUrl corresponds to legacy's
/// SMPP_API_URL env var - the only gateway integration point that is actually live
/// in the legacy app (NEW_URL_WA_SERVER/MEDIA_URL served a dead image-send path and
/// are intentionally not carried forward).
/// </summary>
public class GatewayOptions
{
    public const string SectionName = "WhatsAppGateway";

    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
