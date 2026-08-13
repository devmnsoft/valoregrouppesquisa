using System.Net;

namespace Valora.Web.Services.Bff;

public sealed class BffApiException(
    HttpStatusCode statusCode,
    string code,
    string message,
    string? correlationId)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public string? CorrelationId { get; } = correlationId;
}
