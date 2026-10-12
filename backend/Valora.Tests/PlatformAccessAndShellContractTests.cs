using System.Text.RegularExpressions;
using Valora.Application.Access;

namespace Valora.Tests;

public sealed class PlatformAccessAndShellContractTests {
    [Fact]
    public void Canonical_permission_codes_are_unique() {
        var duplicates = ValoraPermissions.All
            .GroupBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Action_evolution_and_journey_permissions_remain_canonical() {
        string[] expected =
        [
            "action.read", "action.manage", "action.approve", "action.complete", "action.comments.manage",
            "evolution.read", "evolution.manage", "evolution.snapshots.generate",
            "journey.read", "journey.manage", "journey.events.create", "journey.events.manage"
        ];

        Assert.All(expected, permission => Assert.True(ValoraAccessCatalog.IsCanonicalPermission(permission), permission));
    }

    [Fact]
    public void PlatformCatalogMapsEveryPermissionWithoutPrefixGuessing() {
        var capabilities = ValoraAccessCatalog.CapabilitiesForStrict(ValoraPermissions.All);
        Assert.NotEmpty(capabilities);
        Assert.Contains("identity", capabilities);
        Assert.Contains("operations", capabilities);
        Assert.Contains("organizational_intelligence", ValoraAccessCatalog.PlatformModules);
        Assert.Equal(ValoraAccessCatalog.PlatformRole, "admin_valora");
    }

    [Fact]
    public void UnitsIsAnOfficialOrganizationPermission() {
        Assert.Equal("organization", ValoraAccessCatalog.PermissionCapability(ValoraPermissions.Units.Read));
        Assert.Equal(new[] { "organization" }, ValoraAccessCatalog.CapabilitiesForStrict(["units.read"]));
    }

    [Fact]
    public void RuntimeResolutionDeniesUnknownPermissionWithoutBreakingLogin() {
        var warnings = new List<string>();
        var capabilities = ValoraAccessCatalog.CapabilitiesFor(["units.read", "legacy.unknown"], warnings.Add);
        Assert.Equal(new[] { "organization" }, capabilities);
        Assert.Equal(new[] { "legacy.unknown" }, warnings);
    }

    [Fact]
    public void RoleAssignmentPolicyReservesPlatformRolesForGlobalAdministrators() {
        var roles = new[]
        {
            new AccessRoleDto(Guid.NewGuid(), ValoraAccessCatalog.PlatformRole, "Administrador Valora", null, true, "active", 1, 0, []),
            new AccessRoleDto(Guid.NewGuid(), "consultor_valora", "Consultor Valora", null, true, "active", 1, 0, []),
            new AccessRoleDto(Guid.NewGuid(), "empresa_admin", "Administrador da empresa", null, true, "active", 1, 0, []),
            new AccessRoleDto(Guid.NewGuid(), "papel_inativo", "Inativo", null, false, "inactive", 1, 0, [])
        };

        var tenantAssignable = RoleAssignmentPolicy.FilterAssignable(roles, actorIsGlobalAdministrator: false).Select(role => role.Code).ToArray();
        Assert.Equal(new[] { "empresa_admin" }, tenantAssignable);

        var globalAssignable = RoleAssignmentPolicy.FilterAssignable(roles, actorIsGlobalAdministrator: true).Select(role => role.Code).ToArray();
        Assert.Contains(ValoraAccessCatalog.PlatformRole, globalAssignable);
        Assert.Contains("consultor_valora", globalAssignable);
        Assert.Contains("empresa_admin", globalAssignable);
        Assert.DoesNotContain("papel_inativo", globalAssignable);
    }

    [Fact]
    public void EveryPermissionSeededByTheCompleteDatabaseScriptIsCanonical() {
        var root = FindRepositoryRoot();
        var sql = File.ReadAllText(Path.Combine(root, "backend/database/postgresql/script_completo.sql"));
        var insertStatements = Regex.Matches(sql, @"INSERT INTO valorapesquisa\.permissions\([^;]+?;",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var seeded = insertStatements.SelectMany(statement =>
                Regex.Matches(statement.Value, @"\('([a-z_]+(?:\.[a-z_]+)+)'").Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var unknown = seeded.Where(permission => !ValoraAccessCatalog.IsCanonicalPermission(permission)).ToArray();
        Assert.NotEmpty(seeded);
        Assert.Empty(unknown);
    }

    [Fact]
    public void AdminValoraLoginPathUsesCompleteCatalogAndDoesNotDependOnTenantAccessService() {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "backend/Valora.Application/Services/Auth/AuthService.cs"));
        Assert.Contains("roles.Contains(ValoraAccessCatalog.PlatformRole", source);
        Assert.Contains("var permissions = ValoraPermissions.All", source);
        Assert.Contains("ValoraAccessCatalog.PlatformModules", source);
        Assert.Contains("ResolveCapabilitiesSafely(permissions)", source);
        Assert.DoesNotContain("Split(\".\")", source);
        Assert.DoesNotContain("Split('.')", source);
    }

    [Fact]
    public void AdministrativeLayoutLoadsCanonicalHiddenRuleAndAdminModulesInOrder() {
        var root = FindRepositoryRoot();
        var layout = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Views/Shared/_AdminLayout.cshtml"));
        Assert.True(layout.IndexOf("design-system/components.css", StringComparison.Ordinal) < layout.IndexOf("valora-v9.css", StringComparison.Ordinal));
        Assert.True(layout.IndexOf("valora-v9.css", StringComparison.Ordinal) < layout.IndexOf("valora-admin.css", StringComparison.Ordinal));
        Assert.True(layout.IndexOf("valora-admin.css", StringComparison.Ordinal) < layout.IndexOf("design-system/responsive.css", StringComparison.Ordinal));
        Assert.Contains("[hidden]", File.ReadAllText(Path.Combine(root, "backend/Valora.Web/wwwroot/css/design-system/tokens.css")));
        var topbar = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Views/Shared/_Topbar.cshtml"));
        Assert.Contains("<details class=\"quick-actions\"", topbar);
        Assert.DoesNotContain("onclick=\"confirm(", topbar, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CriticalTypedAndWorkerRepositoriesNeverUseSelectStar() {
        var root = FindRepositoryRoot();
        var repositories = new[]
        {
            "IntelligenceProcessingJobRepository.cs",
            "UserRepository.cs",
            "CommunicationRepository.cs"
        };

        foreach (var repository in repositories) {
            var source = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories", repository));
            Assert.DoesNotMatch(@"(?i)SELECT\s+\*", source);
        }
    }

    [Fact]
    public void AdministrativeUserCreationUsesCanonicalRoleAssignmentPolicyOnPostAndOptions() {
        var root = FindRepositoryRoot();
        var controller = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Controllers/AdminHubController.cs"));
        Assert.Contains("CreateAdministrativeAsync(org, Actor, Context.IsGlobalAdministrator", controller);
        Assert.Contains("ListAssignableRolesAsync(org, Context.IsGlobalAdministrator", controller);
        Assert.DoesNotContain("ListRolesAsync(org, CancellationToken.None)).Select(r => new SelectListItem", controller);
    }

    [Fact]
    public void UserEditUsesSingleTransactionalAdministrativeCommand() {
        var root = FindRepositoryRoot();
        var controller = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Controllers/AdminHubController.cs"));
        var repository = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories/UserAdministrationRepository.cs"));
        Assert.Contains("UpdateAdministrativeAsync(org, Actor, Context.IsGlobalAdministrator, id", controller);
        Assert.DoesNotContain("SetRolesAsync(org, Actor, id", controller);
        Assert.DoesNotContain("UpdateStatusAsync(org, Actor, id", controller);
        Assert.Contains("using var tx = c.BeginTransaction()", repository);
        Assert.Contains("FOR UPDATE", repository);
        Assert.Contains("LAST_ADMINISTRATOR", repository);
        Assert.Contains("INSERT INTO valorapesquisa.audit_logs", repository);
        Assert.Contains("UPDATE valorapesquisa.user_sessions SET revoked_at", repository);
        Assert.Contains("tx.Commit()", repository);
    }

    [Fact]
    public void UserRepositoriesRejectInactiveRolesAndPreferOrganizationSpecificRoleCode() {
        var root = FindRepositoryRoot();
        var userRepository = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories/UserRepository.cs"));
        var adminRepository = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories/UserAdministrationRepository.cs"));
        Assert.Contains("status='active'", userRepository);
        Assert.Contains("ORDER BY (organization_id IS NULL)", userRepository);
        Assert.Contains("DISTINCT ON (code)", adminRepository);
        Assert.Contains("status='active'", adminRepository);
        Assert.Contains("ORDER BY code,(organization_id IS NULL)", adminRepository);
    }

    [Fact]
    public void DiagnosticWorkspaceResponsesUseExplicitPublicProjection() {
        var root = FindRepositoryRoot();
        var repository = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories/DiagnosticWorkspaceRepository.cs"));
        Assert.Contains("jsonb_build_object(", repository);
        Assert.Contains("'answerCount'", repository);
        Assert.Contains("'hasQualitativeAnswer'", repository);
        Assert.DoesNotContain("to_jsonb(x)-'participant_email'-'participant_name'", repository);
        Assert.DoesNotContain("'participant_phone'", repository);
        Assert.DoesNotContain("'result_token_hash'", repository);
    }

    [Fact]
    public void DiagnosticWorkspaceActionTabUsesActionCenterAsCanonicalExecutionSource() {
        var root = FindRepositoryRoot();
        var repository = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories/DiagnosticWorkspaceRepository.cs"));
        var script = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/wwwroot/js/pages/diagnostic-workspace.js"));
        Assert.Contains("ExecutionAsync", repository);
        Assert.Contains("action_plans p", repository);
        Assert.Contains("action_items i", repository);
        Assert.Contains("action_plan_change_history", repository);
        Assert.Contains("action_item_status_history", repository);
        Assert.Contains("originType=diagnostic", script);
        Assert.Contains("/ActionCenter/Plans/Details/", script);
        Assert.Contains("/ActionCenter/Items/Details/", script);
    }

    [Fact]
    public void DiagnosticWorkspaceExecutionDoesNotExposePlanSiblingsFromSingleLinkedItem() {
        var root = FindRepositoryRoot();
        var repository = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories/DiagnosticWorkspaceRepository.cs"));

        Assert.Contains("direct_plans", repository);
        Assert.Contains("OR p.id IN (SELECT id FROM direct_plans)", repository);
        Assert.DoesNotContain("OR p.id IN (SELECT id FROM linked_plans)", repository);
    }

    [Fact]
    public void DiagnosticWorkspaceCampaignFormCollectsEmailRecipientsAndLegalBasis() {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/wwwroot/js/pages/diagnostic-workspace.js"));

        Assert.Contains("name=\"recipients\"", script);
        Assert.Contains("hasConsent", script);
        Assert.Contains("hasLegalBasis", script);
        Assert.Contains("uniqueRecipients().map(email => ({ email", script);
        Assert.DoesNotContain("recipients: []", script);
        Assert.Contains("allowedTabs", script);
        Assert.Contains("activeRequest", script);
        Assert.Contains("mappedEvidenceCount", script);
        Assert.DoesNotContain("'Mapeada' : 'A calcular'", script);
    }

    [Fact]
    public void EvolutionCycleCanStartFromDiagnosticResultWithoutManualGuid() {
        var root = FindRepositoryRoot();
        var controller = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Controllers/EvolutionController.cs"));
        var repository = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories/EvolutionRepository.cs"));
        var cyclesView = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Views/Evolution/Cycles.cshtml"));
        var workspace = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/wwwroot/js/pages/diagnostic-workspace.js"));

        Assert.Contains("ResultOptions", controller);
        Assert.Contains("selectedResult?.Percentage", controller);
        Assert.Contains("Selecione um resultado calculado", controller);
        Assert.Contains("result_scores rs", repository);
        Assert.Contains("metadata_json->>'baselineSource'", repository);
        Assert.Contains("diagnostic_id DiagnosticId,result_id ResultId", repository);
        Assert.Contains("name=\"ResultId\"", cyclesView);
        Assert.Contains("GUIDs não são digitados manualmente", cyclesView);
        Assert.Contains("/Evolution/Cycles?diagnosticId=", workspace);
    }

    [Fact]
    public void EvolutionSnapshotsUseCycleScopeInsteadOfOrganizationWideCounts() {
        var root = FindRepositoryRoot();
        var repository = File.ReadAllText(Path.Combine(root, "backend/Valora.Infrastructure/Repositories/EvolutionRepository.cs"));

        Assert.Contains("scoped_plans", repository);
        Assert.Contains("cyc.diagnostic_id", repository);
        Assert.Contains("cyc.result_id", repository);
        Assert.DoesNotContain("FROM valorapesquisa.action_items WHERE organization_id=@o AND deleted_at IS NULL), alerts", repository);
    }

    [Fact]
    public void OrganizationSelectorUsesServerPaginationSearchAndPreservesSelectedOrganization() {
        var root = FindRepositoryRoot();
        var controller = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Controllers/BffAuthController.cs"));
        var script = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/wwwroot/js/shell/topbar-shell.js"));
        Assert.Contains("[FromQuery] string? search", controller);
        Assert.Contains("pageSize = Math.Clamp(pageSize, 10, 100)", controller);
        Assert.Contains("selectedOrganizationId", controller);
        Assert.DoesNotContain("page=1&pageSize=100&status=active", controller);
        Assert.Contains("organizationAbort?.abort()", script);
        Assert.Contains("selectedOrganizationId", script);
        Assert.Contains("valora:organization-changing", script);
    }

    private static string FindRepositoryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend/Valora.Web"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
