using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class ManualJsonReader(
    ILegacyMappingService m,
    ILegacyDataNormalizer n) : JsonLegacySourceReader(m, n), IManualJsonReader
{
    public override bool CanRead(string sourceType) =>
        sourceType.Equals("manual", StringComparison.OrdinalIgnoreCase)
        || sourceType.Equals("json", StringComparison.OrdinalIgnoreCase);
}
