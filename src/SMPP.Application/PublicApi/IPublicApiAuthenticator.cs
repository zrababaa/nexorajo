namespace SMPP.Application.PublicApi;

/// <summary>Validates a token+secret pair (legacy header names token-id/secret-key) and
/// resolves the owning, active account.</summary>
public interface IPublicApiAuthenticator
{
    Task<int?> AuthenticateAsync(string token, string secret, CancellationToken ct = default);
}
