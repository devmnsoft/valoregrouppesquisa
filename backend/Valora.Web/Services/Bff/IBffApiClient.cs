using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Services.Bff;

public interface IBffApiClient
{
    Task<BffAuthenticationResult> PostAuthenticationAsync(string path, object request, CancellationToken cancellationToken);
    Task PostAsync(string path, object? request, string? bearer, CancellationToken cancellationToken);
}
