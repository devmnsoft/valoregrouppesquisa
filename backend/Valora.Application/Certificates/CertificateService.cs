using System.Text;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Certificates;

public sealed class CertificateService(IPublicResultService results, ILogger<CertificateService> logger) : ICertificateService
{
    public object Plan(Guid responseId, string format) { logger.LogInformation("Certificate plan requested. ResponseId={ResponseId} Format={Format}", responseId, format); return new { product="Valora Pulse™", issuer="Valora Group", title="Certificado de Diagnóstico", responseId, format, status = "implemented" }; }

    public async Task<string> BuildCertificateHtmlAsync(Guid responseId, string resultToken)
    {
        var data = await results.GetAsync(responseId, new PublicResultRequest(resultToken));
        var issuedAt = data.Certificate.IssuedAt ?? DateTime.UtcNow;
        return $"""
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><title>Certificado Valora Group</title></head>
<body style="font-family:Arial,sans-serif;margin:40px;color:#16213e">
<section style="border:6px solid #d4af37;border-radius:24px;padding:40px;text-align:center;max-width:980px;margin:auto">
<h1 style="font-size:42px;margin:0">Valora Group</h1>
<h2>Certificado de participação no diagnóstico Valora Group.</h2>
<p>Certificamos que <strong>{Esc(data.Response.ParticipantName ?? "Participante")}</strong> concluiu a pesquisa <strong>{Esc(data.Survey.Title)}</strong>.</p>
<p>Data de emissão: {issuedAt:dd/MM/yyyy HH:mm} UTC</p>
<p>Código do certificado: <strong>{Esc(data.Certificate.CertificateCode)}</strong></p>
<p>Score: <strong>{data.Result.Percentage:N2}%</strong> • Nível: <strong>{Esc(data.Result.MaturityLabel)}</strong></p>
<p>{Esc(data.Result.RadarText)}</p>
<p>Validação: /public/results/{responseId}?token=seu-token</p>
</section></body></html>
""";
    }

    public async Task<byte[]> RenderPdfAsync(Guid responseId, string resultToken)
    {
        var data = await results.GetAsync(responseId, new PublicResultRequest(resultToken));
        var text = $"Certificado Valora Group\\nParticipante: {data.Response.ParticipantName ?? "Participante"}\\nPesquisa: {data.Survey.Title}\\nCódigo: {data.Certificate.CertificateCode}\\nScore: {data.Result.Percentage:N2}%\\nNível: {data.Result.MaturityLabel}";
        return MinimalPdf(text);
    }

    public async Task<byte[]> RenderImageAsync(Guid responseId, string resultToken)
    {
        _ = await results.GetAsync(responseId, new PublicResultRequest(resultToken));
        return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAyAAAAGQCAIAAADZR5NjAAAAGXRFWHRTb2Z0d2FyZQBWYWxvcmEgR3JvdXAgUE5HFwmzfwAAADNJREFUeJztwTEBAAAAwqD1T20JT6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB4G8kQAAFWkUdVAAAAAElFTkSuQmCC");
    }

    static string Esc(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    static byte[] MinimalPdf(string text)
    {
        static string PdfEsc(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", "").Replace("\n", ") Tj 0 -18 Td (");
        var stream = $"BT /F1 18 Tf 72 760 Td ({PdfEsc(text)}) Tj ET";
        var objects = new[]
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            $"5 0 obj << /Length {Encoding.ASCII.GetByteCount(stream)} >> stream\n{stream}\nendstream endobj\n"
        };
        var sb = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int>{0};
        foreach (var obj in objects){ offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString())); sb.Append(obj); }
        var xref = Encoding.ASCII.GetByteCount(sb.ToString()); sb.Append($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach(var off in offsets.Skip(1)) sb.Append(off.ToString("0000000000")+" 00000 n \n");
        sb.Append($"trailer << /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
