namespace Valora.Web.Services.Bff;

/// <summary>Represents an infrastructure failure before an HTTP response was received.</summary>
public sealed class BffApiUnavailableException(string baseUrl, Exception innerException)
    : Exception("The configured Valora API is unavailable.", innerException)
{
    public string BaseUrl { get; } = baseUrl.TrimEnd('/');
}
