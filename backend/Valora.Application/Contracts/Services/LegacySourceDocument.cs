using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public sealed record LegacySourceDocument(string Collection,string LegacyId,string TargetEntity,string MaskedJson,string NormalizedMaskedJson,IReadOnlyList<string> UnmappedFields,IReadOnlyList<string> SensitiveFields);
