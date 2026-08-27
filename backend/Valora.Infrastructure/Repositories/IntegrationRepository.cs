using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Integrations;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;

public sealed class IntegrationRepository(IDbConnectionFactory connections) : IIntegrationRepository
{
    public async Task<AuthenticatedApiKey?> AuthenticateAsync(string hash, CancellationToken ct)
    {
        const string sql = """
            UPDATE valorapesquisa.api_keys SET last_used_at=now(),use_count=use_count+1,updated_at=now()
            WHERE key_hash=@hash AND status='active' AND revoked_at IS NULL AND deleted_at IS NULL
              AND (expires_at IS NULL OR expires_at>now())
            RETURNING id,organization_id OrganizationId,scopes;
            """;
        using var c = connections.Create();
        var row = await c.QuerySingleOrDefaultAsync<ApiKeyRow>(new CommandDefinition(sql, new { hash }, cancellationToken: ct));
        return row is null ? null : new(row.Id, row.OrganizationId, row.Scopes.ToHashSet(StringComparer.Ordinal));
    }

    public async Task RecordApiUseAsync(AuthenticatedApiKey? key, string prefix, string endpoint, int status, string? scope, string correlationId, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO valorapesquisa.integration_logs(organization_id,api_key_id,event_type,status,endpoint,scope_used,correlation_id,metadata)
            VALUES(@organizationId,@keyId,@eventType,@status,@endpoint,@scope,@correlationId,jsonb_build_object('key_prefix',@prefix))
            """;
        using var c = connections.Create();
        await c.ExecuteAsync(new CommandDefinition(sql, new { organizationId = key?.OrganizationId, keyId = key?.Id, eventType = key is null ? "api.authentication_failed" : "api.key_used", status, endpoint, scope, correlationId, prefix }, cancellationToken: ct));
    }

    public async Task<PublicDataResult?> PublicDataAsync(string resource, Guid id, CancellationToken ct)
    {
        var (table, projection) = resource switch
        {
            "organizations" => ("organizations", "jsonb_build_object('id',id,'name',name,'status',status,'createdAt',created_at)"),
            "diagnostics" => ("surveys", "jsonb_build_object('id',id,'title',title,'status',status,'createdAt',created_at)"),
            "reports" => ("reports", "jsonb_build_object('id',id,'status',status,'format',format,'createdAt',created_at)"),
            "benchmark" => ("benchmark_snapshots", "jsonb_build_object('id',id,'snapshot',data,'createdAt',created_at)"),
            "evolution" => ("organizations", "jsonb_build_object('organizationId',id,'dataset','evolution','generatedAt',now())"),
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };
        var organizationExpression = resource is "organizations" or "evolution" ? "id" : "organization_id";
        var sql = $"SELECT {organizationExpression} OrganizationId,{projection} Data FROM valorapesquisa.{table} WHERE id=@id AND deleted_at IS NULL";
        using var c = connections.Create();
        var row = await c.QuerySingleOrDefaultAsync<PublicRow>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
        return row is null ? null : new(row.OrganizationId, JsonSerializer.Deserialize<object>(row.Data)!);
    }

    public async Task<PublicDataResult?> CertificateAsync(string code, CancellationToken ct)
    {
        const string sql = "SELECT organization_id OrganizationId,jsonb_build_object('code',code,'status',status,'issuedAt',issued_at,'valid',revoked_at IS NULL) Data FROM valorapesquisa.certificates WHERE code=@code AND deleted_at IS NULL";
        using var c = connections.Create();
        var row = await c.QuerySingleOrDefaultAsync<PublicRow>(new CommandDefinition(sql, new { code }, cancellationToken: ct));
        return row is null ? null : new(row.OrganizationId, JsonSerializer.Deserialize<object>(row.Data)!);
    }

    public async Task<Guid> EnqueueEmailAsync(Guid organizationId, string template, string recipient, object payload, CancellationToken ct)
    {
        const string sql = "INSERT INTO valorapesquisa.email_outbox(organization_id,template_code,recipient,payload) VALUES(@organizationId,@template,@recipient,CAST(@payload AS jsonb)) RETURNING id";
        using var c = connections.Create();
        return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql, new { organizationId, template, recipient, payload = JsonSerializer.Serialize(payload) }, cancellationToken: ct));
    }

    public async Task<Guid> CreateImportAsync(Guid organizationId, string type, string format, string checksum, IReadOnlyList<ImportValidationRow> rows, CancellationToken ct)
    {
        const string sql = "INSERT INTO valorapesquisa.import_batches(organization_id,type,format,checksum,status,total_rows,valid_rows,error_rows) VALUES(@organizationId,@type,@format,@checksum,@status,@total,@valid,@invalid) RETURNING id";
        using var c = connections.Create();
        var invalid = rows.Count(x => x.Errors.Count != 0);
        return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql, new { organizationId, type, format, checksum, status = invalid == 0 ? "validated" : "invalid", total = rows.Count, valid = rows.Count - invalid, invalid }, cancellationToken: ct));
    }

    private sealed record ApiKeyRow(Guid Id, Guid OrganizationId, string[] Scopes);
    private sealed record PublicRow(Guid OrganizationId, string Data);
}
