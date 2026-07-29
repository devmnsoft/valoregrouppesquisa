using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class LegacyMappingService : ILegacyMappingService
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        { "companies", "organizations" },
        { "organizations", "organizations" },
        { "companyProfiles", "organizations" },
        { "users", "users" },
        { "authUsers", "users" },
        { "companyUsers", "users" },
        { "participants", "survey_participants" },
        { "plans", "plans" },
        { "modules", "modules" },
        { "companyModules", "organization_modules" },
        { "subscription", "subscriptions" },
        { "forms", "forms" },
        { "questions", "questions" },
        { "dimensions", "form_dimensions" },
        { "options", "question_options" },
        { "surveys", "surveys" },
        { "publicLinks", "survey_links" },
        { "surveyLinks", "survey_links" },
        { "invites", "survey_invites" },
        { "responses", "responses" },
        { "answers", "response_answers" },
        { "results", "result_scores" },
        { "scores", "dimension_scores" },
        { "certificates", "certificates" },
        { "emailJobs", "email_jobs" },
        { "communications", "communications" },
        { "outbox", "email_jobs" },
        { "auditLogs", "audit_logs" },
        { "logs", "audit_logs" },
        { "events", "audit_logs" },
        { "consents", "lgpd_consents" },
        { "privacyRequests", "privacy_requests" }
    };

    public string MapCollectionToTarget(string collection) =>
        Map.GetValueOrDefault(collection, "manual_review");

    public IReadOnlyList<string> GetUnmappedFields(string collection, IEnumerable<string> fields) =>
        Map.ContainsKey(collection) ? Array.Empty<string>() : fields.ToArray();
}
