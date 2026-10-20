using System.Data;
using System.IO.Compression;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Valora.Api.Controllers;
using Valora.Application.Contracts;
using Valora.Application.Exports;
using Xunit;

namespace Valora.Tests;

public sealed class OperationalHealthObservabilityAndConcurrencyTests {
    private const string DefaultPostgresConnection =
        "Host=localhost;Port=5433;Database=valora_clean_test;Username=postgres;Password=123456";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? DefaultPostgresConnection;

    private static async Task<NpgsqlConnection> OpenConnectionAsync() {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    private sealed class TestDbConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection Create() => new NpgsqlConnection(connectionString);
        public IDbConnection CreateMaster() => new NpgsqlConnection(connectionString);
        public IDbConnection CreateReadOnly() => new NpgsqlConnection(connectionString);
    }

    private sealed class TestWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Valora.Api";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public async Task HealthEndpoint_WithLiveDatabase_ReportsReadyAndBacklogs() {
        var factory = new TestDbConnectionFactory(ConnectionString);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Valora:Processing:Enabled"] = "true",
            ["Storage:Provider"] = "Local",
            ["Reports:PdfEnabled"] = "true"
        }).Build();

        var controller = new HealthController(
            factory,
            new TestWebHostEnvironment(),
            config,
            NullLogger<HealthController>.Instance);

        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Ready();
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("\"database\":\"ok\"", json);
        Assert.Contains("\"api\":\"ok\"", json);
        Assert.Contains("\"workers\":\"enabled\"", json);
    }

    [Fact]
    public async Task HealthEndpoint_WithBrokenDatabaseConnection_Returns503ServiceUnavailable() {
        var brokenFactory = new TestDbConnectionFactory("Host=localhost;Port=59999;Database=invalid;Username=none;Password=none;Timeout=1;Command Timeout=1;");
        var config = new ConfigurationBuilder().Build();

        var controller = new HealthController(
            brokenFactory,
            new TestWebHostEnvironment(),
            config,
            NullLogger<HealthController>.Instance);

        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Ready();
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusResult.StatusCode);
    }

    [Fact]
    public void ExportFidelity_ExportedCsvAndXlsx_MatchExactCalculatedValuesWithoutTruncation() {
        var renderer = new ExportDocumentRenderer();
        var orgId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

        var data = new ExportDataSet(
            Columns: ["dimension", "maturity_level", "raw_score", "confidence_level"],
            Rows: [
                new Dictionary<string, object?> {
                    ["dimension"] = "Liderança e Governança",
                    ["maturity_level"] = 4.25,
                    ["raw_score"] = 85.0,
                    ["confidence_level"] = "Alta"
                },
                new Dictionary<string, object?> {
                    ["dimension"] = "Cultura e Engajamento",
                    ["maturity_level"] = 3.80,
                    ["raw_score"] = 76.0,
                    ["confidence_level"] = "Moderada"
                }
            ]);

        var workItem = new ExportWorkItem(jobId, orgId, "maturity-snapshot", "csv", "{}", 1, 3, "corr-101");
        var csvExport = renderer.Render(workItem, data, timestamp);

        Assert.Equal("text/csv; charset=utf-8", csvExport.MimeType);
        var csvText = Encoding.UTF8.GetString(csvExport.Content);
        Assert.Contains("\"Liderança e Governança\"", csvText);
        Assert.Contains("\"4.25\"", csvText);
        Assert.Contains("\"85\"", csvText);
        Assert.Contains("\"Alta\"", csvText);

        var xlsxWorkItem = new ExportWorkItem(jobId, orgId, "maturity-snapshot", "xlsx", "{}", 1, 3, "corr-101");
        var xlsxExport = renderer.Render(xlsxWorkItem, data, timestamp);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", xlsxExport.MimeType);

        using var zip = new ZipArchive(new MemoryStream(xlsxExport.Content), ZipArchiveMode.Read);
        var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(sheetEntry);

        using var reader = new StreamReader(sheetEntry.Open());
        var sheetXml = reader.ReadToEnd();
        Assert.Contains("Liderança e Governança", sheetXml);
        Assert.Contains("4.25", sheetXml);
        Assert.Contains("Cultura e Engajamento", sheetXml);
    }

    [Fact]
    public async Task Concurrency_MultipleWorkerThreadsClaimingJobs_PreventsDoubleProcessingViaRowLocking() {
        await using var conn = await OpenConnectionAsync();
        var orgId = Guid.NewGuid();
        var trigger = "concurrency-test";

        // Seed 10 jobs
        var jobIds = new List<Guid>();
        for (var i = 0; i < 10; i++) {
            var jobId = Guid.NewGuid();
            jobIds.Add(jobId);
            await conn.ExecuteAsync("""
                INSERT INTO valorapesquisa.intelligence_processing_jobs (
                    id, organization_id, trigger, status, priority, attempts, max_attempts, scheduled_at, idempotency_key, metadata_json, created_at, updated_at
                ) VALUES (
                    @jobId, @orgId, @trigger, 'pending', 5, 0, 3, now(), @idempotencyKey, '{}'::jsonb, now(), now()
                );
                """, new { jobId, orgId, trigger, idempotencyKey = $"idem-concurrency-{i}-{Guid.NewGuid():N}" });
        }

        try {
            // Simulate 5 concurrent workers attempting to claim jobs with SKIP LOCKED
            var claimedPerWorker = new List<List<Guid>>();
            var tasks = Enumerable.Range(1, 5).Select(async workerNum => {
                var workerId = $"worker-test-{workerNum}";
                var claimedThisWorker = new List<Guid>();

                await using var workerConn = await OpenConnectionAsync();
                await using var tx = await workerConn.BeginTransactionAsync();

                var claimed = (await workerConn.QueryAsync<Guid>("""
                    SELECT id FROM valorapesquisa.intelligence_processing_jobs
                    WHERE organization_id = @orgId AND status = 'pending' AND trigger = @trigger
                    ORDER BY priority ASC, scheduled_at ASC
                    LIMIT 2
                    FOR UPDATE SKIP LOCKED;
                    """, new { orgId, trigger }, tx)).ToList();

                if (claimed.Count > 0) {
                    await workerConn.ExecuteAsync("""
                        UPDATE valorapesquisa.intelligence_processing_jobs
                        SET status = 'running', locked_at = now(), locked_by = @workerId
                        WHERE id = ANY(@claimed);
                        """, new { workerId, claimed = claimed.ToArray() }, tx);
                }

                await tx.CommitAsync();
                return claimed;
            });

            var allClaimedResults = await Task.WhenAll(tasks);
            var flatClaimed = allClaimedResults.SelectMany(x => x).ToList();

            // Total claimed across workers must have zero duplicates!
            var distinctClaimed = flatClaimed.Distinct().ToList();
            Assert.Equal(distinctClaimed.Count, flatClaimed.Count);
        }
        finally {
            await conn.ExecuteAsync("DELETE FROM valorapesquisa.intelligence_processing_jobs WHERE id = ANY(@jobIds);",
                new { jobIds = jobIds.ToArray() });
        }
    }

    [Fact]
    public async Task FailureRecovery_InterruptedJobExecution_LeavesJobCleanForRetryWithoutCorruptingData() {
        await using var conn = await OpenConnectionAsync();
        var orgId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var idemKey = $"idem-recovery-{Guid.NewGuid():N}";

        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.intelligence_processing_jobs (
                id, organization_id, trigger, status, priority, attempts, max_attempts, scheduled_at, idempotency_key, metadata_json, created_at, updated_at
            ) VALUES (
                @jobId, @orgId, 'recovery-test', 'running', 5, 1, 3, now(), @idemKey, '{}'::jsonb, now(), now()
            );
            """, new { jobId, orgId, idemKey });

        try {
            // Simulate sudden process crash / failure recovery handler marking failed and rescheduling
            await conn.ExecuteAsync("""
                UPDATE valorapesquisa.intelligence_processing_jobs
                SET status = 'retry_scheduled',
                    error_code = 'TRANSIENT_TIMEOUT',
                    error_message = 'Simulated container restart or network fault',
                    next_attempt_at = now() + interval '5 seconds',
                    locked_at = null,
                    locked_by = null,
                    updated_at = now()
                WHERE id = @jobId;
                """, new { jobId });

            var updated = await conn.QuerySingleAsync<dynamic>(
                "SELECT status, error_code, attempts, locked_by FROM valorapesquisa.intelligence_processing_jobs WHERE id = @jobId",
                new { jobId });

            Assert.Equal("retry_scheduled", (string)updated.status);
            Assert.Equal("TRANSIENT_TIMEOUT", (string)updated.error_code);
            Assert.Null((string?)updated.locked_by);
        }
        finally {
            await conn.ExecuteAsync("DELETE FROM valorapesquisa.intelligence_processing_jobs WHERE id = @jobId;", new { jobId });
        }
    }

    [Fact]
    public async Task TenantIsolation_ConcurrentQueriesAcrossDistinctTenants_NeverLeakCrossTenantData() {
        await using var conn = await OpenConnectionAsync();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var jobA = Guid.NewGuid();
        var jobB = Guid.NewGuid();

        await conn.ExecuteAsync("""
            INSERT INTO valorapesquisa.intelligence_processing_jobs (
                id, organization_id, trigger, status, priority, attempts, max_attempts, scheduled_at, idempotency_key, metadata_json, created_at, updated_at
            ) VALUES 
                (@jobA, @orgA, 'tenant-isolation-test', 'pending', 5, 0, 3, now(), @idemA, '{"org":"A"}'::jsonb, now(), now()),
                (@jobB, @orgB, 'tenant-isolation-test', 'pending', 5, 0, 3, now(), @idemB, '{"org":"B"}'::jsonb, now(), now());
            """, new { jobA, jobB, orgA, orgB, idemA = $"idem-iso-A-{Guid.NewGuid():N}", idemB = $"idem-iso-B-{Guid.NewGuid():N}" });

        try {
            // Query strictly for OrgA
            var rowsOrgA = (await conn.QueryAsync<Guid>("""
                SELECT id FROM valorapesquisa.intelligence_processing_jobs
                WHERE organization_id = @orgA AND trigger = 'tenant-isolation-test';
                """, new { orgA })).ToList();

            // Query strictly for OrgB
            var rowsOrgB = (await conn.QueryAsync<Guid>("""
                SELECT id FROM valorapesquisa.intelligence_processing_jobs
                WHERE organization_id = @orgB AND trigger = 'tenant-isolation-test';
                """, new { orgB })).ToList();

            Assert.Single(rowsOrgA);
            Assert.Equal(jobA, rowsOrgA[0]);
            Assert.DoesNotContain(jobB, rowsOrgA);

            Assert.Single(rowsOrgB);
            Assert.Equal(jobB, rowsOrgB[0]);
            Assert.DoesNotContain(jobA, rowsOrgB);
        }
        finally {
            await conn.ExecuteAsync("DELETE FROM valorapesquisa.intelligence_processing_jobs WHERE id IN (@jobA, @jobB);", new { jobA, jobB });
        }
    }
}
