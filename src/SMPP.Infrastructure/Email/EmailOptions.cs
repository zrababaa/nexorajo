namespace SMPP.Infrastructure.Email;

/// <summary>Bound from the "Email" config section - see appsettings.Production.example.json.
/// SmtpHost left blank (the appsettings.json/Development default) means email sending is
/// disabled: SmtpEmailSender throws and callers that treat it as best-effort log and move on.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 465;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    /// <summary>Where spam-keyword-blocked alerts are sent - see SpamKeywordFilterService.</summary>
    public string SpamAlertRecipient { get; set; } = string.Empty;
}
