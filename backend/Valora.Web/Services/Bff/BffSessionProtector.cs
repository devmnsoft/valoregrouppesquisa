using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Valora.Web.Services.Bff;

public sealed class BffSessionProtector(IDataProtectionProvider provider, ILogger<BffSessionProtector> logger)
{
    private readonly IDataProtector protector = provider.CreateProtector("Valora.Web.Bff.Session.v1");
    public byte[] Protect(BffServerSession session) => System.Text.Encoding.UTF8.GetBytes(
        protector.Protect(JsonSerializer.Serialize(session)));
    public BffServerSession? Unprotect(byte[] payload)
    {
        try
        {
            return JsonSerializer.Deserialize<BffServerSession>(
                protector.Unprotect(System.Text.Encoding.UTF8.GetString(payload)));
        }
        catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException or JsonException)
        {
            logger.LogWarning(exception, "BFF session payload could not be unprotected and will be treated as expired.");
            return null;
        }
    }
}
