using Microsoft.EntityFrameworkCore;
using SMPP.Application.PublicApi;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class PublicApiAuthenticator : IPublicApiAuthenticator
{
    private readonly SmppDbContext _db;

    public PublicApiAuthenticator(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<int?> AuthenticateAsync(string token, string secret, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.ApiToken == token && u.ApiSecret == secret && u.IsActive, ct);

        return user?.Id;
    }
}
