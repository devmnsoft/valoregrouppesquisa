using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Services.Bff;

public sealed record BffServerSession(string AccessToken, DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, BffSafeSession SafeSession)
{
    public const int CurrentSessionVersion = 2;
    public int SessionVersion { get; init; } = CurrentSessionVersion;
}
