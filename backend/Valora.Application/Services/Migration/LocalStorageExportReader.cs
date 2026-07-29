using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class LocalStorageExportReader(
    ILegacyMappingService m,
    ILegacyDataNormalizer n) : JsonLegacySourceReader(m, n), ILocalStorageExportReader
{
    public override bool CanRead(string sourceType) =>
        sourceType.Equals("localStorage", StringComparison.OrdinalIgnoreCase)
        || sourceType.Equals("local", StringComparison.OrdinalIgnoreCase);
}
