using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Valora.Web.Services.Bff;

public sealed class DistributedBffSessionStore(IDistributedCache cache, BffSessionProtector protector)
    : IDistributedBffSessionStore
{
    private static string Key(string ticket) => $"valora:bff:session:{ticket}";

    public Task SetAsync(string ticket, BffServerSession session, CancellationToken cancellationToken = default) =>
        cache.SetAsync(Key(ticket), protector.Protect(session), new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = session.RefreshTokenExpiresAt,
            SlidingExpiration = TimeSpan.FromMinutes(30)
        }, cancellationToken);

    public async Task<BffServerSession?> GetAsync(string ticket, CancellationToken cancellationToken = default)
    {
        var value = await cache.GetAsync(Key(ticket), cancellationToken);
        return value is null ? null : protector.Unprotect(value);
    }

    public Task RemoveAsync(string ticket, CancellationToken cancellationToken = default) => cache.RemoveAsync(Key(ticket), cancellationToken);
}
