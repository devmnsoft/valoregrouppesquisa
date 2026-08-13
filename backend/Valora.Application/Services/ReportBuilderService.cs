using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class ReportBuilderService(IResponseRepository responses, ISurveyRepository surveys, IOrganizationRepository orgs)
{
    private const int MinimumExecutiveSample = 5;

    public async Task<string> BuildAsync(Guid organizationId, Guid? surveyId, Guid? responseId, string format)
    {
        var organization = await orgs.GetAsync(organizationId);
        var allResponses = await responses.ListAdminAsync(organizationId);
        var selected = allResponses.Where(item => !surveyId.HasValue || ReadGuid(item, "survey_id") == surveyId).ToList();
        if (responseId.HasValue) selected = selected.Where(item => ReadGuid(item, "id") == responseId).ToList();
        var completed = selected.Count(item => string.Equals(Read(item, "status"), "completed", StringComparison.OrdinalIgnoreCase));
        var sufficient = selected.Count >= MinimumExecutiveSample;
        var warning = sufficient ? null : "Os dados disponíveis ainda são insuficientes para sustentar uma conclusão executiva completa.";
        var survey = surveyId.HasValue ? await surveys.GetAdminAsync(organizationId, surveyId.Value) : null;
        var payload = new
        {
            organization,
            diagnostic = survey,
            period = new { from = selected.Select(x => ReadDate(x, "created_at")).Where(x => x.HasValue).Min(), to = DateTimeOffset.UtcNow },
            responseCount = selected.Count,
            completionRate = selected.Count == 0 ? 0 : Math.Round(completed * 100m / selected.Count, 1),
            evidence = new { completedResponses = completed, source = "Respostas persistidas no diagnóstico selecionado" },
            executive = new { sufficientData = sufficient, warning, overallScore = (decimal?)null, maturityLevel = sufficient ? "Aguardando cálculo consolidado por dimensão" : "Não determinado", strengths = Array.Empty<string>(), weaknesses = Array.Empty<string>(), risks = Array.Empty<string>(), recommendations = Array.Empty<string>() },
            dimensions = Array.Empty<object>(),
            generatedAt = DateTimeOffset.UtcNow
        };
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            return $"diagnostico,respostas,concluidas,taxa_conclusao,dados_suficientes\n\"{EscapeCsv(Read(survey, "title") ?? "Consolidado da organização")}\",{selected.Count},{completed},{payload.completionRate.ToString(System.Globalization.CultureInfo.InvariantCulture)},{sufficient.ToString().ToLowerInvariant()}";
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static string? Read(object? value, string name)
    {
        if (value is null) return null;
        if (value is IDictionary<string, object> values)
        {
            var pair = values.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase));
            return pair.Value?.ToString();
        }
        return value.GetType().GetProperty(name, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.GetValue(value)?.ToString();
    }
    private static Guid? ReadGuid(object? value, string name) => Guid.TryParse(Read(value, name), out var id) ? id : null;
    private static DateTimeOffset? ReadDate(object? value, string name) => DateTimeOffset.TryParse(Read(value, name), out var date) ? date : null;
    private static string EscapeCsv(string value) => value.Replace("\"", "\"\"");
}
