using Valora.Application.Access;
using Valora.Application.Communication;

namespace Valora.Tests;

public sealed class CommunicationCenterContractTests
{
    [Fact]
    public void Communication_permissions_are_canonical_and_unique()
    {
        var required = new[]
        {
            "notifications.read", "notifications.manage", "notifications.mark_read",
            "communication.read", "communication.manage", "communication.templates.read",
            "communication.templates.manage", "communication.outbox.read", "communication.outbox.manage",
            "communication.reminders.read", "communication.reminders.manage"
        };

        Assert.All(required, permission => Assert.True(ValoraAccessCatalog.IsCanonicalPermission(permission), permission));
        Assert.Equal(ValoraPermissions.All.Count, ValoraPermissions.All.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Template_validation_rejects_variables_outside_allowlist()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            EmailTemplateService.ValidatePlaceholders("Olá {{name}}, token {{secret}}", ["name"]));

        Assert.Contains("secret", error.Message);
    }

    [Fact]
    public void Complete_script_guards_communication_tables_and_operational_columns()
    {
        var sql = File.ReadAllText("../../../database/postgresql/script_completo.sql");
        foreach (var table in new[] { "notification_recipients", "notification_templates", "notification_events", "communication_outbox", "communication_delivery_attempts", "email_template_versions", "reminder_rules", "reminder_jobs", "message_audit_logs" })
            Assert.Contains($"CREATE TABLE IF NOT EXISTS valorapesquisa.{table}", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS read_at", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS scheduled_at", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS deleted_at", sql);
    }

    [Fact]
    public void Collaboration_center_migration_is_organization_scoped_and_complete()
    {
        var sql = File.ReadAllText("../../../database/postgresql/migrations/2026_08_communication_collaboration_center.sql");
        var tables = new[]
        {
            "communication_channels", "communication_batches", "communication_recipients", "communication_events",
            "notification_center_items", "collaboration_threads", "collaboration_comments", "collaboration_mentions",
            "approval_flows", "approval_flow_steps", "approval_requests", "approval_decisions", "reminder_events",
            "organization_announcements"
        };

        Assert.All(tables, table => Assert.Contains($"CREATE TABLE IF NOT EXISTS valorapesquisa.{table}", sql));
        Assert.Equal(tables.Length, sql.Split("organization_id uuid NOT NULL", StringSplitOptions.None).Length - 1);
        Assert.Contains("ck_approval_rejection_reason", sql);
        Assert.Contains("destination_hash", sql);
        Assert.Contains("invitation_token_hash", sql);
    }
}
