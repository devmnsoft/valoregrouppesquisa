namespace Valora.Application.Access;

public static class ValoraModules
{
    public const string Identity = "identity"; public const string Organization = "organization";
    public const string Forms = "forms"; public const string Surveys = "surveys";
    public const string Distribution = "distribution"; public const string Responses = "responses";
    public const string Results = "results"; public const string Certificates = "certificates";
    public const string Communications = "communications"; public const string Audit = "audit";
    public const string Settings = "settings"; public const string Operations = "operations";

    public static readonly IReadOnlyList<string> All = [Identity, Organization, Forms, Surveys, Distribution,
        Responses, Results, Certificates, Communications, Audit, Settings, Operations];
}

/// <summary>Canonical access vocabulary shared by authentication and navigation.</summary>
public static class ValoraAccessCatalog
{
    public const string PlatformRole = "admin_valora";
    public const string ContextVersion = "2";

    private static readonly IReadOnlyDictionary<string, string> ModuleAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["intelligence"] = "organizational_intelligence", ["inteligenciaorganizacional"] = "organizational_intelligence",
            ["relatorios"] = "reports", ["diagnostics"] = "surveys", ["users"] = "identity", ["plans"] = "organization"
        };

    public static readonly IReadOnlyList<string> PlatformModules = ValoraModules.All
        .Concat(["organizational_intelligence", "reports", "dashboard", "enterprise"])
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static string NormalizeModule(string module)
    {
        var normalized = module.Trim().ToLowerInvariant().Replace('-', '_');
        return ModuleAliases.TryGetValue(normalized, out var canonical) ? canonical : normalized;
    }

    private static readonly IReadOnlyDictionary<string, string> PermissionCapabilities =
        BuildPermissionCapabilities();

    private static IReadOnlyDictionary<string, string> BuildPermissionCapabilities()
    {
        var duplicate = ValoraPermissions.Definitions
            .GroupBy(definition => definition.Permission, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate permission code found: {duplicate.Key}");

        return ValoraPermissions.Definitions.ToDictionary(
            definition => definition.Permission,
            definition => definition.Capability,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Runtime-safe resolution. Unknown database values are denied and reported, never inferred.</summary>
    public static IReadOnlyList<string> CapabilitiesFor(IEnumerable<string> permissions, Action<string>? reportUnknown = null)
    {
        var capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var permission in permissions.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (PermissionCapabilities.TryGetValue(permission, out var capability)) capabilities.Add(capability);
            else reportUnknown?.Invoke(permission);
        }
        return capabilities.ToArray();
    }

    /// <summary>Strict resolution for seed/catalog validation and tests.</summary>
    public static IReadOnlyList<string> CapabilitiesForStrict(IEnumerable<string> permissions) => permissions
        .Select(PermissionCapability).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static string PermissionCapability(string permission) =>
        PermissionCapabilities.TryGetValue(permission, out var capability)
            ? capability
            : throw new InvalidOperationException($"Permissão fora do catálogo canônico: {permission}");

    public static bool IsCanonicalPermission(string permission) => PermissionCapabilities.ContainsKey(permission);
}

public static class ValoraPermissions
{
    public static class Plans { public const string Read="plans.read", Manage="plans.manage"; }
    public static class Subscriptions { public const string Read="subscriptions.read", Manage="subscriptions.manage"; }
    public static class Billing { public const string Read="billing.read", Manage="billing.manage"; }
    public static class Usage { public const string Read="usage.read", Manage="usage.manage"; }
    public static class Upgrades { public const string Manage="upgrades.manage"; }
    public static class Organization { public const string Read="organization.read", Update="organization.update", BrandingRead="organization.branding.read", BrandingUpdate="organization.branding.update", SubscriptionRead="organization.subscription.read", UsageRead="organization.usage.read"; }
    public static class OrganizationCurrent { public const string Read="organization.current.read", Update="organization.current.update"; }
    public static class OrganizationOnboarding { public const string Read="organization.onboarding.read", Update="organization.onboarding.update"; }
    public static class Units { public const string Read="units.read", Create="units.create", Update="units.update", Disable="units.disable", Delete="units.delete"; }
    public static class Departments { public const string Read="departments.read", Create="departments.create", Update="departments.update", Disable="departments.disable", Delete="departments.delete"; }
    public static class BusinessGroups { public const string Read="business_groups.read", Create="business_groups.create", Update="business_groups.update", Disable="business_groups.disable", Delete="business_groups.delete"; }
    public static class LegalEntities { public const string Read="legal_entities.read", Create="legal_entities.create", Update="legal_entities.update", Disable="legal_entities.disable", Delete="legal_entities.delete"; }
    public static class Invitations { public const string Read="invitations.read", Create="invitations.create", Resend="invitations.resend", Cancel="invitations.cancel"; }
    public static class Sessions { public const string Read="sessions.read", Revoke="sessions.revoke"; }
    public static class Users { public const string Read="users.read", Create="users.create", Update="users.update", Disable="users.disable", AssignRoles="users.assign_roles", AssignScopes="users.assign_scopes"; }
    public static class Roles { public const string Read="roles.read", Create="roles.create", Update="roles.update", Delete="roles.delete", AssignPermissions="roles.assign_permissions"; }
    public static class Forms { public const string Read="forms.read", Create="forms.create", Update="forms.update", Publish="forms.publish", Archive="forms.archive", Restore="forms.restore"; }
    public static class Surveys { public const string Read="surveys.read", Create="surveys.create", Update="surveys.update", Publish="surveys.publish", Distribute="surveys.distribute", Close="surveys.close"; }
    public static class Responses { public const string Read="responses.read", Export="responses.export", Anonymize="responses.anonymize"; }
    public static class Results { public const string Read="results.read", Export="results.export", Compare="results.compare"; }
    public static class Diagnostics { public const string Read="diagnostics.read", Manage="diagnostics.manage"; }
    public static class Onboarding { public const string Read="onboarding.read", Manage="onboarding.manage"; }
    public static class Campaigns { public const string Read="campaigns.read", Manage="campaigns.manage"; }
    public static class Respondents { public const string Read="respondents.read", Manage="respondents.manage"; }
    public static class Evidence { public const string Read="evidence.read", Manage="evidence.manage"; }
    public static class Indexes { public const string Read="indexes.read"; }
    public static class Priorities { public const string Read="priorities.read", Manage="priorities.manage"; }
    public static class Leadership { public const string Read="leadership.read", Manage="leadership.manage"; }
    public static class Branding { public const string Manage="branding.manage"; }
    public static class WorkflowForms { public const string Manage="forms.manage"; }
    public static class WorkflowResponses { public const string Submit="responses.submit"; }
    public static class WorkflowResults { public const string Manage="results.manage"; }
    public static class Intelligence { public const string Read="intelligence.read", Process="intelligence.process"; }
    public static class Deliverables { public const string Read="deliverables.read", Manage="deliverables.manage"; }
    public static class Reports { public const string Read="reports.read", Generate="reports.generate", Download="reports.download"; }
    public static class Certificates { public const string Read="certificates.read", Generate="certificates.generate", Download="certificates.download", Revoke="certificates.revoke", Validate="certificates.validate"; }
    public static class ShareLinks { public const string Read="share_links.read", Manage="share_links.manage"; }
    public static class PublicResults { public const string Manage="public_results.manage"; }
    public static class Communications { public const string Read="communications.read", Send="communications.send", Retry="communications.retry", Cancel="communications.cancel"; }
    public static class Audit { public const string Read="audit.read"; }
    public static class Operations { public const string Read="operations.read", Execute="operations.execute"; }
    public static class Settings { public const string Read="settings.read", Update="settings.update"; }
    public static class SettingsManagement { public const string Manage="settings.manage"; }
    public static class OrganizationalIntelligence { public const string Read="organizational_intelligence.read", Generate="organizational_intelligence.generate", JourneyCreate="organizational_intelligence.journey.create", ActionCreate="organizational_intelligence.action.create"; }
    public static class DecisionCenter { public const string Read="decision_center.read"; }
    public static class Decisions { public const string Read="decisions.read", Manage="decisions.manage", Approve="decisions.approve"; }
    public static class Alerts { public const string Read="alerts.read", Manage="alerts.manage", Resolve="alerts.resolve"; }
    public static class Indicators { public const string Read="indicators.read", Manage="indicators.manage"; }
    public static class Governance { public const string Read="governance.read", Manage="governance.manage", MeetingsRead="governance.meetings.read", MeetingsManage="governance.meetings.manage"; }
    public static class Action { public const string Read="action.read", Manage="action.manage", Approve="action.approve", Complete="action.complete", CommentsManage="action.comments.manage"; }
    public static class Evolution { public const string Read="evolution.read", Manage="evolution.manage", SnapshotsGenerate="evolution.snapshots.generate"; }
    public static class Journey { public const string Read="journey.read", Manage="journey.manage", EventsCreate="journey.events.create", EventsManage="journey.events.manage"; }
    public static class IntelligentDeliverables
    {
        public const string DashboardRead="dashboard.read", RadarRead="radar.read", HeatmapRead="heatmap.read",
            BenchmarkRead="benchmark.read", InsightsRead="insights.read";

        // Compatibility aliases point to the single canonical definitions above. Properties are
        // deliberately not reflected into All, so each permission code is registered only once.
        public static string ActionRead => Action.Read;
        public static string ActionManage => Action.Manage;
        public static string EvolutionRead => Evolution.Read;
        public static string EvolutionManage => Evolution.Manage;
        public static string JourneyRead => Journey.Read;
    }
    public static class Benchmark
    {
        public const string Generate="benchmark.generate", Compare="benchmark.compare",
            Export="benchmark.export", Admin="benchmark.admin";
    }
    public static class OneOnOne
    {
        public const string Read="one_on_one.read", Manage="one_on_one.manage", Schedule="one_on_one.schedule",
            NotesManage="one_on_one.notes.manage", FeedbackManage="one_on_one.feedback.manage";
    }
    public static class LeadershipDevelopment
    {
        public const string Read="leadership_development.read", Manage="leadership_development.manage";
    }
    public static class Ai
    {
        public const string Read="ai.read", Manage="ai.manage", RunsRead="ai.runs.read", RunsManage="ai.runs.manage",
            PromptsRead="ai.prompts.read", PromptsManage="ai.prompts.manage", InsightsRead="ai.insights.read",
            InsightsReview="ai.insights.review", InsightsPublish="ai.insights.publish", Reprocess="ai.reprocess",
            UsageRead="ai.usage.read";
    }
    public static class Administration
    {
        public const string Read="administration.read", Manage="administration.manage";
        public const string OrganizationsRead="organizations.read", OrganizationsManage="organizations.manage",
            QuestionsRead="questions.read", QuestionsManage="questions.manage", IntelligenceManage="intelligence.manage",
            IntegrationsRead="integrations.read", IntegrationsManage="integrations.manage",
            NotificationsRead="notifications.read", NotificationsManage="notifications.manage", JobsRead="jobs.read",
            JobsManage="jobs.manage", LogsRead="logs.read", SupportRead="support.read", SupportManage="support.manage";
    }
    public static class Methodology
    {
        public const string Read="methodology.read", Manage="methodology.manage", Publish="methodology.publish", Clone="methodology.clone", Validate="methodology.validate";
        public const string ConceptsRead="methodology.concepts.read", ConceptsManage="methodology.concepts.manage";
        public const string IndexesRead="methodology.indexes.read", IndexesManage="methodology.indexes.manage";
        public const string QuestionsRead="methodology.questions.read", QuestionsManage="methodology.questions.manage";
        public const string PromptsRead="methodology.prompts.read", PromptsManage="methodology.prompts.manage";
        public const string GuardrailsRead="methodology.guardrails.read", GuardrailsManage="methodology.guardrails.manage";
    }

    public static readonly IReadOnlyList<string> All = typeof(ValoraPermissions).GetNestedTypes()
        .SelectMany(type => type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        .Where(field => field.IsLiteral && field.FieldType == typeof(string)).Select(field => (string)field.GetRawConstantValue()!).ToArray();

    internal static readonly IReadOnlyList<(string Permission, string Capability)> Definitions = All
        .Select(permission => (permission, CapabilityForCanonicalPermission(permission))).ToArray();

    private static string CapabilityForCanonicalPermission(string permission) => permission.Split('.')[0] switch
    {
        "users" or "roles" or "sessions" or "invitations" => ValoraModules.Identity,
        "organization" or "units" or "departments" or "business_groups" or "legal_entities" or "plans" or
        "subscriptions" or "billing" or "usage" or "upgrades" => ValoraModules.Organization,
        "organizational_intelligence" or "methodology" or "decision_center" or "decisions" or "alerts" or "indicators" or "governance" or "diagnostics" or "dashboard" or "radar" or "reports" or "deliverables" or "share_links" or "public_results" or "action" or "heatmap" or
        "evolution" or "journey" or "benchmark" or "insights" or "ai" or "one_on_one" or
        "evidence" or "indexes" or "priorities" or "leadership" or "leadership_development" or "intelligence" or "questions" => "organizational_intelligence",
        "onboarding" or "branding" => ValoraModules.Organization,
        "campaigns" => ValoraModules.Surveys,
        "respondents" => ValoraModules.Responses,
        "organizations" => ValoraModules.Organization,
        "administration" or "integrations" or "notifications" or "jobs" or "logs" or "support" => ValoraModules.Operations,
        var capability => capability
    };
}
