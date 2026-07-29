using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public sealed record LegacySourceReadResult(string SourceType,string SourceName,string Sha256,IReadOnlyList<LegacySourceDocument> Documents);
