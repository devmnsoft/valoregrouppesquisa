using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Services.Bff;

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

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? request, string bearer, string correlationId, CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(method, path)
        {
            Content = request is null ? null : JsonContent.Create(request, options: JsonOptions)
        };
        message.Headers.Authorization = new("Bearer", bearer);
        message.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        return await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(string.IsNullOrWhiteSpace(detail) ? "Não foi possível concluir a solicitação." : detail,
            null, response.StatusCode == HttpStatusCode.Unauthorized ? HttpStatusCode.Unauthorized : response.StatusCode);
    }
}
