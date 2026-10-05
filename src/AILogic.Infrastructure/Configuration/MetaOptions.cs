namespace AILogic.Infrastructure.Configuration;

public sealed class MetaOptions
{
    public string GraphApiVersion { get; init; } = "v22.0";
    public string WebhookVerifyToken { get; init; } = string.Empty;
    public string MessengerPageAccessToken { get; init; } = string.Empty;
    public string WhatsAppAccessToken { get; init; } = string.Empty;
    public string WhatsAppPhoneNumberId { get; init; } = string.Empty;
}
