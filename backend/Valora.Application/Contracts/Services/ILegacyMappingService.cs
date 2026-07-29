using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ILegacyMappingService { string MapCollectionToTarget(string collection); IReadOnlyList<string> GetUnmappedFields(string collection,IEnumerable<string> fields); }
