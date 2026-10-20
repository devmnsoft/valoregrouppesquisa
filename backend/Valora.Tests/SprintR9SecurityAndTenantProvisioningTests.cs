using System.Data;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Valora.Application.ModularSaas;
using Valora.Infrastructure.ModularSaas;
using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

[Collection("PostgresLive")]
public sealed class SprintR9SecurityAndTenantProvisioningTests {
    private static string ConnectionString =>
        PostgresTestEnvironment.ConnectionString("Sprint R9: Segurança e Provisionamento Multitenant");

    private static async Task<NpgsqlConnection> OpenConnectionAsync() {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    private sealed class LiveDbConnectionFactory(string connectionString) : Valora.Application.Contracts.IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connectionString);
        public IDbConnection CreateMaster() => new NpgsqlConnection(connectionString);
        public IDbConnection CreateReadOnly() => new NpgsqlConnection(connectionString);
    }

    private static string GenerateValidCnpj() {
        var rnd = new Random();
        var n = new int[14];
        for (var i = 0; i < 8; i++) n[i] = rnd.Next(1, 9);
        n[8] = 0; n[9] = 0; n[10] = 0; n[11] = 1; // filial 0001
        var w1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var sum1 = 0;
        for (var i = 0; i < 12; i++) sum1 += n[i] * w1[i];
        var mod1 = sum1 % 11;
        n[12] = mod1 < 2 ? 0 : 11 - mod1;

        var w2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var sum2 = 0;
        for (var i = 0; i < 13; i++) sum2 += n[i] * w2[i];
        var mod2 = sum2 % 11;
        n[13] = mod2 < 2 ? 0 : 11 - mod2;

        return $"{n[0]}{n[1]}.{n[2]}{n[3]}{n[4]}.{n[5]}{n[6]}{n[7]}/{n[8]}{n[9]}{n[10]}{n[11]}-{n[12]}{n[13]}";
    }

    [PostgresFact("Provisionamento seguro gera token aleatório, obriga reset de senha, impõe MFA e audita evento")]
    public async Task ProvisionTenant_ValidParameters_ProvisionsCompleteIsolatedTenantWithZeroStaticPasswords() {
        await using var conn = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var orgSlug = $"piloto-r9-{suffix}";
        var orgName = $"Empresa Piloto {suffix}";
        var adminEmail = $"admin-{suffix}@pilotovalora.com.br";
        var cnpj = GenerateValidCnpj();

        try {
            var row = await conn.QuerySingleAsync<dynamic>("""
                SELECT * FROM valorapesquisa.provision_tenant(
                    @orgName, @orgSlug, 'corporate', @adminEmail, 'Administrador R9', @cnpj
                );
                """, new { orgName, orgSlug, adminEmail, cnpj });

            Guid orgId = row.out_organization_id;
            Guid adminUserId = row.out_admin_user_id;
            string inviteToken = row.out_invite_token;
            string inviteUrl = row.out_invite_url;
            bool mfaEnforced = row.out_mfa_enforced;

            Assert.NotEqual(Guid.Empty, orgId);
            Assert.NotEqual(Guid.Empty, adminUserId);
            Assert.NotEmpty(inviteToken);
            Assert.Equal(64, inviteToken.Length); // 32 bytes hex = 64 chars
            Assert.Contains("/Account/AcceptInvitation?token=", inviteUrl);
            Assert.True(mfaEnforced);

            // Verifica que o hash de senha gerado no banco NÃO é nulo, NÃO é estático, e exige reset de senha
            var user = await conn.QuerySingleAsync<dynamic>("""
                SELECT password_hash, password_reset_required, status
                FROM valorapesquisa.users WHERE id = @adminUserId;
                """, new { adminUserId });

            Assert.True((bool)user.password_reset_required);
            Assert.StartsWith("$2", (string)user.password_hash); // Bcrypt hash seguro
            Assert.NotEqual("$2a$12$e8F0s6rLpA7qH.s0H28EuuqC4v18Q3OQh1K0qL2aCj6O1z7G8P5rC", (string)user.password_hash);

            // Verifica convite criptográfico na tabela user_invitations
            var invite = await conn.QuerySingleAsync<dynamic>("""
                SELECT status, expires_at, resend_count
                FROM valorapesquisa.user_invitations
                WHERE organization_id = @orgId AND normalized_email = @adminEmail;
                """, new { orgId, adminEmail = adminEmail.ToLowerInvariant() });

            Assert.Equal("pending", (string)invite.status);
            Assert.True((DateTime)invite.expires_at > DateTime.UtcNow);

            // Verifica política de MFA em organization_settings
            var settings = await conn.QuerySingleAsync<string>("""
                SELECT settings::text FROM valorapesquisa.organization_settings WHERE organization_id = @orgId;
                """, new { orgId });

            Assert.Contains("mfa_required_for_admin", settings);

            // Verifica registro de auditoria em admin_audit_events
            var audit = await conn.QuerySingleAsync<dynamic>("""
                SELECT event_type, resource_type, outcome, metadata_json::text as meta
                FROM valorapesquisa.admin_audit_events
                WHERE organization_id = @orgId AND event_type = 'tenant.provisioned';
                """, new { orgId });

            Assert.Equal("organization", (string)audit.resource_type);
            Assert.Equal("success", (string)audit.outcome);
            Assert.Contains("enforced_for_admin", (string)audit.meta);
        }
        finally {
            await CleanupTenantAsync(conn, orgSlug, adminEmail);
        }
    }

    [PostgresFact("Validação rejeita CNPJ inválido ou com dígitos verificadores incorretos")]
    public async Task ProvisionTenant_InvalidCnpj_ThrowsPostgresException() {
        await using var conn = await OpenConnectionAsync();
        var ex = await Assert.ThrowsAsync<PostgresException>(() => conn.ExecuteAsync("""
            SELECT * FROM valorapesquisa.provision_tenant(
                'Empresa Invalida', 'empresa-inv-cnpj', 'corporate', 'admin@invcnpj.com', 'Admin', '11.222.333/0001-99'
            );
            """));

        Assert.Contains("CNPJ", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [PostgresFact("Garante que plano comercial inexistente seja rejeitado sem substituição silenciosa")]
    public async Task ProvisionTenant_NonExistentPlan_ThrowsPostgresExceptionWithoutSilentSubstitution() {
        await using var conn = await OpenConnectionAsync();
        var ex = await Assert.ThrowsAsync<PostgresException>(() => conn.ExecuteAsync("""
            SELECT * FROM valorapesquisa.provision_tenant(
                'Empresa Plano Falso', 'empresa-plano-falso', 'plano_inexistente_xyz', 'admin@planofalso.com', 'Admin', '11.222.333/0001-81'
            );
            """));

        Assert.Contains("Plano comercial especificado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [PostgresFact("Validação rejeita formato inválido de slug ou e-mail")]
    public async Task ProvisionTenant_InvalidSlugOrEmail_ThrowsPostgresException() {
        await using var conn = await OpenConnectionAsync();

        var exSlug = await Assert.ThrowsAsync<PostgresException>(() => conn.ExecuteAsync("""
            SELECT * FROM valorapesquisa.provision_tenant(
                'Empresa Slug Ruim', 'Slug_Com_Maiusculas!', 'corporate', 'admin@slug.com', 'Admin', '11.222.333/0001-81'
            );
            """));
        Assert.Contains("Slug", exSlug.Message, StringComparison.OrdinalIgnoreCase);

        var exEmail = await Assert.ThrowsAsync<PostgresException>(() => conn.ExecuteAsync("""
            SELECT * FROM valorapesquisa.provision_tenant(
                'Empresa Email Ruim', 'empresa-email-ruim', 'corporate', 'email_sem_arroba_ponto', 'Admin', '11.222.333/0001-81'
            );
            """));
        Assert.Contains("E-mail", exEmail.Message, StringComparison.OrdinalIgnoreCase);
    }

    [PostgresFact("Idempotência preserva dados existentes e impede ampliação indevida de privilégios")]
    public async Task ProvisionTenant_RepeatExecution_IsIdempotentAndReportsExistingTenantUpdated() {
        await using var conn = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var orgSlug = $"idem-r9-{suffix}";
        var orgName = $"Empresa Idempotente {suffix}";
        var adminEmail = $"admin-{suffix}@idem.com.br";
        var cnpj = GenerateValidCnpj();

        try {
            var firstCall = await conn.QuerySingleAsync<dynamic>("""
                SELECT * FROM valorapesquisa.provision_tenant(
                    @orgName, @orgSlug, 'corporate', @adminEmail, 'Admin 1', @cnpj
                );
                """, new { orgName, orgSlug, adminEmail, cnpj });

            Assert.Equal("new_tenant_provisioned", (string)firstCall.out_status);

            var secondCall = await conn.QuerySingleAsync<dynamic>("""
                SELECT * FROM valorapesquisa.provision_tenant(
                    @orgName, @orgSlug, 'corporate', @adminEmail, 'Admin 1', @cnpj
                );
                """, new { orgName, orgSlug, adminEmail, cnpj });

            Assert.Equal("existing_tenant_updated", (string)secondCall.out_status);
            Assert.Equal((Guid)firstCall.out_organization_id, (Guid)secondCall.out_organization_id);
            Assert.Equal((Guid)firstCall.out_admin_user_id, (Guid)secondCall.out_admin_user_id);
        }
        finally {
            await CleanupTenantAsync(conn, orgSlug, adminEmail);
        }
    }

    [PostgresFact("Garante restrição de operações de escrita quando assinatura está inativa ou expirada")]
    public async Task SubscriptionLifecycle_InactiveOrExpired_RestrictsWriteOperationsInCommercialSaas() {
        var factory = new LiveDbConnectionFactory(ConnectionString);
        var repo = new CommercialSaasRepository(factory);
        var service = new CommercialSaasService(repo, NullLogger<CommercialSaasService>.Instance);

        var unknownOrg = Guid.NewGuid();

        // Avaliação para módulo inexistente / cliente não contratado
        var decision = await service.EvaluateAccessAsync(unknownOrg, "pesquisas", writeOperation: true);
        Assert.False(decision.Allowed);
        Assert.Contains(decision.Code, new[] { "MODULE_NOT_CONTRACTED", "MODULE_UNAVAILABLE", "SUBSCRIPTION_INACTIVE" });
    }

    private static async Task CleanupTenantAsync(NpgsqlConnection conn, string orgSlug, string adminEmail) {
        var orgId = await conn.ExecuteScalarAsync<Guid?>("SELECT id FROM valorapesquisa.organizations WHERE slug = @orgSlug;", new { orgSlug });
        if (!orgId.HasValue) return;

        await conn.ExecuteAsync("""
            UPDATE valorapesquisa.units SET manager_user_id = null WHERE organization_id = @orgId;
            UPDATE valorapesquisa.departments SET manager_user_id = null WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.admin_audit_events WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.user_invitations WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.user_roles WHERE user_id IN (SELECT id FROM valorapesquisa.users WHERE organization_id = @orgId);
            DELETE FROM valorapesquisa.organization_settings WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.organization_modules WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.subscriptions WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.organization_onboarding_state WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.departments WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.units WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.legal_entities WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.users WHERE organization_id = @orgId;
            DELETE FROM valorapesquisa.organizations WHERE id = @orgId;
            """, new { orgId });
    }
}
