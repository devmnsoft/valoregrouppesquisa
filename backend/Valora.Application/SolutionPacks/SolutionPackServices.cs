using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Valora.Application.SolutionPacks;

public sealed record SolutionPackSummary(Guid Id, Guid? OrganizationId, string Name, string Description, string Category, string Segment, string Status, bool IsOfficial, string? CurrentVersion, DateTime UpdatedAt);
public sealed record SolutionPackDetails(Guid Id, Guid? OrganizationId, string Name, string Description, string Category, string Segment, string Status, bool IsOfficial, string? CurrentVersion, string? Evidence, IReadOnlyList<SolutionPackItemDto> Items, IReadOnlyList<SolutionPackDependencyDto> Dependencies);
public sealed record SolutionPackItemDto(Guid Id, string ItemType, string Name, string SourceModule, Guid? SourceTemplateId, string MetadataJson);
public sealed record SolutionPackDependencyDto(Guid Id, string DependencyType, string? RequiredModule, string? RequiredPermission, string? MinimumVersion);
public sealed record InstallationDto(Guid Id, Guid OrganizationId, Guid SolutionPackId, string PackName, string VersionNumber, string Status, bool CanRollback, DateTime InstalledAt);
public sealed record UpdateDto(Guid InstallationId, Guid SolutionPackId, string PackName, string InstalledVersion, string AvailableVersion);
public sealed record InstallationPreview(Guid PackId, string VersionNumber, IReadOnlyList<SolutionPackItemDto> Items, IReadOnlyList<SolutionPackDependencyDto> MissingDependencies, bool RequiresOverwriteConfirmation, bool CanInstall);
// Classes (não records): em .NET, records somente honram metadados de validação declarados nos
// parâmetros do construtor primário, invisíveis ao runtime Validator. Como classes, o metadata
// fica visível tanto ao MVC quanto às chamadas Validator.ValidateObject abaixo.
public sealed class CreateSolutionPackRequest {
    [Required, StringLength(160, MinimumLength = 3)] public string Name { get; set; } = "";
    [Required, StringLength(2000)] public string Description { get; set; } = "";
    [Required, StringLength(80)] public string Category { get; set; } = "";
    [Required, StringLength(80)] public string Segment { get; set; } = "";
    public bool IsOfficial { get; set; }
    public string? Evidence { get; set; }

    public CreateSolutionPackRequest() { }

    public CreateSolutionPackRequest(string name, string description, string category, string segment, bool isOfficial, string? evidence) {
        Name = name; Description = description; Category = category; Segment = segment; IsOfficial = isOfficial; Evidence = evidence;
    }
}
public sealed class NewVersionRequest {
    [Required, StringLength(32)] public string VersionNumber { get; set; } = "";
    public IReadOnlyList<SolutionPackItemInput> Items { get; set; } = new List<SolutionPackItemInput>();
    public IReadOnlyList<SolutionPackDependencyInput> Dependencies { get; set; } = new List<SolutionPackDependencyInput>();
    public string? ReleaseNotes { get; set; }
}
public sealed class SolutionPackItemInput {
    [Required] public string ItemType { get; set; } = "";
    [Required] public string Name { get; set; } = "";
    [Required] public string SourceModule { get; set; } = "";
    public Guid? SourceTemplateId { get; set; }
    public string? MetadataJson { get; set; }
}
public sealed class SolutionPackDependencyInput {
    [Required] public string DependencyType { get; set; } = "";
    public string? RequiredModule { get; set; }
    public string? RequiredPermission { get; set; }
    public string? MinimumVersion { get; set; }
}
public sealed record InstallSolutionPackRequest(Guid VersionId, bool ConfirmOverwrite);

public interface ISolutionPackRepository {
    Task<IReadOnlyList<SolutionPackSummary>> ListAsync(Guid organizationId, string? segment, string? category, CancellationToken ct);
    Task<SolutionPackDetails?> GetAsync(Guid id, Guid organizationId, CancellationToken ct);
    Task<Guid> CreateAsync(Guid id, Guid organizationId, Guid actor, CreateSolutionPackRequest request, CancellationToken ct);
    Task<Guid> CreateVersionAsync(Guid packId, Guid organizationId, Guid actor, NewVersionRequest request, CancellationToken ct);
    Task<bool> PublishAsync(Guid packId, Guid organizationId, Guid actor, CancellationToken ct);
    Task<InstallationPreview?> PreviewAsync(Guid packId, Guid organizationId, Guid? versionId, CancellationToken ct);
    Task<Guid> InstallAsync(Guid installationId, Guid packId, Guid organizationId, Guid actor, InstallSolutionPackRequest request, CancellationToken ct);
    Task<bool> RollbackAsync(Guid installationId, Guid organizationId, Guid actor, CancellationToken ct);
    Task<IReadOnlyList<InstallationDto>> InstallationsAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<UpdateDto>> UpdatesAsync(Guid organizationId, CancellationToken ct);
}

public sealed class SolutionPackService(ISolutionPackRepository repository, ILogger<SolutionPackService> logger) {
    public Task<IReadOnlyList<SolutionPackSummary>> List(Guid organizationId, string? segment, string? category, CancellationToken ct) => repository.ListAsync(Required(organizationId), segment, category, ct);
    public Task<SolutionPackDetails?> Get(Guid id, Guid organizationId, CancellationToken ct) => repository.GetAsync(Required(id), Required(organizationId), ct);
    public async Task<Guid> Create(Guid organizationId, Guid actor, CreateSolutionPackRequest request, CancellationToken ct) { Required(organizationId); Required(actor); Validator.ValidateObject(request, new ValidationContext(request), true); var id = Guid.NewGuid(); await repository.CreateAsync(id, organizationId, actor, request, ct); logger.LogInformation("Solution pack {PackId} created in organization {OrganizationId}", id, organizationId); return id; }
    internal static Guid Required(Guid id) => id == Guid.Empty ? throw new ValidationException("Selecione uma organização válida.") : id;
}
public sealed class SolutionPackVersionService(ISolutionPackRepository repository, ILogger<SolutionPackVersionService> logger) {
    public async Task<Guid> NewVersion(Guid packId, Guid organizationId, Guid actor, NewVersionRequest request, CancellationToken ct) { SolutionPackService.Required(packId); SolutionPackService.Required(organizationId); SolutionPackService.Required(actor); Validator.ValidateObject(request, new ValidationContext(request), true); if (request.Items.Count == 0) throw new ValidationException("Selecione ao menos um item para a versão."); var id = await repository.CreateVersionAsync(packId, organizationId, actor, request, ct); logger.LogInformation("Version {VersionId} created for pack {PackId}", id, packId); return id; }
    public Task<bool> Publish(Guid packId, Guid organizationId, Guid actor, CancellationToken ct) => repository.PublishAsync(SolutionPackService.Required(packId), SolutionPackService.Required(organizationId), SolutionPackService.Required(actor), ct);
}
public sealed class SolutionPackDependencyService(ISolutionPackRepository repository) { public Task<InstallationPreview?> Preview(Guid packId, Guid organizationId, Guid? versionId, CancellationToken ct) => repository.PreviewAsync(SolutionPackService.Required(packId), SolutionPackService.Required(organizationId), versionId, ct); }
public sealed class SolutionPackInstallationService(ISolutionPackRepository repository, ILogger<SolutionPackInstallationService> logger) {
    public Task<IReadOnlyList<InstallationDto>> List(Guid organizationId, CancellationToken ct) => repository.InstallationsAsync(SolutionPackService.Required(organizationId), ct);
    public async Task<Guid> Install(Guid packId, Guid organizationId, Guid actor, InstallSolutionPackRequest request, CancellationToken ct) { SolutionPackService.Required(request.VersionId); var preview = await repository.PreviewAsync(packId, organizationId, request.VersionId, ct) ?? throw new ValidationException("Pacote ou versão indisponível."); if (!preview.CanInstall) throw new ValidationException("As dependências do pacote não foram atendidas."); if (preview.RequiresOverwriteConfirmation && !request.ConfirmOverwrite) throw new ValidationException("Confirme a preservação ou substituição dos itens existentes no preview."); var id = Guid.NewGuid(); await repository.InstallAsync(id, packId, organizationId, actor, request, ct); logger.LogInformation("Pack {PackId} installed as {InstallationId} in {OrganizationId}", packId, id, organizationId); return id; }
}
public sealed class SolutionPackRollbackService(ISolutionPackRepository repository) { public Task<bool> Rollback(Guid installationId, Guid organizationId, Guid actor, CancellationToken ct) => repository.RollbackAsync(SolutionPackService.Required(installationId), SolutionPackService.Required(organizationId), SolutionPackService.Required(actor), ct); }
public sealed class SolutionPackCatalogService(ISolutionPackRepository repository) { public Task<IReadOnlyList<UpdateDto>> Updates(Guid organizationId, CancellationToken ct) => repository.UpdatesAsync(SolutionPackService.Required(organizationId), ct); }
