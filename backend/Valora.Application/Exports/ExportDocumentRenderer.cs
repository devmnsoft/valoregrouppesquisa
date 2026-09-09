using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using Valora.Application.Contracts;

namespace Valora.Application.Exports;

public sealed class ExportDocumentRenderer {
    public GeneratedExport Render(ExportWorkItem job, ExportDataSet data, DateTimeOffset generatedAt) {
        var safeEntity = string.Concat(job.Entity.Where(character => char.IsLetterOrDigit(character) || character == '-'));
        var (mimeType, content) = job.Format switch {
            "csv" => ("text/csv; charset=utf-8", RenderCsv(data)),
            "json" => ("application/json", RenderJson(job, data, generatedAt)),
            "xlsx" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", RenderXlsx(data)),
            "pdf" => ("application/pdf", RenderPdf(job, data, generatedAt)),
            _ => throw new InvalidOperationException("Formato de exportação não suportado.")
        };
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        return new(job.Id, job.OrganizationId, $"valora-{safeEntity}-{generatedAt:yyyyMMdd-HHmmss}.{job.Format}",
            mimeType, content, checksum, generatedAt.AddHours(24));
    }

    private static byte[] RenderCsv(ExportDataSet data) {
        var output = new StringBuilder();
        output.AppendLine(string.Join(',', data.Columns.Select(Csv)));
        foreach (var row in data.Rows)
            output.AppendLine(string.Join(',', data.Columns.Select(column => Csv(Value(row.GetValueOrDefault(column))))));
        return new UTF8Encoding(true).GetBytes(output.ToString());
    }

    private static string Csv(string value) {
        var protectedValue = value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' ? $"'{value}" : value;
        return $"\"{protectedValue.Replace("\"", "\"\"")}\"";
    }

    private static byte[] RenderJson(ExportWorkItem job, ExportDataSet data, DateTimeOffset generatedAt) =>
        JsonSerializer.SerializeToUtf8Bytes(new {
            entity = job.Entity,
            filters = JsonDocument.Parse(job.FilterJson).RootElement,
            generatedAt,
            evidenceCount = data.Rows.Count,
            rows = data.Rows
        }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static byte[] RenderXlsx(ExportDataSet data) {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true)) {
            Add(zip, "[Content_Types].xml", "<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Add(zip, "_rels/.rels", "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add(zip, "xl/workbook.xml", "<?xml version=\"1.0\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Dados\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add(zip, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var rows = new[] { data.Columns.ToArray() }.Concat(data.Rows.Select(row => data.Columns.Select(column => Value(row.GetValueOrDefault(column))).ToArray()));
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            var index = 1;
            foreach (var row in rows) {
                sheet.Append($"<row r=\"{index++}\">");
                foreach (var cell in row) sheet.Append("<c t=\"inlineStr\"><is><t>").Append(Xml(cell)).Append("</t></is></c>");
                sheet.Append("</row>");
            }
            Add(zip, "xl/worksheets/sheet1.xml", sheet.Append("</sheetData></worksheet>").ToString());
        }
        return output.ToArray();
    }

    private static byte[] RenderPdf(ExportWorkItem job, ExportDataSet data, DateTimeOffset generatedAt) {
        var lines = new List<string> { "VALORA INSIGHT - EXPORTACAO", $"Entidade: {job.Entity}", $"Registros: {data.Rows.Count}", $"Gerado em: {generatedAt:O}" };
        lines.Add(string.Join(" | ", data.Columns));
        lines.AddRange(data.Rows.Take(200).Select(row => string.Join(" | ", data.Columns.Select(column => Value(row.GetValueOrDefault(column))))));
        var body = new StringBuilder("BT /F1 8 Tf 36 806 Td ");
        foreach (var line in lines.SelectMany(Wrap)) body.Append('(').Append(Pdf(line)).Append(") Tj 0 -11 Td ");
        body.Append("ET");
        var stream = Encoding.ASCII.GetBytes(body.ToString());
        string[] objects = ["<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>", $"<< /Length {stream.Length} >>\nstream\n{Encoding.ASCII.GetString(stream)}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"];
        using var output = new MemoryStream();
        using var writer = new StreamWriter(output, Encoding.ASCII, 1024, true) { NewLine = "\n" };
        writer.WriteLine("%PDF-1.4"); writer.Flush(); var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++) { offsets.Add(output.Position); writer.WriteLine($"{i + 1} 0 obj\n{objects[i]}\nendobj"); writer.Flush(); }
        var xref = output.Position; writer.WriteLine($"xref\n0 {objects.Length + 1}\n0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1)) writer.WriteLine($"{offset:0000000000} 00000 n ");
        writer.WriteLine($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); writer.Flush();
        return output.ToArray();
    }

    private static string Value(object? value) => value switch { null => string.Empty, DateTime date => date.ToUniversalTime().ToString("O"), DateTimeOffset date => date.ToUniversalTime().ToString("O"), _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty };
    private static void Add(ZipArchive zip, string path, string value) { var entry = zip.CreateEntry(path, CompressionLevel.Optimal); using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)); writer.Write(value); }
    private static string Xml(string value) { var output = new StringBuilder(); using var writer = XmlWriter.Create(output, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment, OmitXmlDeclaration = true }); writer.WriteString(value); writer.Flush(); return output.ToString(); }
    private static string Pdf(string value) => Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value)).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static IEnumerable<string> Wrap(string value) => Enumerable.Range(0, Math.Max(1, (value.Length + 99) / 100)).Select(index => value.Substring(index * 100, Math.Min(100, value.Length - index * 100)));
}
