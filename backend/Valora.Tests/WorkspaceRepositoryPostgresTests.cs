using System.Data;
using Npgsql;
using Valora.Application.Contracts;
using Valora.Infrastructure.Repositories;

namespace Valora.Tests;

[Trait("Category", "DatabaseContract")]
public sealed class WorkspaceRepositoryPostgresTests {
    [Fact]
    public void My_day_binds_wide_and_controller_derives_scope_from_roles() {
        var repository = File.ReadAllText(Support.RepositoryPaths.InfrastructureFile("Repositories", "WorkspaceRepositories.cs"));
        var controller = File.ReadAllText(Support.RepositoryPaths.ApiFile("Controllers", "WorkspaceController.cs"));

        Assert.Contains("new { o, u, wide }, ct", repository, StringComparison.Ordinal);
        Assert.Contains("Query(string sql, object parameters, CancellationToken ct)", repository, StringComparison.Ordinal);
        Assert.Contains("User.IsInRole(\"admin_valora\") || User.IsInRole(\"admin_cliente\")", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[FromQuery] bool wide", controller, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Workspace_queries_enforce_visibility_tenant_and_idempotent_pins() {
        var connectionString = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Matches("(?i)(test|teste|homolog|qa)", builder.Database ?? "");

        var organization = Guid.NewGuid();
        var otherOrganization = Guid.NewGuid();
        var user = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var ownItem = Guid.NewGuid();
        var sharedItem = Guid.NewGuid();
        var thirdPartyItem = Guid.NewGuid();
        var otherTenantItem = Guid.NewGuid();

        await using var setup = new NpgsqlConnection(connectionString);
        await setup.OpenAsync();
        try {
            await using (var insert = new NpgsqlCommand("""
                INSERT INTO valorapesquisa.workspace_items
                    (id, organization_id, item_type, title, status, priority, due_at, owner_user_id)
                VALUES
                    (@own, @organization, 'approval', 'Own', 'pending', 'high', now(), @user),
                    (@shared, @organization, 'approval', 'Shared', 'pending', 'high', now(), NULL),
                    (@thirdParty, @organization, 'approval', 'Third party', 'pending', 'high', now(), @owner),
                    (@otherTenant, @otherOrganization, 'approval', 'Other tenant', 'pending', 'critical', now(), @owner)
                """, setup)) {
                insert.Parameters.AddWithValue("own", ownItem);
                insert.Parameters.AddWithValue("shared", sharedItem);
                insert.Parameters.AddWithValue("thirdParty", thirdPartyItem);
                insert.Parameters.AddWithValue("otherTenant", otherTenantItem);
                insert.Parameters.AddWithValue("organization", organization);
                insert.Parameters.AddWithValue("otherOrganization", otherOrganization);
                insert.Parameters.AddWithValue("user", user);
                insert.Parameters.AddWithValue("owner", owner);
                await insert.ExecuteNonQueryAsync();
            }

            await using (var recent = new NpgsqlCommand("INSERT INTO valorapesquisa.user_recent_items(organization_id,user_id,workspace_item_id) VALUES(@organization,@user,@own)", setup)) {
                recent.Parameters.AddWithValue("organization", organization);
                recent.Parameters.AddWithValue("user", user);
                recent.Parameters.AddWithValue("own", ownItem);
                await recent.ExecuteNonQueryAsync();
            }

            var repository = new WorkspaceRepository(new TestConnectionFactory(connectionString));
            var restricted = await repository.MyDayAsync(organization, user, false, CancellationToken.None);
            Assert.Equal(new[] { ownItem, sharedItem }.Order(), restricted.Select(item => item.Id).Order());

            var wide = await repository.MyDayAsync(organization, user, true, CancellationToken.None);
            Assert.Equal(new[] { ownItem, sharedItem, thirdPartyItem }.Order(), wide.Select(item => item.Id).Order());
            Assert.DoesNotContain(wide, item => item.Id == otherTenantItem);

            Assert.Single(await repository.RecentAsync(organization, user, CancellationToken.None));
            await repository.PinAsync(organization, user, ownItem, CancellationToken.None);
            await repository.PinAsync(organization, user, ownItem, CancellationToken.None);
            Assert.Single(await repository.PinnedAsync(organization, user, CancellationToken.None));
            await repository.UnpinAsync(organization, user, ownItem, CancellationToken.None);
            await repository.UnpinAsync(organization, user, ownItem, CancellationToken.None);
            Assert.Empty(await repository.PinnedAsync(organization, user, CancellationToken.None));
        }
        finally {
            await using var cleanup = new NpgsqlCommand("""
                DELETE FROM valorapesquisa.user_recent_items WHERE organization_id IN (@organization, @otherOrganization);
                DELETE FROM valorapesquisa.user_pinned_items WHERE organization_id IN (@organization, @otherOrganization);
                DELETE FROM valorapesquisa.workspace_items WHERE organization_id IN (@organization, @otherOrganization);
                """, setup);
            cleanup.Parameters.AddWithValue("organization", organization);
            cleanup.Parameters.AddWithValue("otherOrganization", otherOrganization);
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    private sealed class TestConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connectionString);
    }
}
