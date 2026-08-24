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
}
