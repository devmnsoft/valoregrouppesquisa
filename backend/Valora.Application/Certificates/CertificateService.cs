using System.Text;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Certificates;

public sealed class CertificateService(IPublicResultService results) : ICertificateService
{
    public async Task<string> BuildCertificateHtmlAsync(Guid responseId, string resultToken)
    {
        var data = await results.GetAsync(responseId, new PublicResultRequest(resultToken));
        var issuedAt = data.Certificate.IssuedAt ?? DateTime.UtcNow;
        return $"""
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Certificado Valora Insight™</title></head>
<body style="font-family:Arial,sans-serif;margin:0;padding:clamp(16px,5vw,48px);color:#102f36;background:#f3f6f4">
<section style="border:6px solid #b9974b;border-radius:24px;padding:clamp(24px,6vw,64px);text-align:center;max-width:980px;margin:auto;background:#fff;box-shadow:0 24px 60px #102f3620">
<p style="color:#8b6d2c;letter-spacing:.22em;text-transform:uppercase;font-weight:bold">Valora Group</p>
<h1 style="font-size:clamp(36px,7vw,64px);margin:0;color:#102f36">Valora Insight™</h1>
<h2 style="font-weight:normal">Certificado de conclusão</h2>
<p>Certificamos que <strong>{Esc(data.Response.ParticipantName ?? "Participante")}</strong> concluiu a pesquisa <strong>{Esc(data.Survey.Title)}</strong>.</p>
<p>Organização: <strong>{Esc(data.Company.PublicName ?? data.Company.Name ?? "Valora Group")}</strong></p>
<p>Data de emissão: {issuedAt:dd/MM/yyyy HH:mm} UTC</p>
<p>Código do certificado: <strong>{Esc(data.Certificate.CertificateCode)}</strong></p>
<p>Score: <strong>{data.Result.Percentage:N2}%</strong> • Nível: <strong>{Esc(data.Result.MaturityLabel)}</strong></p>
<p>{Esc(data.Result.RadarText)}</p>
<p>Valide a autenticidade em <strong>/certificado/validar/{Uri.EscapeDataString(data.Certificate.CertificateCode)}</strong></p>
</section></body></html>
""";
    }

    public async Task<byte[]> RenderPdfAsync(Guid responseId, string resultToken)
    {
        var data = await results.GetAsync(responseId, new PublicResultRequest(resultToken));
        var text = $"VALORA INSIGHT - CERTIFICADO\\nValora Group\\n\\nParticipante: {data.Response.ParticipantName ?? "Participante"}\\nOrganizacao: {data.Company.PublicName ?? data.Company.Name ?? "Valora Group"}\\nPesquisa: {data.Survey.Title}\\nEmissao: {(data.Certificate.IssuedAt ?? DateTime.UtcNow):dd/MM/yyyy}\\nResultado: {data.Result.Percentage:N2}% - {data.Result.MaturityLabel}\\nCodigo de validacao: {data.Certificate.CertificateCode}\\nValidacao: /certificado/validar/{data.Certificate.CertificateCode}";
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
        var stream = $"0.063 0.184 0.212 rg 0 0 595 842 re f\n0.725 0.592 0.294 RG 5 w 28 28 539 786 re S\n1 1 1 rg BT /F1 18 Tf 72 740 Td ({PdfEsc(text)}) Tj ET";
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
