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
