using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Valora.Application.FormalDeliverables;

/// <summary>Dependency-free renderers. PDF and XLSX bytes are complete documents, never placeholder payloads.</summary>
public sealed class ExecutiveReportExportService : IExecutiveReportExportService
{
    public GeneratedDocument Render(DiagnosisDocumentSnapshot snapshot, DeliverableFormat format, DateTimeOffset generatedAt)
    {
        var id = Guid.NewGuid();
        var trace = $"VLR-{generatedAt:yyyyMMdd}-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{snapshot.OrganizationId}:{snapshot.DiagnosisId}:{id}")))[..12]}";
        var slug = Slug(snapshot.OrganizationName);
        var (extension, mime, bytes) = format switch
        {
            DeliverableFormat.Pdf or DeliverableFormat.CertificatePdf => ("pdf", "application/pdf", RenderPdf(snapshot, trace, generatedAt, format == DeliverableFormat.CertificatePdf)),
            DeliverableFormat.Xlsx => ("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", RenderXlsx(snapshot, trace, generatedAt)),
            DeliverableFormat.Json => ("json", "application/json", RenderJson(snapshot, trace, generatedAt)),
            DeliverableFormat.Docx => throw new NotSupportedException("A exportação DOCX ainda não está habilitada neste ambiente. Use PDF ou XLSX."),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        return new GeneratedDocument(id, snapshot.OrganizationId, snapshot.DiagnosisId, format,
            $"valora-{(format == DeliverableFormat.CertificatePdf ? "certificado" : "relatorio")}-{slug}-{generatedAt:yyyyMMdd}.{extension}", mime, bytes, trace, generatedAt);
    }

    private static byte[] RenderJson(DiagnosisDocumentSnapshot s, string trace, DateTimeOffset at) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        organization = new { id = s.OrganizationId, name = s.OrganizationName },
        diagnosis = new { id = s.DiagnosisId, name = s.DiagnosisName, completedAt = s.CompletedAt },
        methodologyVersion = s.MethodologyVersion,
        scores = new { overall = s.OverallScore, maturityLevel = s.MaturityLevel },
        dimensions = s.Dimensions, concepts = Array.Empty<object>(), evidenceItems = s.EvidenceItems,
        risks = s.Risks, opportunities = s.Opportunities, recommendations = s.Recommendations,
        limitations = s.Limitations,
        actionPlan = s.ActionPlan, generatedAt = at, traceCode = trace
    }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static byte[] RenderPdf(DiagnosisDocumentSnapshot s, string trace, DateTimeOffset generatedAt, bool certificate)
    {
        var lines = new List<string>();
        if (certificate)
        {
            lines.AddRange(["VALORA INSIGHT - CERTIFICADO", s.OrganizationName, s.DiagnosisName, $"Score geral: {s.OverallScore:0.0}", $"Maturidade: {s.MaturityLevel}", $"Metodologia: {s.MethodologyName} {s.MethodologyVersion}", $"Emitido em: {generatedAt:dd/MM/yyyy}", $"Validacao publica: /p/certificates/{trace}"]);
        }
        else
        {
            lines.AddRange(["VALORA EXECUTIVE REPORT", s.OrganizationName, s.DiagnosisName, $"Score geral: {s.OverallScore:0.0} | {s.MaturityLevel}", "RESUMO EXECUTIVO", s.ExecutiveSummary, "LEITURA ESTRATEGICA", s.StrategicReading, "DIMENSOES"]);
            lines.AddRange(s.Dimensions.Select(x => $"{x.Name}: {x.Score:0.0} - {x.Interpretation}"));
            lines.Add("EVIDENCIAS"); lines.AddRange(s.EvidenceItems.Count > 0 ? s.EvidenceItems.Select(x => $"{x.Dimension}: {x.Description} ({x.Source})") : ["Nenhuma evidencia consolidada foi disponibilizada para esta emissao."]);
            lines.Add("RISCOS"); lines.AddRange(s.Risks); lines.Add("OPORTUNIDADES"); lines.AddRange(s.Opportunities);
            lines.Add("PONTOS FORTES"); lines.AddRange(s.Strengths); lines.Add("FRAGILIDADES"); lines.AddRange(s.Weaknesses);
            lines.Add("PRIORIDADES E PLANO RECOMENDADO"); lines.AddRange(s.ActionPlan.Select(x => $"{x.Priority}: {x.Action} ({x.Owner})"));
            lines.Add("DECISOES E EVOLUCAO"); lines.Add("Nao ha historico formal suficiente para apresentar decisoes ou evolucao nesta emissao.");
            lines.Add("LIMITACOES DA ANALISE"); lines.AddRange(s.Limitations);
            lines.Add($"Data de emissao: {generatedAt:dd/MM/yyyy HH:mm} UTC");
            lines.Add($"Metodologia: {s.MethodologyName} | versao {s.MethodologyVersion}"); lines.Add($"Rastreabilidade: {trace}");
        }
        var content = new StringBuilder("BT /F1 11 Tf 54 780 Td 0 -18 Td ");
        foreach (var line in lines.SelectMany(Wrap)) content.Append('(').Append(PdfEscape(line)).Append(") Tj 0 -16 Td ");
        content.Append("ET");
        var stream = Encoding.ASCII.GetBytes(content.ToString());
        var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>", $"<< /Length {stream.Length} >>\nstream\n{Encoding.ASCII.GetString(stream)}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        using var output = new MemoryStream(); using var writer = new StreamWriter(output, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine("%PDF-1.4"); writer.Flush(); var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++) { offsets.Add(output.Position); writer.WriteLine($"{i + 1} 0 obj"); writer.WriteLine(objects[i]); writer.WriteLine("endobj"); writer.Flush(); }
        var xref = output.Position; writer.WriteLine($"xref\n0 {objects.Length + 1}\n0000000000 65535 f "); foreach (var o in offsets.Skip(1)) writer.WriteLine($"{o:0000000000} 00000 n ");
        writer.WriteLine($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); writer.Flush(); return output.ToArray();
    }

    private static byte[] RenderXlsx(DiagnosisDocumentSnapshot s, string trace, DateTimeOffset at)
    {
        var sheets = new Dictionary<string, IEnumerable<string[]>>
        {
            ["Resumo"] = Rows(new[] { "Organização", s.OrganizationName }, new[] { "Diagnóstico", s.DiagnosisName }, new[] { "Score", s.OverallScore.ToString("0.0") }, new[] { "Maturidade", s.MaturityLevel }),
            ["Scores"] = Rows(new[] { "Indicador", "Valor" }, new[] { "Score geral", s.OverallScore.ToString("0.0") }),
            ["Dimensões"] = new[] { new[] { "Dimensão", "Score", "Interpretação" } }.Concat(s.Dimensions.Select(x => new[] { x.Name, x.Score.ToString("0.0"), x.Interpretation })),
            ["Conceitos"] = Rows(new[] { "Metodologia", "Versão" }, new[] { s.MethodologyName, s.MethodologyVersion }),
            ["Evidências"] = new[] { new[] { "Dimensão", "Evidência", "Fonte" } }.Concat(s.EvidenceItems.Select(x => new[] { x.Dimension, x.Description, x.Source })),
            ["Respostas"] = Rows(new[] { "Privacidade", s.IsAnonymous ? "Diagnóstico anônimo: dados pessoais omitidos" : "Dados consolidados; respostas individuais não incluídas" }),
            ["Recomendações"] = new[] { new[] { "Recomendação" } }.Concat(s.Recommendations.Select(x => new[] { x })),
            ["Limitações"] = new[] { new[] { "Limitação metodológica" } }.Concat(s.Limitations.Select(x => new[] { x })),
            ["Plano de ação"] = new[] { new[] { "Prioridade", "Ação", "Responsável", "Prazo" } }.Concat(s.ActionPlan.Select(x => new[] { x.Priority, x.Action, x.Owner, x.DueDate?.ToString("yyyy-MM-dd") ?? "" })),
            ["Auditoria"] = Rows(new[] { "Gerado em", "Rastreabilidade" }, new[] { at.ToString("O"), trace })
        };
        using var output = new MemoryStream(); using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Add(zip, "[Content_Types].xml", ContentTypes(sheets.Count)); Add(zip, "_rels/.rels", "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add(zip, "xl/workbook.xml", Workbook(sheets.Keys)); Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Count));
            var i = 1; foreach (var sheet in sheets) Add(zip, $"xl/worksheets/sheet{i++}.xml", Worksheet(sheet.Value));
        } return output.ToArray();
    }

    private static IEnumerable<string[]> Rows(params string[][] rows) => rows;
    private static void Add(ZipArchive z, string name, string value) { var e = z.CreateEntry(name, CompressionLevel.Optimal); using var w = new StreamWriter(e.Open(), new UTF8Encoding(false)); w.Write(value); }
    private static string Worksheet(IEnumerable<string[]> rows) { var b = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>"); var r = 1; foreach (var row in rows) { b.Append($"<row r=\"{r++}\">"); foreach (var cell in row) b.Append("<c t=\"inlineStr\"><is><t>").Append(Xml(cell)).Append("</t></is></c>"); b.Append("</row>"); } return b.Append("</sheetData></worksheet>").ToString(); }
    private static string Workbook(IEnumerable<string> names) => "<?xml version=\"1.0\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>" + string.Concat(names.Select((n, i) => $"<sheet name=\"{Xml(n)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>")) + "</sheets></workbook>";
    private static string WorkbookRels(int count) => "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" + string.Concat(Enumerable.Range(1, count).Select(i => $"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>")) + "</Relationships>";
    private static string ContentTypes(int count) => "<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" + string.Concat(Enumerable.Range(1, count).Select(i => $"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>")) + "</Types>";
    private static string Xml(string value) { var b = new StringBuilder(); using var w = XmlWriter.Create(b, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment, OmitXmlDeclaration = true }); w.WriteString(value); w.Flush(); return b.ToString(); }
    private static IEnumerable<string> Wrap(string text) { if (string.IsNullOrWhiteSpace(text)) return [""]; return Enumerable.Range(0, (text.Length + 74) / 75).Select(i => text.Substring(i * 75, Math.Min(75, text.Length - i * 75))); }
    private static string PdfEscape(string value) => Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value)).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Slug(string value) { var chars = value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray(); return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries)); }
}
