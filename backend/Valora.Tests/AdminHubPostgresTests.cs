using System.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Valora.Application.CompanyRegistration;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;
using Valora.Application.Services;
using Valora.Infrastructure.Database;
using Valora.Infrastructure.Repositories;
using Valora.Infrastructure.Security;
using Xunit;
using Xunit.Sdk;

namespace Valora.Tests;

/// <summary>Contract regressions for the real Admin Hub CRUD surfaces (organizations, users, registration idempotency).</summary>
[Trait("Category", "DatabaseContract")]
public sealed class AdminHubPostgresTests {
    private sealed class TestConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connectionString);
    }

    private static string Connection() {
        var connectionString = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw SkipException.ForSkip("VALORA_TEST_POSTGRES_CONNECTION não configurada; regressão PostgreSQL não executada.");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Matches("(?i)(test|teste|homolog|qa)", builder.Database ?? "");
        return connectionString;
    }

    private static async Task<int> ExecAsync(NpgsqlConnection connection, string sql, params (string Name, object? Value)[] parameters) {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql, params (string Name, object? Value)[] parameters) {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return await command.ExecuteScalarAsync();
    }

    private static async Task CleanupOrganizationAsync(string connectionString, params Guid[] organizationIds) {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var organizationId in organizationIds) {
            await ExecAsync(connection, """
                DELETE FROM valorapesquisa.outbox_messages WHERE aggregate_id=@o;
                DELETE FROM valorapesquisa.organization_consents WHERE organization_id=@o;
                DELETE FROM valorapesquisa.onboarding_steps WHERE organization_id=@o;
                DELETE FROM valorapesquisa.onboarding_checklists WHERE organization_id=@o;
                DELETE FROM valorapesquisa.audit_logs WHERE organization_id=@o;
                DELETE FROM valorapesquisa.organization_branding WHERE organization_id=@o;
                DELETE FROM valorapesquisa.organization_settings WHERE organization_id=@o;
                DELETE FROM valorapesquisa.subscriptions WHERE organization_id=@o;
                DELETE FROM valorapesquisa.user_scopes WHERE organization_id=@o;
                DELETE FROM valorapesquisa.user_roles WHERE user_id IN (SELECT id FROM valorapesquisa.users WHERE organization_id=@o);
                DELETE FROM valorapesquisa.users WHERE organization_id=@o;
                DELETE FROM valorapesquisa.addresses WHERE organization_id=@o;
                DELETE FROM valorapesquisa.units WHERE organization_id=@o;
                DELETE FROM valorapesquisa.legal_entities WHERE organization_id=@o;
                DELETE FROM valorapesquisa.idempotency_keys WHERE organization_id=@o;
                DELETE FROM valorapesquisa.roles WHERE organization_id=@o;
                DELETE FROM valorapesquisa.organizations WHERE id=@o;
                """, ("o", organizationId));
        }
    }

    private static async Task<Guid?> EnsureGlobalRoleAsync(string connectionString, string code) {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var existing = await ScalarAsync(connection, "SELECT id FROM valorapesquisa.roles WHERE code=@c AND organization_id IS NULL AND deleted_at IS NULL LIMIT 1", ("c", code));
        if (existing is { } role) return (Guid)role;
        var roleId = Guid.NewGuid();
        await ExecAsync(connection, "INSERT INTO valorapesquisa.roles(id,code,name,is_system) VALUES(@id,@c,@n,true)", ("id", roleId), ("c", code), ("n", "Papel de contrato " + code));
        return roleId;
    }

    [Fact]
    public async Task User_administration_list_is_scoped_to_each_organization_and_cross_org_get_is_null() {
        var connectionString = Connection();
        var factory = new TestConnectionFactory(connectionString);
        var orgRepo = new OrganizationRepository(factory, NullLogger<OrganizationRepository>.Instance);
        var adminRepo = new UserAdministrationRepository(factory);
        var service = new UserAdministrationService(adminRepo);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var orgA = await orgRepo.CreateAsync($"EVO AdminHub A {suffix}", $"ah-a-{suffix}@evo.test.local", $"ah-a-{suffix}", "free");
        var orgB = await orgRepo.CreateAsync($"EVO AdminHub B {suffix}", $"ah-b-{suffix}@evo.test.local", $"ah-b-{suffix}", "free");
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var hash = new BCryptPasswordHasher().Hash("SenhaEvo#1");
        try {
            await using var setup = new NpgsqlConnection(connectionString);
            await setup.OpenAsync();
            await ExecAsync(setup, "INSERT INTO valorapesquisa.users(id,organization_id,name,email,password_hash) VALUES(@id,@o,'Usuário A',@e,@h)", ("id", userA), ("o", orgA), ("e", $"user-a-{suffix}@evo.test.local"), ("h", hash));
            await ExecAsync(setup, "INSERT INTO valorapesquisa.users(id,organization_id,name,email,password_hash) VALUES(@id,@o,'Usuário B',@e,@h)", ("id", userB), ("o", orgB), ("e", $"user-b-{suffix}@evo.test.local"), ("h", hash));

            var listA = await service.ListAsync(orgA, new UserListQuery());
            var listB = await service.ListAsync(orgB, new UserListQuery());
            Assert.Equal(1, listA.TotalItems);
            Assert.Equal(1, listB.TotalItems);
            Assert.Contains(listA.Items, item => item.Id == userA);
            Assert.DoesNotContain(listA.Items, item => item.Id == userB);
            Assert.Contains(listB.Items, item => item.Id == userB);
            Assert.DoesNotContain(listB.Items, item => item.Id == userA);
            Assert.Null(await adminRepo.GetAsync(orgA, userB));
        }
        finally {
            await CleanupOrganizationAsync(connectionString, orgA, orgB);
        }
    }

    [Fact]
    public async Task Last_administrator_cannot_be_deactivated_or_demoted_by_role_removal() {
        var connectionString = Connection();
        var factory = new TestConnectionFactory(connectionString);
        var orgRepo = new OrganizationRepository(factory, NullLogger<OrganizationRepository>.Instance);
        var userRepo = new UserRepository(factory, NullLogger<UserRepository>.Instance);
        var service = new UserAdministrationService(new UserAdministrationRepository(factory));
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var org = await orgRepo.CreateAsync($"EVO LastAdmin {suffix}", $"lastadmin-{suffix}@evo.test.local", $"lastadmin-{suffix}", "free");
        Guid? createdGlobalRole = null;
        try {
            await using (var probe = new NpgsqlConnection(connectionString)) {
                await probe.OpenAsync();
                var present = await ScalarAsync(probe, "SELECT 1 FROM valorapesquisa.roles WHERE code='empresa_admin' AND organization_id IS NULL AND deleted_at IS NULL LIMIT 1");
                if (present is null) {
                    createdGlobalRole = Guid.NewGuid();
                    await ExecAsync(probe, "INSERT INTO valorapesquisa.roles(id,code,name,is_system) VALUES(@id,'empresa_admin','Papel de teste',true)", ("id", createdGlobalRole.Value));
                }
            }

            var hash = new BCryptPasswordHasher().Hash("SenhaEvo#1");
            var adminA = await userRepo.CreateAsync(org, "Administrador A", $"admin-a-{suffix}@evo.test.local", hash, "empresa_admin");
            var adminB = await userRepo.CreateAsync(org, "Administrador B", $"admin-b-{suffix}@evo.test.local", hash, "empresa_admin");
            var actor = Guid.NewGuid();

            await service.UpdateStatusAsync(org, actor, adminB, new UpdateUserStatusRequest("inactive"));
            await Assert.ThrowsAsync<BusinessRuleAppException>(() => service.UpdateStatusAsync(org, actor, adminA, new UpdateUserStatusRequest("suspended")));
            await service.UpdateStatusAsync(org, actor, adminA, new UpdateUserStatusRequest("active"));
            await Assert.ThrowsAsync<BusinessRuleAppException>(() => service.SetRolesAsync(org, actor, adminA, new UpdateUserRolesRequest([])));
            await service.SetRolesAsync(org, actor, adminB, new UpdateUserRolesRequest(["empresa_admin"]));
            await Assert.ThrowsAsync<BusinessRuleAppException>(() => service.SetRolesAsync(org, actor, adminB, new UpdateUserRolesRequest([])));
            await service.SetRolesAsync(org, actor, adminA, new UpdateUserRolesRequest(["empresa_admin"]));
        }
        finally {
            await CleanupOrganizationAsync(connectionString, org);
            if (createdGlobalRole is { } roleId) {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await ExecAsync(connection, "DELETE FROM valorapesquisa.roles WHERE id=@id AND organization_id IS NULL", ("id", roleId));
            }
        }
    }

    [Fact]
    public async Task User_create_prefers_global_roles_rejects_unknown_roles_and_duplicate_emails() {
        var connectionString = Connection();
        var factory = new TestConnectionFactory(connectionString);
        var orgRepo = new OrganizationRepository(factory, NullLogger<OrganizationRepository>.Instance);
        var userRepo = new UserRepository(factory, NullLogger<UserRepository>.Instance);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var org = await orgRepo.CreateAsync($"EVO RolePref {suffix}", $"rolepref-{suffix}@evo.test.local", $"rolepref-{suffix}", "free");
        var globalRoleId = Guid.NewGuid();
        var orgRoleId = Guid.NewGuid();
        var roleCode = $"ah-pref-{suffix}";
        try {
            await using var setup = new NpgsqlConnection(connectionString);
            await setup.OpenAsync();
            await ExecAsync(setup, "INSERT INTO valorapesquisa.roles(id,organization_id,code,name,is_system) VALUES(@g,NULL,@c,'Pref Global',false),(@o,@org,@c,'Pref Local',false)", ("g", globalRoleId), ("o", orgRoleId), ("c", roleCode), ("org", org));

            var userId = await userRepo.CreateAsync(org, "Usuário Pref", $"pref-{suffix}@evo.test.local", new BCryptPasswordHasher().Hash("SenhaEvo#1"), roleCode);
            var assigned = (Guid)(await ScalarAsync(setup, "SELECT role_id FROM valorapesquisa.user_roles WHERE user_id=@u", ("u", userId)))!;
            Assert.Equal(globalRoleId, assigned);

            var unknown = await Record.ExceptionAsync(() => userRepo.CreateAsync(org, "Usuário Sem Papel", $"unknown-{suffix}@evo.test.local", "hash", $"ah-missing-{suffix}"));
            Assert.IsType<InvalidOperationException>(unknown);

            var duplicate = await Record.ExceptionAsync(() => userRepo.CreateAsync(org, "Usuário Pref", $"pref-{suffix}@evo.test.local", "hash", roleCode));
            var conflict = Assert.IsType<PostgresException>(duplicate);
            Assert.Equal("23505", conflict.SqlState);
        }
        finally {
            await CleanupOrganizationAsync(connectionString, org);
            await using var cleanup = new NpgsqlConnection(connectionString);
            await cleanup.OpenAsync();
            await ExecAsync(cleanup, "DELETE FROM valorapesquisa.roles WHERE id=@g OR id=@o", ("g", globalRoleId), ("o", orgRoleId));
        }
    }

    [Fact]
    public async Task Organization_update_uses_expected_version_compare_and_swap_and_rejects_stale_writers() {
        var connectionString = Connection();
        var factory = new TestConnectionFactory(connectionString);
        var orgRepo = new OrganizationRepository(factory, NullLogger<OrganizationRepository>.Instance);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var org = await orgRepo.CreateAsync($"EVO CAS {suffix}", $"cas-{suffix}@evo.test.local", $"cas-{suffix}", "free");
        try {
            var before = await orgRepo.GetAsync(org);
            Assert.NotNull(before);
            var first = await orgRepo.UpdateCurrentAsync(org, new UpdateOrganizationRequest("Nome Público EVO", null, ExpectedVersion: before!.Version));
            Assert.NotNull(first);
            var stale = await orgRepo.UpdateCurrentAsync(org, new UpdateOrganizationRequest("Outro Nome EVO", null, ExpectedVersion: before.Version));
            Assert.Null(stale);
            var after = await orgRepo.GetAsync(org);
            Assert.Equal("Nome Público EVO", after!.PublicName);
            Assert.Equal(before.Version + 1, after.Version);
        }
        finally {
            await CleanupOrganizationAsync(connectionString, org);
        }
    }

    [Fact]
    public async Task Company_registration_is_idempotent_per_key_and_rejects_conflicting_payloads() {
        var connectionString = Connection();
        var factory = new TestConnectionFactory(connectionString);
        var handler = new RegisterCompanyHandler(new DbTransactionFactory(factory), new CompanyRegistrationRepository(), new BCryptPasswordHasher(), new RegisterCompanyValidator());
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var key = Guid.NewGuid().ToString("N");
        string? createdOrganizationId = null;
        try {
            RegisterCompanyRequest Build(string companyName) => new(
                Cnpj: "11222333000181",
                CompanyName: companyName,
                TradeName: null,
                AdministratorName: "Administrador Replay",
                AdministratorEmail: $"replay-{suffix}@evo.test.local",
                Password: "ValoraEvo#2026",
                Phone: "",
                Language: "pt-BR",
                TimeZone: "America/Sao_Paulo",
                AcceptedTerms: true,
                AcceptedPrivacyPolicy: true,
                IdempotencyKey: key,
                PlanCode: "free",
                RoleTitle: null);

            var first = await handler.HandleAsync(Build($"EVO Replay {suffix}"), "127.0.0.1");
            createdOrganizationId = first.OrganizationId.ToString();
            Assert.False(first.Replayed);

            var second = await handler.HandleAsync(Build($"EVO Replay {suffix}"), "127.0.0.1");
            Assert.True(second.Replayed);
            Assert.Equal(first.OrganizationId, second.OrganizationId);
            Assert.Equal(first.UserId, second.UserId);

            var conflicting = await Record.ExceptionAsync(() => handler.HandleAsync(Build($"EVO Replay Mudada {suffix}"), "127.0.0.1"));
            Assert.IsType<InvalidOperationException>(conflicting);
        }
        finally {
            await using var cleanup = new NpgsqlConnection(connectionString);
            await cleanup.OpenAsync();
            await ExecAsync(cleanup, "DELETE FROM valorapesquisa.idempotency_keys WHERE key=@k", ("k", key));
            if (createdOrganizationId is { } id)
                await CleanupOrganizationAsync(connectionString, Guid.Parse(id));
        }
    }
}
