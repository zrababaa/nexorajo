namespace SMPP.Application.Abstractions;

/// <summary>Sends a single outbound email. Throws on delivery failure - callers that treat email as
/// best-effort (e.g. alerting) are responsible for catching and logging.</summary>
public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string body, CancellationToken ct = default);
}
