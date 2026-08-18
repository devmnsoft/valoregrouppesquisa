using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Services.Bff;

public sealed class BffApiClient(HttpClient httpClient, IOptions<ApiOptions> options) : IBffApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("/health", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }
        catch (Exception exception) when (IsConnectivityFailure(exception, cancellationToken))
        {
            throw new BffApiUnavailableException(options.Value.BaseUrl, exception);
        }
    }

    public async Task<JsonElement> GetHealthAsync(string path, string correlationId, CancellationToken cancellationToken)
    {
        if (!path.StartsWith("/health", StringComparison.Ordinal))
            throw new ArgumentException("Somente endpoints de saude podem ser consultados sem uma sessao.", nameof(path));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return document.RootElement.Clone();
        }
        catch (Exception exception) when (IsConnectivityFailure(exception, cancellationToken))
        {
            throw new BffApiUnavailableException(options.Value.BaseUrl, exception);
        }
    }

    public async Task<BffAuthenticationResult> PostAuthenticationAsync(string path, object request, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            message.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
            using var response = await httpClient.SendAsync(message, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<BffAuthenticationResult>(JsonOptions, cancellationToken)
                ?? throw new HttpRequestException("A API retornou uma resposta de autenticação vazia.");
        }
        catch (Exception exception) when (IsConnectivityFailure(exception, cancellationToken))
        {
            throw new BffApiUnavailableException(options.Value.BaseUrl, exception);
        }
    }

    private static bool IsConnectivityFailure(Exception exception, CancellationToken requestCancellation) =>
        exception is HttpRequestException { StatusCode: null }
        || exception is TimeoutException
        || exception is TaskCanceledException && !requestCancellation.IsCancellationRequested;

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
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var headerCorrelationId = response.Headers.TryGetValues("X-Correlation-Id", out var correlationValues)
            ? correlationValues.FirstOrDefault()
            : null;
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var code = ReadString(root, "code") ?? ReadString(root, "error");
            var message = ReadString(root, "detail") ?? ReadString(root, "message");
            var correlationId = ReadString(root, "correlationId") ?? headerCorrelationId;
            throw new BffApiException(
                response.StatusCode,
                code ?? "API_ERROR",
                message ?? "Não foi possível concluir a solicitação.",
                correlationId);
        }
        catch (JsonException)
        {
            throw new BffApiException(response.StatusCode, "API_ERROR", "Não foi possível concluir a solicitação.", headerCorrelationId);
        }
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(propertyName, out var value)) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
