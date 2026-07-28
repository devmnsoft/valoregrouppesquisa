using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Services.Bff;

public sealed record BffUser(Guid Id, string Name, string Email, string Role);
public sealed record BffOrganization(Guid Id, string Name, string? TradeName, string Slug);
public sealed record BffPlan(string Id, string Name);
public sealed record BffAuthenticationResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid SessionId,
    BffUser User,
    BffOrganization? Organization,
    BffPlan? Plan);

public sealed record BffSafeSession(BffUser User, BffOrganization? Organization, BffPlan? Plan);
public sealed record BffServerSession(string AccessToken, DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, BffSafeSession SafeSession);

public interface IBffApiClient
{
    Task<BffAuthenticationResult> PostAuthenticationAsync(string path, object request, CancellationToken cancellationToken);
    Task PostAsync(string path, object? request, string? bearer, CancellationToken cancellationToken);
}

public sealed class BffApiClient(HttpClient httpClient, IOptions<ApiOptions> options) : IBffApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BffAuthenticationResult> PostAuthenticationAsync(string path, object request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(path, request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<BffAuthenticationResult>(JsonOptions, cancellationToken)
            ?? throw new HttpRequestException("A API retornou uma resposta de autenticação vazia.");
    }

    public async Task PostAsync(string path, object? request, string? bearer, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = request is null ? null : JsonContent.Create(request, options: JsonOptions)
        };
        if (!string.IsNullOrWhiteSpace(bearer)) message.Headers.Authorization = new("Bearer", bearer);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(string.IsNullOrWhiteSpace(detail) ? "Não foi possível concluir a solicitação." : detail,
            null, response.StatusCode == HttpStatusCode.Unauthorized ? HttpStatusCode.Unauthorized : response.StatusCode);
    }
}
