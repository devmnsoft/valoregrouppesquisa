namespace Valora.Application.Contracts;

public interface ICertificateService
{
    Task<string> BuildCertificateHtmlAsync(Guid responseId, string resultToken);
    Task<byte[]> RenderPdfAsync(Guid responseId, string resultToken);
    Task<byte[]> RenderImageAsync(Guid responseId, string resultToken);
}
