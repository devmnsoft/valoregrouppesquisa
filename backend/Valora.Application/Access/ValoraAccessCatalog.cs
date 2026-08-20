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

    public static IReadOnlyList<string> CapabilitiesFor(IEnumerable<string> permissions) => permissions
        .Select(PermissionCapability).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static string PermissionCapability(string permission) => permission switch
    {
        var value when value.StartsWith("users.", StringComparison.OrdinalIgnoreCase) || value.StartsWith("roles.", StringComparison.OrdinalIgnoreCase) => "identity",
        var value when value.StartsWith("results.", StringComparison.OrdinalIgnoreCase) => "results",
        var value when value.StartsWith("responses.", StringComparison.OrdinalIgnoreCase) => "responses",
        var value when value.StartsWith("forms.", StringComparison.OrdinalIgnoreCase) => "forms",
        var value when value.StartsWith("surveys.", StringComparison.OrdinalIgnoreCase) => "surveys",
        var value when value.StartsWith("organization.", StringComparison.OrdinalIgnoreCase) => "organization",
        var value when value.StartsWith("communications.", StringComparison.OrdinalIgnoreCase) => "communications",
        var value when value.StartsWith("audit.", StringComparison.OrdinalIgnoreCase) => "audit",
        var value when value.StartsWith("operations.", StringComparison.OrdinalIgnoreCase) => "operations",
        var value when value.StartsWith("settings.", StringComparison.OrdinalIgnoreCase) => "settings",
        var value when value.StartsWith("certificates.", StringComparison.OrdinalIgnoreCase) => "certificates",
        _ => throw new InvalidOperationException($"Permissão fora do catálogo canônico: {permission}")
    };
}

public static class ValoraPermissions
{
    public static class Organization { public const string Read="organization.read", Update="organization.update", BrandingRead="organization.branding.read", BrandingUpdate="organization.branding.update", SubscriptionRead="organization.subscription.read", UsageRead="organization.usage.read"; }
    public static class Units { public const string Read="units.read", Create="units.create", Update="units.update", Disable="units.disable"; }
    public static class Departments { public const string Read="departments.read", Create="departments.create", Update="departments.update", Disable="departments.disable"; }
    public static class Users { public const string Read="users.read", Create="users.create", Update="users.update", Disable="users.disable", AssignRoles="users.assign_roles", AssignScopes="users.assign_scopes"; }
    public static class Roles { public const string Read="roles.read", Create="roles.create", Update="roles.update", Delete="roles.delete", AssignPermissions="roles.assign_permissions"; }
    public static class Forms { public const string Read="forms.read", Create="forms.create", Update="forms.update", Publish="forms.publish", Archive="forms.archive", Restore="forms.restore"; }
    public static class Surveys { public const string Read="surveys.read", Create="surveys.create", Update="surveys.update", Publish="surveys.publish", Distribute="surveys.distribute", Close="surveys.close"; }
    public static class Responses { public const string Read="responses.read", Export="responses.export", Anonymize="responses.anonymize"; }
    public static class Results { public const string Read="results.read", Export="results.export", Compare="results.compare"; }
    public static class Certificates { public const string Read="certificates.read", Generate="certificates.generate", Revoke="certificates.revoke"; }
    public static class Communications { public const string Read="communications.read", Send="communications.send", Retry="communications.retry", Cancel="communications.cancel"; }
    public static class Audit { public const string Read="audit.read"; }
    public static class Operations { public const string Read="operations.read", Execute="operations.execute"; }
    public static class Settings { public const string Read="settings.read", Update="settings.update"; }

    public static readonly IReadOnlyList<string> All = typeof(ValoraPermissions).GetNestedTypes()
        .SelectMany(type => type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        .Where(field => field.IsLiteral && field.FieldType == typeof(string)).Select(field => (string)field.GetRawConstantValue()!).ToArray();
}
