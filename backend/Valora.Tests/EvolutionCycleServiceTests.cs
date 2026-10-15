using System.Text.Json;
using Valora.Application.Evolution;
using Valora.Application.OrganizationalIntelligence;
using Valora.Application.Common;
using Valora.Application.Access;
using Valora.Application.ModularSaas;

namespace Valora.Tests;

public sealed class EvolutionCycleServiceTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Every_measured_dimension_requires_preserved_scale_and_calculation_criteria(bool missingScale) {
        var reading = Reading() with {
            MeasuredDimensionCodes = ["people", "process"],
            SnapshotJson = missingScale ? Reading().SnapshotJson : """{"methodology":{"code":"professional","version":3},"questions":[{"dimension":"people","scale":{"min":1,"max":5}},{"dimension":"process","scale":{"min":1,"max":5}}]}""",
            CriteriaRows = missingScale ? [new("people", "trust", "maturity", "1", 1), new("process", "trust", "maturity", "1", 1)] : Reading().CriteriaRows
        };
        var measurement = EvolutionMeasurementBasis.Capture(reading);
        var comparison = EvolutionComparisonService.Compare(measurement.Score, measurement.Basis, measurement.Score, measurement.Basis);
        Assert.False(comparison.IsComparable);
        Assert.Null(comparison.AbsoluteVariation);
        Assert.Contains(missingScale ? "escala" : "critérios", comparison.Limitation);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"methodology\":{\"code\":\"professional\",\"version\":{}}}")]
    [InlineData("{\"methodology\":{\"code\":\"unknown\",\"version\":3}}")]
    public void Incomplete_snapshot_never_proves_comparability(string json) {
        var measurement = EvolutionMeasurementBasis.Capture(Reading() with { SnapshotJson = json });
        var comparison = EvolutionComparisonService.Compare(measurement.Score, measurement.Basis, measurement.Score, measurement.Basis);
        Assert.False(comparison.IsComparable);
        Assert.Null(comparison.AbsoluteVariation);
    }
    [Theory]
    [InlineData("{")]
    [InlineData("{\"methodology\":")]
    public async Task Malformed_metadata_does_not_remove_other_selection_options(string json) {
        var invalid = Reading() with { SnapshotJson = json };
        var service = new EvolutionCycleService(new CaptureEvolutionRepository(),
            new IntelligenceRepositoryStub([invalid, Reading() with { SurveyId = Guid.NewGuid() }]), new ContextStub(), new CommercialStub());
        var options = await service.ResultOptions(OrganizationId, null, default);
        Assert.Equal(2, options.Count);
        Assert.Contains("comparação está indisponível", options[0].Limitation);
        Assert.Equal("professional@3", options[1].MethodologyVersion);
        var preserved = EvolutionMeasurementBasis.Capture(invalid);
        Assert.False(preserved.Basis.DataAvailable);
        Assert.Equal(invalid.MaturityIndex, preserved.Score);
        Assert.Contains("metadados", preserved.Limitation);
        Assert.False(EvolutionComparisonService.Compare(preserved.Score, preserved.Basis, preserved.Score, preserved.Basis).IsComparable);
    }

    [Fact]
    public void Malformed_calculation_rule_preserves_score_without_comparability() {
        var reading = Reading() with { CriteriaRows = [new("people", "trust", "maturity", "1", 1) { CalculationRuleJson = "{" }] };
        var preserved = EvolutionMeasurementBasis.Capture(reading);
        Assert.False(preserved.Basis.DataAvailable);
        Assert.Equal(reading.MaturityIndex, preserved.Score);
    }
    private static readonly Guid OrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SurveyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task ReferenceReviewRejectsInvalidCommandsAndForeignOrganization() {
        var service = new EvolutionCycleService(new CaptureEvolutionRepository(), new IntelligenceRepositoryStub([]), new ContextStub(), new CommercialStub());
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReviewReference(OrganizationId, UserId, Guid.NewGuid(), 1, "", "Next step", Guid.NewGuid().ToString("N"), default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReviewReference(OrganizationId, UserId, Guid.NewGuid(), 1, "Evidence", "Next step", "invalid", default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ReviewReference(Guid.NewGuid(), UserId, Guid.NewGuid(), 1, "Evidence", "Next step", Guid.NewGuid().ToString("N"), default));
    }

    [Fact]
    public async Task OpenWithOrganizationalMeasurementUsesValidatedReadingInsteadOfPostedBaseline() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]), new ContextStub(), new CommercialStub());

        var id = await service.Open(OrganizationId, UserId, Request(SurveyId) with { BaselineScore = 1m }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.NotNull(repository.LastOpen);
        Assert.Equal(72.5m, repository.LastOpen!.BaselineScore);
        Assert.Equal("organizational_measurement", repository.LastOpen.BaselineSource);
        Assert.Equal("professional@3", repository.LastOpen.MethodologyVersion);
        Assert.Equal("population-a", repository.LastOpen.PopulationSignature);
        Assert.Equal("people", repository.LastOpen.DimensionSignature);
        Assert.Equal(SurveyId, repository.LastOpen.DiagnosticId);
        Assert.Null(repository.LastOpen.ResultId);
        Assert.NotEqual(SurveyId, repository.LastOpen.Measurement!.Id);
        Assert.Equal(SurveyId, repository.LastOpen.Measurement.SurveyId);
        Assert.Contains("Medição inicial selecionada", repository.LastOpen.EvidenceSummary);
        Assert.Contains("4 resposta(s) pontuada(s)", repository.LastOpen.EvidenceSummary);
    }

    [Fact]
    public async Task OpenRejectsMeasurementThatIsNotAvailableForOrganization() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]), new ContextStub(), new CommercialStub());

        await Assert.ThrowsAsync<ArgumentException>(() => service.Open(OrganizationId, UserId, Request(Guid.NewGuid()), CancellationToken.None));

        Assert.Null(repository.LastOpen);
    }

    [Fact]
    public async Task ManualCannotForgeCalculatedOriginOrBasis() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([]), new ContextStub(), new CommercialStub());
        await service.Open(OrganizationId, UserId, Request(SurveyId) with { SourceSurveyId = null, BaselineScore = 0, BaselineSource = "organizational_measurement", MethodologyVersion = "professional@3", Measurement = EvolutionMeasurementBasis.Capture(Reading()) }, default);
        Assert.Equal("manual_informed", repository.LastOpen!.BaselineSource);
        Assert.Null(repository.LastOpen.Measurement);
        Assert.Null(repository.LastOpen.MethodologyVersion);
        Assert.Equal(0, repository.LastOpen.BaselineScore);
    }

    [Fact]
    public async Task SelectorExcludesMissingScoreButPreservesCalculatedZero() {
        var service = new EvolutionCycleService(new CaptureEvolutionRepository(), new IntelligenceRepositoryStub([Reading() with { MaturityIndex = null }, Reading() with { SurveyId = Guid.NewGuid(), MaturityIndex = 0 }]), new ContextStub(), new CommercialStub());
        Assert.Equal(0, Assert.Single(await service.ResultOptions(OrganizationId, null, default)).Score);
    }

    [Fact]
    public async Task EvidenceLimitIncludesServerContext() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]), new ContextStub(), new CommercialStub());
        await Assert.ThrowsAsync<ArgumentException>(() => service.Open(OrganizationId, UserId, Request(SurveyId) with { EvidenceSummary = new string('x', 2000) }, default));
        Assert.Null(repository.LastOpen);
    }

    [Fact]
    public async Task UnavailableScoreCannotOpenCalculatedCycle() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading() with { MaturityIndex = null }]), new ContextStub(), new CommercialStub());
        await Assert.ThrowsAsync<ArgumentException>(() => service.Open(OrganizationId, UserId, Request(SurveyId), default));
        Assert.Null(repository.LastOpen);
    }

    [Fact]
    public async Task DirectCommandHonorsCommercialAccess() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]), new ContextStub(), new CommercialStub(false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.Open(OrganizationId, UserId, Request(SurveyId), default));
        Assert.Null(repository.LastOpen);
    }

    [Fact]
    public async Task SurveyIdCannotBeUsedAsAnIndividualResultReference() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]), new ContextStub(), new CommercialStub());
        await Assert.ThrowsAsync<ArgumentException>(() => service.Open(OrganizationId, UserId, Request(SurveyId) with { ResultId = SurveyId }, default));
        Assert.Null(repository.LastOpen);
    }

    [Fact]
    public async Task DifferentOrganizationCannotInvokeCommand() {
        var service = new EvolutionCycleService(new CaptureEvolutionRepository(), new IntelligenceRepositoryStub([Reading()]), new ContextStub(), new CommercialStub());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.Open(Guid.NewGuid(), UserId, Request(SurveyId), default));
    }

    [Fact]
    public void SignaturesIgnorePropertyAndCriterionOrderButPreserveScaleChanges() {
        var first = Reading() with { CriteriaRows = [new("people", "a", "maturity", "1.00", 1), new("people", "b", "maturity", "2", 1)] };
        var second = first with { SnapshotJson = "{\"questions\":[{\"scale\":{\"max\":5.0,\"min\":1},\"dimension\":\"people\"}],\"methodology\":{\"version\":3,\"code\":\"professional\"}}", CriteriaRows = first.CriteriaRows.Reverse().ToArray() };
        var basis = EvolutionMeasurementBasis.Capture(first).Basis;
        Assert.Equal(basis, EvolutionMeasurementBasis.Capture(second).Basis);
        Assert.False(EvolutionComparisonService.Compare(1, basis, 2, EvolutionMeasurementBasis.Capture(second with { SnapshotJson = second.SnapshotJson.Replace("5.0", "10") }).Basis).IsComparable);
    }


    private static readonly PreservedEvolutionMeasurement Initial = EvolutionMeasurementBasis.Capture(Reading() with { CapturedAt = new DateTime(2026, 1, 1) });

    [Theory]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, false, true)]
    public async Task Explicit_permissions_distinguish_read_manage_and_monitor(bool read, bool manage, bool monitor, bool global) {
        var permissions = new List<string>();
        if (read) permissions.Add(ValoraPermissions.Evolution.Read);
        if (manage) permissions.Add(ValoraPermissions.Evolution.Manage);
        if (monitor) permissions.Add(ValoraPermissions.Evolution.SnapshotsGenerate);
        var context = new ContextStub(permissions.ToArray(), global);
        var repository = new CaptureEvolutionRepository();
        var commercial = new CommercialStub();
        var cycles = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]), context, commercial);
        var snapshots = new EvolutionSnapshotService(repository, context, commercial);
        if (read || global) {
            await cycles.List(OrganizationId, default);
            await snapshots.List(OrganizationId, Guid.NewGuid(), default);
        }
        else {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => cycles.List(OrganizationId, default));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => snapshots.List(OrganizationId, Guid.NewGuid(), default));
        }
        if (manage || global) await cycles.Open(OrganizationId, UserId, Request(SurveyId), default);
        else await Assert.ThrowsAsync<UnauthorizedAccessException>(() => cycles.Open(OrganizationId, UserId, Request(SurveyId), default));
        if (monitor || global) await snapshots.Generate(OrganizationId, UserId, Guid.NewGuid(), "Evidence", "Interpretation", "Next step", default);
        else await Assert.ThrowsAsync<UnauthorizedAccessException>(() => snapshots.Generate(OrganizationId, UserId, Guid.NewGuid(), "Evidence", "Interpretation", "Next step", default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => cycles.List(Guid.NewGuid(), default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => snapshots.Generate(OrganizationId, Guid.NewGuid(), Guid.NewGuid(), "Evidence", "Interpretation", "Next step", default));
    }

    [Fact]
    public async Task Commercial_restriction_after_preview_prevents_confirmation_and_replay() {
        var repository = new CaptureEvolutionRepository();
        var commercial = new CommercialStub();
        var cycles = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]), new ContextStub(), commercial);
        var cycle = Guid.NewGuid();
        var preview = await cycles.Preview(OrganizationId, UserId, cycle, SurveyId, default);
        commercial.Enabled = false;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => cycles.ConfirmReevaluation(OrganizationId, UserId, cycle, SurveyId, preview.Version, preview.Fingerprint, Guid.NewGuid().ToString("N"), default));
        repository.ReplayId = Guid.NewGuid();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => cycles.ConfirmReevaluation(OrganizationId, UserId, cycle, SurveyId, preview.Version, preview.Fingerprint, Guid.NewGuid().ToString("N"), default));
        Assert.Equal(0, repository.ReevaluationWrites);
        var monitor = new EvolutionSnapshotService(repository, new ContextStub([ValoraPermissions.Evolution.SnapshotsGenerate]), commercial);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => monitor.Generate(OrganizationId, UserId, cycle, "Evidence", "Interpretation", "Next step", default));
        Assert.Equal(0, repository.SnapshotWrites);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("source")]
    [InlineData("closed")]
    public async Task Changed_cycle_or_source_requires_new_preview_without_writing(string change) {
        var repository = new CaptureEvolutionRepository();
        var readings = new List<EvolutionSurveyComparisonDto> { Reading() };
        var cycles = new EvolutionCycleService(repository, new IntelligenceRepositoryStub(readings), new ContextStub(), new CommercialStub());
        var cycle = Guid.NewGuid();
        var preview = await cycles.Preview(OrganizationId, UserId, cycle, SurveyId, default);
        if (change == "version") repository.CurrentVersion++;
        if (change == "closed") repository.Status = "closed";
        if (change == "source") readings[0] = Reading() with { MaturityIndex = 80 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => cycles.ConfirmReevaluation(OrganizationId, UserId, cycle, SurveyId, preview.Version, preview.Fingerprint, Guid.NewGuid().ToString("N"), default));
        Assert.Equal(0, repository.ReevaluationWrites);
    }

    [Fact]
    public async Task Authorized_replay_returns_existing_registration_without_new_write() {
        var repository = new CaptureEvolutionRepository { ReplayId = Guid.NewGuid(), Status = "closed" };
        var cycles = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([]), new ContextStub(), new CommercialStub());
        Assert.Equal(repository.ReplayId, await cycles.ConfirmReevaluation(OrganizationId, UserId, Guid.NewGuid(), SurveyId, 1, new string('a', 64), Guid.NewGuid().ToString("N"), default));
        Assert.Equal(0, repository.ReevaluationWrites);
    }

    private static OpenEvolutionCycleRequest Request(Guid resultId) => new(
        DiagnosticId: null,
        ResultId: null,
        GovernanceCycleId: null,
        Title: "Ciclo validado",
        Summary: "Acompanhar evolução sem contaminar a medição.",
        BaselineScore: null,
        TargetScore: 85m,
        BaselineLevel: null,
        PeriodStart: new DateTime(2026, 1, 1),
        PeriodEnd: null,
        EvidenceSummary: "Evidência inicial registrada.", SourceSurveyId: resultId);

    private static EvolutionSurveyComparisonDto Reading() => new(
        SurveyId,
        "Diagnóstico de Cultura",
        new DateTime(2026, 1, 1),
        new DateTime(2026, 2, 1),
        """
        {"methodology":{"code":"professional","version":3},"questions":[{"dimension":"people","scale":{"min":1,"max":5}}]}
        """,
        4,
        "population-a",
        72.5m,
        ["people"],
        [new EvolutionQuestionCriterionRow("people", "trust", "maturity", "1", 1)]);

    private sealed class CaptureEvolutionRepository : IEvolutionRepository {
        public long CurrentVersion { get; set; } = 1;
        public string Status { get; set; } = "open";
        public int ReevaluationWrites { get; private set; }
        public int SnapshotWrites { get; private set; }
        public Guid? ReplayId { get; set; }
        public Task<long> Version(Guid o, Guid cycle, CancellationToken c) => Task.FromResult(CurrentVersion);
        public Task<PreservedEvolutionMeasurement?> Baseline(Guid o, Guid cycle, CancellationToken c) => Task.FromResult<PreservedEvolutionMeasurement?>(Initial);
        public Task<Guid?> ReevaluationReplay(Guid o, Guid cycle, string commandId, string fingerprint, Guid surveyId, long version, CancellationToken c) => Task.FromResult(ReplayId);
        public Task<Guid> Reevaluate(Guid o, Guid u, Guid cycle, EvolutionReevaluationPreview preview, string commandId, CancellationToken c) { ReevaluationWrites++; return Task.FromResult(Guid.NewGuid()); }

        public Task<Guid?> ResultSurvey(Guid o, Guid resultId, CancellationToken c) => Task.FromResult<Guid?>(null);
        public Task<Guid?> DiagnosticSurvey(Guid o, Guid diagnosticId, CancellationToken c) => Task.FromResult<Guid?>(diagnosticId == SurveyId ? SurveyId : null);
        public Task<bool> LinksAvailable(Guid o, Guid? diagnosticId, Guid? governanceCycleId, CancellationToken c) => Task.FromResult(true);
        public OpenEvolutionCycleRequest? LastOpen { get; private set; }
        public Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c) => Task.FromResult<IReadOnlyList<EvolutionCycleDto>>(Array.Empty<EvolutionCycleDto>());
        public Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c) => Task.FromResult<EvolutionCycleDto?>(new(id, null, null, "Cycle", "Objective", Status, 60, 60, null, null, null, "insufficient_sample", DateTime.UtcNow, null, "Evidence", null, "organizational_measurement", "professional@3", DateTime.UtcNow));
        public Task<IReadOnlyList<EvolutionSnapshotDto>> Snapshots(Guid o, Guid cycle, CancellationToken c) => Task.FromResult<IReadOnlyList<EvolutionSnapshotDto>>(Array.Empty<EvolutionSnapshotDto>());
        public Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest r, CancellationToken c) { LastOpen = r; return Task.FromResult(Guid.NewGuid()); }
        public Task<Guid> Snapshot(Guid o, Guid u, Guid cycle, string evidence, string interpretation, string recommendation, CancellationToken c) { SnapshotWrites++; return Task.FromResult(Guid.NewGuid()); }
        public Task Close(Guid o, Guid u, Guid cycle, string learning, CancellationToken c) => Task.CompletedTask;
    }
    private sealed class ContextStub(string[]? permissions = null, bool global = false) : ICurrentRequestContext {
        public CurrentRequestContext GetCurrent() => new(UserId, null, OrganizationId, global ? OrganizationId : null, global, [], permissions ?? [ValoraPermissions.Evolution.Read, ValoraPermissions.Evolution.Manage], [], [], [], "active", 1);
    }
    private sealed class CommercialStub(bool enabled = true) : ICommercialSaasRepository {
        public bool Enabled { get; set; } = enabled;
        public Task<ModuleAccessDecision> EvaluateAccessAsync(Guid clientId, string moduleCode, bool writeOperation, CancellationToken c) => Task.FromResult(Enabled ? ModuleAccessDecision.Granted() : ModuleAccessDecision.Denied("MODULE_READ_ONLY", "Somente consulta."));
        public Task<IReadOnlyList<CommercialModule>> ListModulesAsync(Guid? clientId, CancellationToken c) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommercialPlan>> ListPlansAsync(CancellationToken c) => throw new NotSupportedException();
        public Task<bool> SetModuleStatusAsync(Guid clientId, string moduleCode, string status, Guid actorUserId, string reason, string correlationId, CancellationToken c) => throw new NotSupportedException();
        public Task RequestUpgradeAsync(Guid clientId, Guid actorUserId, string moduleCode, string reason, string correlationId, CancellationToken c) => throw new NotSupportedException();
    }

    private sealed class IntelligenceRepositoryStub(IReadOnlyList<EvolutionSurveyComparisonDto> readings) : IOrganizationalIntelligenceRepository {
        public Task<IReadOnlyList<EvolutionSurveyComparisonDto>> SelectEvolutionSurveysAsync(Guid organizationId, Guid? surveyId, int page, int size, CancellationToken ct) => Task.FromResult(readings);
        public Task<EvolutionSurveyComparisonDto?> GetEvolutionSurveyAsync(Guid organizationId, Guid surveyId, CancellationToken ct) => Task.FromResult(readings.FirstOrDefault(x => x.SurveyId == surveyId));
        public Task<IReadOnlyList<EvolutionSurveyComparisonDto>> ListEvolutionComparisonSurveysAsync(Guid organizationId, CancellationToken ct) => Task.FromResult(readings);
        public Task<EvidenceSummaryDto> GetEvidenceAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<OrganizationalIntelligenceDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> ListRunsAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<OrganizationalIntelligenceRunDto?> GetRunAsync(Guid organizationId, Guid id, CancellationToken ct) => throw new NotImplementedException();
        public Task SaveAnalysisAsync(OrganizationalIntelligenceRunDto run, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<OrganizationalJourneyEventDto>> ListJourneyAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<OrganizationalJourneyEventDto> CreateJourneyEventAsync(OrganizationalJourneyEventDto item, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<ValoraIndicatorDefinitionDto>> ListIndicatorsAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<ValoraActionDto>> ListActionsAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<ValoraActionDto> CreateActionAsync(ValoraActionDto item, Guid userId, CancellationToken ct) => throw new NotImplementedException();
        public Task<ValoraActionDto?> UpdateActionAsync(Guid organizationId, Guid actionId, UpdateValoraActionRequest request, Guid userId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<ValoraActionHistoryDto>> ListActionHistoryAsync(Guid organizationId, Guid actionId, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> DeleteActionAsync(Guid organizationId, Guid actionId, Guid userId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<EvidenceItemDto>> ListEvidenceItemsAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<IntelligenceModuleRecordDto>> ListModuleRecordsAsync(Guid organizationId, string module, CancellationToken ct) => throw new NotImplementedException();
    }
}
