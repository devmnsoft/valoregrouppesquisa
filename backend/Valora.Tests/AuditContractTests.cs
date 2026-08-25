using Valora.Application.DTOs;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "Unit")]
public sealed class AuditContractTests
{
    [Fact]
    public void Null_metadata_is_normalized_without_changing_text_entity_identifiers()
    {
        var entry = new AuditEntry(null, null, "certificate.validated", "certificate", "VALORA-2026-A", null, null, "corr-1");

        Assert.Equal("{}", entry.MetadataJson);
        Assert.Equal("VALORA-2026-A", entry.EntityId);
        Assert.Equal("corr-1", entry.CorrelationId);
        Assert.Equal("info", entry.Severity);
    }

    [Theory]
    [InlineData("WARNING", "warning")]
    [InlineData(" error ", "error")]
    [InlineData("success", "info")]
    [InlineData("high", "info")]
    [InlineData(null, "info")]
    public void Severity_is_normalized_to_the_database_contract(string? supplied, string expected)
    {
        var entry = new AuditEntry(null, null, "test", null, null, null, severity: supplied);

        Assert.Equal(expected, entry.Severity);
    }
}
