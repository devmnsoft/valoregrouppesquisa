using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Methodology;

namespace Valora.Infrastructure.Repositories;

public sealed class MethodologyRepository(IDbConnectionFactory connections) : IMethodologyRepository
{
    public async Task<IReadOnlyList<MethodologyConceptDto>> ListConceptsAsync(string? search, string? pillar, CancellationToken ct)
    {
        const string sql = "SELECT id,code,name,pillar,definition,evolution_guidance EvolutionGuidance,related_indices RelatedIndices,deprecated_terms DeprecatedTerms,version FROM valorapesquisa.methodology_concepts WHERE deleted_at IS NULL AND (@search IS NULL OR name ILIKE '%'||@search||'%' OR definition ILIKE '%'||@search||'%') AND (@pillar IS NULL OR pillar=@pillar) ORDER BY pillar,name";
        using var c = connections.Create(); return (await c.QueryAsync<MethodologyConceptDto>(new CommandDefinition(sql, new { search = Blank(search), pillar = Blank(pillar) }, cancellationToken: ct))).ToList();
    }
    public async Task<MethodologyConceptDto?> GetConceptAsync(string code, CancellationToken ct)
    { using var c = connections.Create(); return await c.QuerySingleOrDefaultAsync<MethodologyConceptDto>(new CommandDefinition("SELECT id,code,name,pillar,definition,evolution_guidance EvolutionGuidance,related_indices RelatedIndices,deprecated_terms DeprecatedTerms,version FROM valorapesquisa.methodology_concepts WHERE lower(code)=lower(@code) AND deleted_at IS NULL", new { code }, cancellationToken: ct)); }
    public async Task<IReadOnlyList<MethodologyRelationDto>> ListRelationsAsync(string? conceptCode, CancellationToken ct)
    { const string sql = "SELECT r.id,s.code SourceCode,s.name SourceName,t.code TargetCode,t.name TargetName,r.relation_type RelationType,r.influence_weight InfluenceWeight,r.rationale FROM valorapesquisa.methodology_relations r JOIN valorapesquisa.methodology_concepts s ON s.id=r.source_concept_id JOIN valorapesquisa.methodology_concepts t ON t.id=r.target_concept_id WHERE r.deleted_at IS NULL AND (@code IS NULL OR lower(s.code)=lower(@code) OR lower(t.code)=lower(@code)) ORDER BY r.influence_weight DESC,s.name,t.name"; using var c=connections.Create(); return (await c.QueryAsync<MethodologyRelationDto>(new CommandDefinition(sql,new { code=Blank(conceptCode)},cancellationToken:ct))).ToList(); }
    public async Task<IReadOnlyList<MethodologyEvidenceDto>> ListEvidenceAsync(string conceptCode, CancellationToken ct)
    { const string sql="SELECT e.id,c.code ConceptCode,e.pattern_type PatternType,e.description,e.minimum_occurrences MinimumOccurrences,e.weight FROM valorapesquisa.methodology_evidence_patterns e JOIN valorapesquisa.methodology_concepts c ON c.id=e.concept_id WHERE lower(c.code)=lower(@conceptCode) AND e.deleted_at IS NULL ORDER BY e.pattern_type,e.weight DESC"; using var c=connections.Create(); return (await c.QueryAsync<MethodologyEvidenceDto>(new CommandDefinition(sql,new { conceptCode},cancellationToken:ct))).ToList(); }
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
