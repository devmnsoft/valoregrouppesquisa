using System.IO.Compression;
using System.Text;
using Valora.Application.Contracts;
using Valora.Application.Exports;

namespace Valora.Tests;

public sealed class ExportDocumentRendererTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private readonly ExportDocumentRenderer renderer = new();

    [Fact]
    public void CsvProtectsSpreadsheetFormulaInjection()
    {
        var export = renderer.Render(Job("csv"), Data("=HYPERLINK(\"https://invalid\")"), DateTimeOffset.UnixEpoch);
        var csv = Encoding.UTF8.GetString(export.Content);

        Assert.Equal("text/csv; charset=utf-8", export.MimeType);
        Assert.Contains("'=HYPERLINK", csv);
        Assert.NotEmpty(export.ChecksumSha256);
    }

    [Fact]
    public void XlsxIsAValidOpenXmlZipWithWorksheet()
    {
        var export = renderer.Render(Job("xlsx"), Data("valor"), DateTimeOffset.UnixEpoch);
        using var zip = new ZipArchive(new MemoryStream(export.Content), ZipArchiveMode.Read);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.MimeType);
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
        Assert.NotNull(zip.GetEntry("xl/worksheets/sheet1.xml"));
    }

    [Fact]
    public void PdfHasSignatureAndJsonCarriesEvidenceCountWithoutPiiFields()
    {
        var pdf = renderer.Render(Job("pdf"), Data("observado"), DateTimeOffset.UnixEpoch);
        var json = renderer.Render(Job("json"), Data("observado"), DateTimeOffset.UnixEpoch);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.Content));
        var jsonText = Encoding.UTF8.GetString(json.Content);
        Assert.Contains("\"evidenceCount\": 1", jsonText);
        Assert.DoesNotContain("participantEmail", jsonText, StringComparison.OrdinalIgnoreCase);
    }

    private static ExportWorkItem Job(string format) => new(Guid.NewGuid(), OrganizationId, "responses", format, "{}", 1, 3, "test-correlation");
    private static ExportDataSet Data(string value) => new(["id", "status"],
        [new Dictionary<string, object?> { ["id"] = Guid.NewGuid(), ["status"] = value }]);
}
