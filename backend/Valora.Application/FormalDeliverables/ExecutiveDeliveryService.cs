using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.Exceptions;
using Valora.Application.Workspace;

namespace Valora.Application.FormalDeliverables;

public sealed class ExecutiveDeliveryService(
    IFormalDeliverableRepository repository,
    IValoraDocumentService documents,
    IDocumentStore store,
    ISecureShareLinkService shares,
    IEntitlementService entitlements,
    IPermissionService permissions,
    IExportAuditService audit) : IExecutiveDeliveryService {
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<PageResult<DeliverableListItemDto>> ListAsync(Guid organizationId, DeliverableListQuery query, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        var (items, total) = await repository.ListAsync(organizationId, query, cancellationToken);
        return new PageResult<DeliverableListItemDto>(items, query.ValidPage, query.ValidPageSize, total);
    }

    public Task<DeliverableDetailsDto?> GetAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        return repository.GetDetailsAsync(organizationId, deliverableId, cancellationToken);
    }

    public async Task<DeliverableDetailsDto> PrepareAsync(Guid organizationId, Guid userId, PrepareDeliverableRequest request, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        if (request is null) throw new ValidationAppException("Informe os dados do entregável.");
        if (string.IsNullOrWhiteSpace(request.CommandId)) throw new ValidationAppException("Informe a chave da operação.");
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 3)
            throw new ValidationAppException("Informe um título com pelo menos 3 caracteres.");

        var existing = await repository.FindByCommandIdAsync(organizationId, request.CommandId.Trim(), cancellationToken);
        if (existing is not null) {
            return await repository.GetDetailsAsync(organizationId, existing.Id, cancellationToken)
                ?? throw new InvalidOperationException("Entregável idempotente não encontrado após preparação.");
        }

        var templateCode = string.IsNullOrWhiteSpace(request.TemplateCode) ? "executive_valora" : request.TemplateCode.Trim();
        var template = await repository.GetTemplateAsync(templateCode, cancellationToken);
        var deliverableType = template?.DeliverableType ?? InferType(templateCode);
        await EnsureModuleAsync(organizationId, deliverableType);

        var result = await repository.LoadEligibleResultAsync(organizationId, request.ResultId, cancellationToken)
            ?? throw new ValidationAppException("Selecione um resultado concluído com score consolidado.");
        if (request.DiagnosticId != Guid.Empty && result.DiagnosticId != request.DiagnosticId)
            throw new ValidationAppException("O resultado informado não pertence ao diagnóstico selecionado.");

        var hash = ComputeResultHash(result);
        var sections = (request.Sections ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var metadata = JsonSerializer.Serialize(new {
            sections,
            executiveNotes = request.ExecutiveNotes,
            reviewerUserId = request.ReviewerUserId,
            sourceResultHash = hash,
            methodologyName = result.MethodologyName,
            methodologyVersion = result.MethodologyVersion,
            templateCode
        }, JsonOptions);

        var entity = new FormalDeliverableEntity(
            id, organizationId, result.DiagnosticId, result.ResultId, deliverableType, request.Title.Trim(),
            DeliverableEditorialStatuses.Draft, DeliverableEditorialStatuses.Draft, DeliverableProcessingStatuses.AwaitingGeneration,
            1, templateCode, JsonSerializer.Serialize(sections), request.ExecutiveNotes?.Trim(),
            request.ReviewerUserId, hash, result.MethodologyName, result.MethodologyVersion,
            null, null, null, request.CommandId.Trim(), null, null, userId, metadata, now, now);
        await repository.InsertAsync(entity, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "deliverable.prepared", "formal_deliverable", id.ToString(), true, null, cancellationToken);
        return await repository.GetDetailsAsync(organizationId, id, cancellationToken)
            ?? throw new InvalidOperationException("Falha ao carregar o entregável preparado.");
    }

    public async Task<DeliverableDetailsDto> SubmitForReviewAsync(Guid organizationId, Guid userId, Guid deliverableId, SubmitReviewRequest request, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        if (string.IsNullOrWhiteSpace(request.CommandId)) throw new ValidationAppException("Informe a chave da operação.");
        var entity = await repository.GetEntityAsync(organizationId, deliverableId, cancellationToken)
            ?? throw new NotFoundAppException("Entregável não encontrado.");
        if (!string.Equals(entity.EditorialStatus, DeliverableEditorialStatuses.Draft, StringComparison.Ordinal))
            throw new InvalidOperationException("Somente rascunhos podem ser enviados para revisão.");

        var updated = entity with {
            EditorialStatus = DeliverableEditorialStatuses.InReview,
            Status = DeliverableEditorialStatuses.InReview,
            CommandId = request.CommandId.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow,
            MetadataJson = MergeMetadata(entity.MetadataJson, new { reviewNotes = request.Notes, submittedForReviewBy = userId })
        };
        await repository.UpdateLifecycleAsync(updated, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "deliverable.submitted_for_review", "formal_deliverable", deliverableId.ToString(), true, null, cancellationToken);
        return await repository.GetDetailsAsync(organizationId, deliverableId, cancellationToken)
            ?? throw new InvalidOperationException("Falha ao carregar o entregável após envio para revisão.");
    }

    public async Task<DeliverableDetailsDto> PublishAsync(Guid organizationId, Guid userId, Guid deliverableId, PublishDeliverableRequest request, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        if (request is null || !request.Confirmed) throw new ValidationAppException("Confirme a publicação do entregável.");
        if (string.IsNullOrWhiteSpace(request.CommandId)) throw new ValidationAppException("Informe a chave da operação.");

        var byCommand = await repository.FindByCommandIdAsync(organizationId, request.CommandId.Trim(), cancellationToken);
        if (byCommand is not null && byCommand.Id == deliverableId &&
            string.Equals(byCommand.EditorialStatus, DeliverableEditorialStatuses.Published, StringComparison.Ordinal)) {
            return await repository.GetDetailsAsync(organizationId, deliverableId, cancellationToken)
                ?? throw new InvalidOperationException("Entregável publicado não encontrado.");
        }

        var entity = await repository.GetEntityAsync(organizationId, deliverableId, cancellationToken)
            ?? throw new NotFoundAppException("Entregável não encontrado.");
        await EnsureModuleAsync(organizationId, entity.DeliverableType);

        var editorialOk = string.Equals(entity.EditorialStatus, DeliverableEditorialStatuses.InReview, StringComparison.Ordinal)
            || (string.Equals(entity.EditorialStatus, DeliverableEditorialStatuses.Draft, StringComparison.Ordinal)
                && entity.ReviewerUserId is Guid reviewer && reviewer == userId);
        if (!editorialOk)
            throw new InvalidOperationException("Publique somente entregáveis em revisão (ou rascunho quando você é o revisor designado).");

        await EnsurePublisherAuthorityAsync(organizationId, userId, entity.ReviewerUserId);

        if (entity.ResultId is null || entity.ResultId == Guid.Empty)
            throw new InvalidOperationException("O entregável não possui resultado de origem.");
        var result = await repository.LoadEligibleResultAsync(organizationId, entity.ResultId.Value, cancellationToken)
            ?? throw new InvalidOperationException("O resultado de origem não está mais elegível para publicação.");
        var currentHash = ComputeResultHash(result);
        if (!string.IsNullOrWhiteSpace(entity.SourceResultHash) &&
            !string.Equals(entity.SourceResultHash, currentHash, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException(
                "O resultado de origem mudou desde a preparação (divergência de fingerprint). Não é possível publicar dados misturados; prepare uma nova versão a partir do resultado atual.");
        }

        var processing = entity with {
            ProcessingStatus = DeliverableProcessingStatuses.Processing,
            Status = DeliverableProcessingStatuses.Processing,
            CommandId = request.CommandId.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await repository.UpdateLifecycleAsync(processing, cancellationToken);
        await repository.WriteGenerationJobAsync(organizationId, deliverableId, userId, "processing", null, cancellationToken);

        try {
            var format = string.Equals(entity.DeliverableType, DeliverableTypes.Certificate, StringComparison.Ordinal)
                ? DeliverableFormat.CertificatePdf
                : DeliverableFormat.Pdf;
            var document = await documents.GenerateAsync(new DocumentRequest(organizationId, entity.ResultId.Value, format, userId), cancellationToken);
            var publishedAt = DateTimeOffset.UtcNow;
            var published = processing with {
                EditorialStatus = DeliverableEditorialStatuses.Published,
                ProcessingStatus = DeliverableProcessingStatuses.Available,
                Status = DeliverableEditorialStatuses.Published,
                DocumentId = document.Id,
                FileId = document.Id,
                PublishedAt = publishedAt,
                PublishedBy = userId,
                SourceResultHash = currentHash,
                UpdatedAt = publishedAt,
                MetadataJson = MergeMetadata(processing.MetadataJson, new {
                    publishNotes = request.Notes,
                    traceCode = document.TraceCode,
                    contentType = document.ContentType,
                    fileName = document.FileName
                })
            };
            await repository.UpdateLifecycleAsync(published, cancellationToken);
            if (published.ParentDeliverableId is Guid parentId)
                await repository.MarkSupersededAsync(organizationId, parentId, cancellationToken);
            await repository.WriteGenerationJobAsync(organizationId, deliverableId, userId, "completed", null, cancellationToken);
            await repository.WriteReportGenerationLogAsync(organizationId, deliverableId, entity.ResultId, userId, format.ToString(), "available", document.TraceCode, cancellationToken);
            await audit.RecordAsync(organizationId, userId, "deliverable.published", "formal_deliverable", deliverableId.ToString(), true, document.TraceCode, cancellationToken);
        }
        catch (Exception ex) {
            var failed = processing with {
                ProcessingStatus = DeliverableProcessingStatuses.Failed,
                Status = DeliverableProcessingStatuses.Failed,
                UpdatedAt = DateTimeOffset.UtcNow,
                MetadataJson = MergeMetadata(processing.MetadataJson, new { lastError = ex.Message })
            };
            await repository.UpdateLifecycleAsync(failed, cancellationToken);
            await repository.WriteGenerationJobAsync(organizationId, deliverableId, userId, "failed", ex.Message, cancellationToken);
            await repository.WriteReportGenerationLogAsync(organizationId, deliverableId, entity.ResultId, userId, "Pdf", "failed", ex.Message, cancellationToken);
            await audit.RecordAsync(organizationId, userId, "deliverable.publish_failed", "formal_deliverable", deliverableId.ToString(), false, ex.Message, cancellationToken);
            throw;
        }

        return await repository.GetDetailsAsync(organizationId, deliverableId, cancellationToken)
            ?? throw new InvalidOperationException("Falha ao carregar o entregável publicado.");
    }

    public async Task<DeliverableDetailsDto> CreateNewVersionAsync(Guid organizationId, Guid userId, Guid deliverableId, string commandId, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        if (string.IsNullOrWhiteSpace(commandId)) throw new ValidationAppException("Informe a chave da operação.");
        var existingCommand = await repository.FindByCommandIdAsync(organizationId, commandId.Trim(), cancellationToken);
        if (existingCommand is not null) {
            return await repository.GetDetailsAsync(organizationId, existingCommand.Id, cancellationToken)
                ?? throw new InvalidOperationException("Nova versão idempotente não encontrada.");
        }

        var parent = await repository.GetEntityAsync(organizationId, deliverableId, cancellationToken)
            ?? throw new NotFoundAppException("Entregável não encontrado.");
        if (!string.Equals(parent.EditorialStatus, DeliverableEditorialStatuses.Published, StringComparison.Ordinal))
            throw new InvalidOperationException("Somente entregáveis publicados podem originar uma nova versão.");
        if (parent.ResultId is null)
            throw new InvalidOperationException("O entregável publicado não possui resultado de origem.");

        var result = await repository.LoadEligibleResultAsync(organizationId, parent.ResultId.Value, cancellationToken)
            ?? throw new InvalidOperationException("O resultado de origem não está mais elegível.");
        var hash = ComputeResultHash(result);
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var child = new FormalDeliverableEntity(
            id, organizationId, parent.DiagnosticId, parent.ResultId, parent.DeliverableType, parent.Title,
            DeliverableEditorialStatuses.Draft, DeliverableEditorialStatuses.Draft, DeliverableProcessingStatuses.AwaitingGeneration,
            parent.VersionNumber + 1, parent.TemplateCode, parent.SectionsJson, parent.ExecutiveNotes,
            parent.ReviewerUserId, hash, result.MethodologyName, result.MethodologyVersion,
            null, null, parent.Id, commandId.Trim(), null, null, userId,
            MergeMetadata(parent.MetadataJson, new { parentDeliverableId = parent.Id, sourceResultHash = hash }),
            now, now);
        await repository.InsertAsync(child, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "deliverable.version_created", "formal_deliverable", id.ToString(), true, null, cancellationToken);
        return await repository.GetDetailsAsync(organizationId, id, cancellationToken)
            ?? throw new InvalidOperationException("Falha ao carregar a nova versão.");
    }

    public async Task<GeneratedDocument> DownloadAsync(Guid organizationId, Guid userId, Guid deliverableId, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        var entity = await repository.GetEntityAsync(organizationId, deliverableId, cancellationToken)
            ?? throw new NotFoundAppException("Entregável não encontrado.");
        await EnsureModuleAsync(organizationId, entity.DeliverableType);
        if (!string.Equals(entity.ProcessingStatus, DeliverableProcessingStatuses.Available, StringComparison.Ordinal))
            throw new InvalidOperationException("O arquivo ainda não está disponível para download.");
        var documentId = entity.DocumentId ?? entity.FileId
            ?? throw new InvalidOperationException("O entregável não possui arquivo persistido.");
        var document = await store.FindAsync(organizationId, documentId, cancellationToken)
            ?? throw new InvalidOperationException("Arquivo publicado indisponível no armazenamento.");
        await audit.RecordAsync(organizationId, userId, "deliverable.downloaded", "formal_deliverable", deliverableId.ToString(), true, document.TraceCode, cancellationToken);
        return document;
    }

    public async Task<CreatedShareLink> ShareAsync(Guid organizationId, Guid userId, Guid deliverableId, CreateDeliverableShareRequest request, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        if (request is null || !request.Confirmed) throw new ValidationAppException("Confirme a criação do link seguro.");
        if (string.IsNullOrWhiteSpace(request.CommandId)) throw new ValidationAppException("Informe a chave da operação.");
        var entity = await repository.GetEntityAsync(organizationId, deliverableId, cancellationToken)
            ?? throw new NotFoundAppException("Entregável não encontrado.");
        if (!string.Equals(entity.EditorialStatus, DeliverableEditorialStatuses.Published, StringComparison.Ordinal) ||
            !string.Equals(entity.ProcessingStatus, DeliverableProcessingStatuses.Available, StringComparison.Ordinal))
            throw new InvalidOperationException("Compartilhe somente entregáveis publicados com arquivo disponível.");
        if (entity.ResultId is null)
            throw new InvalidOperationException("O entregável não possui resultado para compartilhamento.");

        var hours = Math.Clamp(request.ValidForHours, 1, 2160);
        var created = await shares.CreateForDeliverableAsync(organizationId, deliverableId, entity.ResultId.Value, userId,
            TimeSpan.FromHours(hours), request.AllowDownload, cancellationToken);
        await audit.RecordAsync(organizationId, userId, "deliverable.share_created", "formal_deliverable", deliverableId.ToString(), true,
            request.InternalLabel, cancellationToken);
        return created;
    }

    public Task<bool> RevokeShareAsync(Guid organizationId, Guid userId, Guid shareLinkId, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        return shares.RevokeAsync(organizationId, shareLinkId, userId, cancellationToken);
    }

    public Task<IReadOnlyList<DeliverableAccessEventDto>> ListAccessHistoryAsync(Guid organizationId, Guid deliverableId, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        return repository.ListAccessHistoryAsync(organizationId, deliverableId, cancellationToken);
    }

    public async Task<CertificateEligibilityResult> CertificateEligibilityAsync(Guid organizationId, Guid resultId, string? templateCode, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        var result = await repository.LoadEligibleResultAsync(organizationId, resultId, cancellationToken);
        if (result is null)
            return new CertificateEligibilityResult(false, null, "certificate.result_incomplete",
                "O resultado precisa estar concluído e com score consolidado para elegibilidade de certificado.");

        var code = string.IsNullOrWhiteSpace(templateCode) ? "certificate_participation" : templateCode.Trim();
        var template = await repository.GetTemplateAsync(code, cancellationToken);
        using var config = JsonDocument.Parse(string.IsNullOrWhiteSpace(template?.ConfigurationJson) ? "{}" : template.ConfigurationJson);
        var root = config.RootElement;
        var certificateType = root.TryGetProperty("certificateType", out var typeEl) ? typeEl.GetString() : null;
        certificateType ??= InferCertificateType(code, template?.DeliverableType);

        if (IsMaturityOrClassification(certificateType, code)) {
            var hasCriteria = root.TryGetProperty("maturityCriteria", out var criteria) && criteria.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                || root.TryGetProperty("classificationCriteria", out var classCriteria) && classCriteria.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                || (root.TryGetProperty("formalMaturityCertification", out var formal) && formal.ValueKind == JsonValueKind.True);
            if (!hasCriteria) {
                return new CertificateEligibilityResult(false, certificateType ?? "maturity",
                    "certificate.maturity_criteria_undefined",
                    "Não há critérios de maturidade/classificação definidos no modelo. A conclusão do questionário não constitui certificação de maturidade.");
            }
        }

        if (string.Equals(certificateType, "participation", StringComparison.OrdinalIgnoreCase)
            || string.Equals(certificateType, "completion", StringComparison.OrdinalIgnoreCase)
            || root.TryGetProperty("formalMaturityCertification", out var flag) && flag.ValueKind == JsonValueKind.False) {
            return new CertificateEligibilityResult(true, certificateType ?? "participation", null,
                "Elegível para certificado de participação/conclusão com base no resultado concluído.");
        }

        if (template is null) {
            return new CertificateEligibilityResult(false, certificateType, "certificate.template_undefined",
                "Modelo de certificado não encontrado ou sem configuração explícita.");
        }

        return new CertificateEligibilityResult(true, certificateType ?? template.DeliverableType, null,
            "Elegível conforme configuração do modelo de certificado.");
    }

    public Task<IReadOnlyList<ResultOptionDto>> EligibleResultsAsync(Guid organizationId, string? search, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        return repository.SearchEligibleResultsAsync(organizationId, search, 50, cancellationToken);
    }

    public Task<IReadOnlyList<TemplateOptionDto>> TemplatesAsync(string? type, CancellationToken cancellationToken = default) =>
        repository.ListTemplatesAsync(type, cancellationToken);

    public Task<IReadOnlyList<ReviewerOptionDto>> ReviewersAsync(Guid organizationId, string? search, CancellationToken cancellationToken = default) {
        EnsureOrg(organizationId);
        return repository.ListReviewersAsync(organizationId, search, 50, cancellationToken);
    }

    public static string ComputeResultHash(EligibleResultInfo result) {
        var payload = $"{result.ResultId:N}|{result.TotalScore}|{result.MaxScore}|{result.Percentage}|{result.SubmittedAt:O}|{result.ScoreUpdatedAt:O}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private async Task EnsureModuleAsync(Guid organizationId, string deliverableType) {
        var primary = string.Equals(deliverableType, DeliverableTypes.Certificate, StringComparison.Ordinal) ? "certificates" : "reports";
        var legacy = string.Equals(deliverableType, DeliverableTypes.Certificate, StringComparison.Ordinal) ? "certificados" : "relatorios";
        if (await entitlements.CanUseAsync(organizationId, primary)) return;
        if (await entitlements.CanUseAsync(organizationId, legacy)) return;
        if (!string.Equals(deliverableType, DeliverableTypes.Certificate, StringComparison.Ordinal)
            && await entitlements.CanUseAsync(organizationId, "executive_reports")) return;
        throw new InvalidOperationException("MODULE_NOT_ENABLED");
    }

    private async Task EnsurePublisherAuthorityAsync(Guid organizationId, Guid userId, Guid? reviewerUserId) {
        if (reviewerUserId is Guid reviewer && reviewer == userId) return;
        if (await permissions.HasPermissionAsync(userId, "reports.generate", organizationId)) return;
        throw new ForbiddenAppException("Sem autoridade para publicar este entregável.");
    }

    private static void EnsureOrg(Guid organizationId) {
        if (organizationId == Guid.Empty) throw new ValidationAppException("Contexto de organização ausente.");
    }

    private static string InferType(string templateCode) =>
        templateCode.Contains("cert", StringComparison.OrdinalIgnoreCase) ? DeliverableTypes.Certificate : DeliverableTypes.ExecutiveReport;

    private static string? InferCertificateType(string code, string? deliverableType) {
        if (code.Contains("maturity", StringComparison.OrdinalIgnoreCase) || code.Contains("classif", StringComparison.OrdinalIgnoreCase))
            return "maturity";
        if (code.Contains("participation", StringComparison.OrdinalIgnoreCase) || code.Contains("completion", StringComparison.OrdinalIgnoreCase))
            return "participation";
        return string.Equals(deliverableType, DeliverableTypes.Certificate, StringComparison.Ordinal) ? "participation" : null;
    }

    private static bool IsMaturityOrClassification(string? certificateType, string code) =>
        string.Equals(certificateType, "maturity", StringComparison.OrdinalIgnoreCase)
        || string.Equals(certificateType, "classification", StringComparison.OrdinalIgnoreCase)
        || code.Contains("maturity", StringComparison.OrdinalIgnoreCase)
        || code.Contains("classif", StringComparison.OrdinalIgnoreCase);

    private static string MergeMetadata(string? existingJson, object patch) {
        Dictionary<string, JsonElement> map = new(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(existingJson)) {
            try {
                using var doc = JsonDocument.Parse(existingJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object) {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                        map[prop.Name] = prop.Value.Clone();
                }
            }
            catch (JsonException) { /* preserve replace on corrupt metadata */ }
        }
        using var patchDoc = JsonDocument.Parse(JsonSerializer.Serialize(patch, JsonOptions));
        foreach (var prop in patchDoc.RootElement.EnumerateObject())
            map[prop.Name] = prop.Value.Clone();
        return JsonSerializer.Serialize(map.ToDictionary(kv => kv.Key, kv => kv.Value), JsonOptions);
    }
}
