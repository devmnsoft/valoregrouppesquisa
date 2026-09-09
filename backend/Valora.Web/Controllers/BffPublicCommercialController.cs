using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[ApiController, AllowAnonymous, Route("bff/public")]
public sealed class BffPublicCommercialController(
    IBffApiClient api,
    PublicAccessSessionStore publicSessions) : ControllerBase {

    [HttpPost("diagnostic/start")]
    public Task Start(CancellationToken ct) => Proxy(HttpMethod.Post, "/api/v1/public/diagnostic/start", null, ct);

    [HttpPost("contact-requests")]
    public Task Contact(CancellationToken ct) => Proxy(HttpMethod.Post, "/api/v1/public/contact-requests", null, ct);

    [HttpPost("plan-interest")]
    public Task Plan(CancellationToken ct) => Proxy(HttpMethod.Post, "/api/v1/public/plan-interest", null, ct);

    [HttpPost("surveys/{surveyId:guid}/session")]
    public async Task<IActionResult> ExchangeSurvey(Guid surveyId, PublicTokenExchangeRequest request, CancellationToken ct) {
        if (!ValidToken(request.Token)) return BadRequest(Error("PUBLIC_TOKEN_REQUIRED", "O link público está incompleto."));
        using var response = await api.SendAsync(HttpMethod.Post, $"/public/surveys/{surveyId}/validate",
            new { token = request.Token }, string.Empty, HttpContext.TraceIdentifier, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) return Payload(response, payload);
        await publicSessions.WriteAsync(HttpContext, "survey", surveyId, request.Token, ct);
        return Payload(response, payload);
    }

    [HttpGet("surveys/{surveyId:guid}")]
    public async Task<IActionResult> Survey(Guid surveyId, CancellationToken ct) {
        var token = await publicSessions.ReadAsync(HttpContext, "survey", surveyId, ct);
        if (token is null) return Unauthorized(Error("PUBLIC_SESSION_EXPIRED", "O acesso expirou. Abra novamente o link da pesquisa."));
        return await Proxy(HttpMethod.Post, $"/public/surveys/{surveyId}/validate", new { token }, ct);
    }

    [HttpPost("results/{responseId:guid}/session")]
    public async Task<IActionResult> ExchangeResult(Guid responseId, PublicTokenExchangeRequest request, CancellationToken ct) {
        if (!ValidToken(request.Token)) return BadRequest(Error("PUBLIC_TOKEN_REQUIRED", "O código de acesso ao resultado é obrigatório."));
        using var response = await api.SendAsync(HttpMethod.Get,
            $"/public/results/{responseId}?token={Uri.EscapeDataString(request.Token)}", null, string.Empty,
            HttpContext.TraceIdentifier, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) return Payload(response, payload);
        await publicSessions.WriteAsync(HttpContext, "result", responseId, request.Token, ct);
        return Payload(response, payload);
    }

    [HttpPost("surveys/{surveyId:guid}/responses")]
    public async Task<IActionResult> Submit(Guid surveyId, CancellationToken ct) {
        var token = await publicSessions.ReadAsync(HttpContext, "survey", surveyId, ct);
        if (token is null) return Unauthorized(Error("PUBLIC_SESSION_EXPIRED", "O acesso expirou. Abra novamente o link da pesquisa."));
        var body = Request.ContentLength > 0 ? await JsonNode.ParseAsync(Request.Body, cancellationToken: ct) as JsonObject : new JsonObject();
        if (body is null) return BadRequest(Error("PUBLIC_REQUEST_INVALID", "Não foi possível interpretar as respostas."));
        body["token"] = token;
        using var response = await api.SendAsync(HttpMethod.Post, $"/public/surveys/{surveyId}/responses", body,
            string.Empty, HttpContext.TraceIdentifier, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) return Payload(response, payload);
        var result = JsonNode.Parse(payload) as JsonObject;
        if (result?["responseId"]?.GetValue<Guid>() is { } responseId &&
            result["resultToken"]?.GetValue<string>() is { } resultToken && ValidToken(resultToken)) {
            await publicSessions.WriteAsync(HttpContext, "result", responseId, resultToken, ct);
            result.Remove("resultToken");
            result["resultUrl"] = $"/public/results/{responseId}";
            payload = result.ToJsonString(JsonOptions);
        }
        return Payload(response, payload);
    }

    [HttpGet("results/{responseId:guid}")]
    public async Task<IActionResult> Result(Guid responseId, CancellationToken ct) {
        var token = await publicSessions.ReadAsync(HttpContext, "result", responseId, ct);
        if (token is null) return Unauthorized(Error("PUBLIC_SESSION_EXPIRED", "O acesso ao resultado expirou."));
        return await Proxy(HttpMethod.Get, $"/public/results/{responseId}?token={Uri.EscapeDataString(token)}", null, ct);
    }

    [AcceptVerbs("GET", "POST")]
    [Route("surveys/{**resource}")]
    public Task Surveys(string? resource, CancellationToken ct) =>
        Proxy(new HttpMethod(Request.Method), $"/public/surveys/{resource}{Request.QueryString}", null, ct);

    [AcceptVerbs("GET", "POST")]
    [Route("results/{**resource}")]
    public Task Results(string? resource, CancellationToken ct) =>
        ProxyResultResource(resource, ct);

    [HttpGet("plans")]
    public Task Plans(CancellationToken ct) => Proxy(HttpMethod.Get, "/plans/public", null, ct);

    [HttpGet("certificates/validate/{code}")]
    public Task Certificate(string code, CancellationToken ct) =>
        Proxy(HttpMethod.Get, $"/certificates/validate/{Uri.EscapeDataString(code)}", null, ct);

    private async Task<IActionResult> Proxy(HttpMethod method, string path, object? body, CancellationToken ct) {
        if (body is null && Request.ContentLength > 0)
            body = await JsonSerializer.DeserializeAsync<object>(Request.Body, cancellationToken: ct);
        using var response = await api.SendAsync(method, path, body, string.Empty, HttpContext.TraceIdentifier, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        return Payload(response, payload);
    }

    private async Task<IActionResult> ProxyResultResource(string? resource, CancellationToken ct) {
        var first = resource?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!Guid.TryParse(first, out var responseId)) return BadRequest(Error("PUBLIC_RESULT_INVALID", "Resultado inválido."));
        var token = await publicSessions.ReadAsync(HttpContext, "result", responseId, ct);
        if (token is null) return Unauthorized(Error("PUBLIC_SESSION_EXPIRED", "O acesso ao resultado expirou."));
        var separator = Request.QueryString.HasValue ? "&" : "?";
        return await Proxy(new HttpMethod(Request.Method), $"/public/results/{resource}{Request.QueryString}{separator}token={Uri.EscapeDataString(token)}", null, ct);
    }

    private ContentResult Payload(HttpResponseMessage response, string payload) => new() {
        StatusCode = (int)response.StatusCode,
        ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
        Content = payload
    };

    private object Error(string code, string message) => new {
        code,
        message,
        correlationId = HttpContext.TraceIdentifier
    };

    private static bool ValidToken(string? token) => !string.IsNullOrWhiteSpace(token) && token.Length is >= 32 and <= 512;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public sealed record PublicTokenExchangeRequest(string Token);
}
