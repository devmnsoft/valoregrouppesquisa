using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Valora.Web.Services.Bff;

public interface IDistributedBffSessionStore
{
    Task SetAsync(string ticket, BffServerSession session, CancellationToken cancellationToken = default);
    Task<BffServerSession?> GetAsync(string ticket, CancellationToken cancellationToken = default);
    Task RemoveAsync(string ticket, CancellationToken cancellationToken = default);
}

public sealed class BffSessionProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("Valora.Web.Bff.Session.v1");
    public byte[] Protect(BffServerSession session) => System.Text.Encoding.UTF8.GetBytes(
        protector.Protect(JsonSerializer.Serialize(session)));
    public BffServerSession? Unprotect(byte[] payload) => JsonSerializer.Deserialize<BffServerSession>(
        protector.Unprotect(System.Text.Encoding.UTF8.GetString(payload)));
}

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

public sealed class BffSessionCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
    }
}
