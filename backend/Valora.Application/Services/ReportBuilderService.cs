using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class ReportBuilderService(IResponseRepository responses, ISurveyRepository surveys, IOrganizationRepository orgs) {
    private const int MinimumExecutiveSample = 5;

    public async Task<string> BuildAsync(Guid organizationId, Guid? surveyId, Guid? responseId, string format) {
        if (responseId.HasValue) {
            var result = await responses.GetAdminAsync(organizationId, responseId.Value)
                ?? throw new InvalidOperationException("Resultado não encontrado no escopo da organização.");
            if (!string.Equals(result.ProcessingStatus, "available", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("O resultado ainda não está disponível para relatório.");
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
                return $"diagnostico,versao,respostas_elegiveis,pontuacao,maximo,percentual,classificacao,processado_em\n\"{EscapeCsv(result.SurveyTitle)}\",{result.FormVersion},{result.EligibleResponseCount},{result.TotalScore?.ToString(System.Globalization.CultureInfo.InvariantCulture)},{result.MaxScore?.ToString(System.Globalization.CultureInfo.InvariantCulture)},{result.Percentage?.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"{EscapeCsv(result.MaturityLabel ?? "Não determinada")}\",{result.ProcessedAt:O}";
            return JsonSerializer.Serialize(new {
                identification = new { result.ResponseId, result.ResultId, result.OrganizationName, diagnostic = result.SurveyTitle, result.PeriodStart, result.PeriodEnd, result.FormVersionId, result.FormVersion, result.ProcessedAt },
                responseCount = result.EligibleResponseCount,
                evidence = new { source = "Resultado processado e persistido", resultId = result.ResultId, completedResponses = result.EligibleResponseCount },
                executive = new { sufficientData = result.TotalScore.HasValue, overallScore = result.TotalScore, maximumScore = result.MaxScore, percentage = result.Percentage, maturityLevel = result.MaturityLabel, summary = result.StrategicTruth, risk = result.RiskIfNothingChanges, nextLevel = result.NextLevel },
                dimensions = result.Dimensions,
                limitations = result.Dimensions.Count == 0 ? new[] { "A cobertura por dimensão não está disponível neste resultado." } : new[] { "Comparações exigem a mesma versão metodológica e bases compatíveis." },
                generatedAt = DateTimeOffset.UtcNow
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        var organization = await orgs.GetAsync(organizationId);
        var allResponses = await responses.ListAdminAsync(organizationId);
        var selected = allResponses.Where(item => !surveyId.HasValue || ReadGuid(item, "survey_id") == surveyId).ToList();
        if (responseId.HasValue) selected = selected.Where(item => ReadGuid(item, "id") == responseId).ToList();
        var completed = selected.Count(item => string.Equals(Read(item, "status"), "completed", StringComparison.OrdinalIgnoreCase));
        var sufficient = selected.Count >= MinimumExecutiveSample;
        var warning = sufficient ? null : "Os dados disponíveis ainda são insuficientes para sustentar uma conclusão executiva completa.";
        var survey = surveyId.HasValue ? await surveys.GetAdminAsync(organizationId, surveyId.Value) : null;
        var payload = new {
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

    private static string? Read(object? value, string name) {
        if (value is null) return null;
        if (value is IDictionary<string, object> values) {
            var pair = values.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase));
            return pair.Value?.ToString();
        }
        return value.GetType().GetProperty(name, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.GetValue(value)?.ToString();
    }
    private static Guid? ReadGuid(object? value, string name) => Guid.TryParse(Read(value, name), out var id) ? id : null;
    private static DateTimeOffset? ReadDate(object? value, string name) => DateTimeOffset.TryParse(Read(value, name), out var date) ? date : null;
    private static string EscapeCsv(string value) => value.Replace("\"", "\"\"");
}
