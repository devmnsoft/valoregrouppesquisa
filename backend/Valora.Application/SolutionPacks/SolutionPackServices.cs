using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Valora.Application.SolutionPacks;

public sealed record SolutionPackSummary(Guid Id, Guid? OrganizationId, string Name, string Description, string Category, string Segment, string Status, bool IsOfficial, string? CurrentVersion, DateTimeOffset UpdatedAt);
public sealed record SolutionPackDetails(Guid Id, Guid? OrganizationId, string Name, string Description, string Category, string Segment, string Status, bool IsOfficial, string? CurrentVersion, string? Evidence, IReadOnlyList<SolutionPackItemDto> Items, IReadOnlyList<SolutionPackDependencyDto> Dependencies);
public sealed record SolutionPackItemDto(Guid Id, string ItemType, string Name, string SourceModule, Guid? SourceTemplateId, string MetadataJson);
public sealed record SolutionPackDependencyDto(Guid Id, string DependencyType, string? RequiredModule, string? RequiredPermission, string? MinimumVersion);
public sealed record InstallationDto(Guid Id, Guid OrganizationId, Guid SolutionPackId, string PackName, string VersionNumber, string Status, bool CanRollback, DateTimeOffset InstalledAt);
public sealed record UpdateDto(Guid InstallationId, Guid SolutionPackId, string PackName, string InstalledVersion, string AvailableVersion);
public sealed record InstallationPreview(Guid PackId, string VersionNumber, IReadOnlyList<SolutionPackItemDto> Items, IReadOnlyList<SolutionPackDependencyDto> MissingDependencies, bool RequiresOverwriteConfirmation, bool CanInstall);
public sealed record CreateSolutionPackRequest([property:Required,StringLength(160,MinimumLength=3)] string Name,[property:Required,StringLength(2000)] string Description,[property:Required,StringLength(80)] string Category,[property:Required,StringLength(80)] string Segment,bool IsOfficial,string? Evidence);
public sealed record NewVersionRequest([property:Required,StringLength(32)] string VersionNumber, IReadOnlyList<SolutionPackItemInput> Items, IReadOnlyList<SolutionPackDependencyInput> Dependencies, string? ReleaseNotes);
public sealed record SolutionPackItemInput([property:Required] string ItemType,[property:Required] string Name,[property:Required] string SourceModule,Guid? SourceTemplateId,string? MetadataJson);
public sealed record SolutionPackDependencyInput([property:Required] string DependencyType,string? RequiredModule,string? RequiredPermission,string? MinimumVersion);
public sealed record InstallSolutionPackRequest(Guid VersionId,bool ConfirmOverwrite);

public interface ISolutionPackRepository
{
    Task<IReadOnlyList<SolutionPackSummary>> ListAsync(Guid organizationId,string? segment,string? category,CancellationToken ct);
    Task<SolutionPackDetails?> GetAsync(Guid id,Guid organizationId,CancellationToken ct);
    Task<Guid> CreateAsync(Guid id,Guid organizationId,Guid actor,CreateSolutionPackRequest request,CancellationToken ct);
    Task<Guid> CreateVersionAsync(Guid packId,Guid organizationId,Guid actor,NewVersionRequest request,CancellationToken ct);
    Task<bool> PublishAsync(Guid packId,Guid organizationId,Guid actor,CancellationToken ct);
    Task<InstallationPreview?> PreviewAsync(Guid packId,Guid organizationId,Guid? versionId,CancellationToken ct);
    Task<Guid> InstallAsync(Guid installationId,Guid packId,Guid organizationId,Guid actor,InstallSolutionPackRequest request,CancellationToken ct);
    Task<bool> RollbackAsync(Guid installationId,Guid organizationId,Guid actor,CancellationToken ct);
    Task<IReadOnlyList<InstallationDto>> InstallationsAsync(Guid organizationId,CancellationToken ct);
    Task<IReadOnlyList<UpdateDto>> UpdatesAsync(Guid organizationId,CancellationToken ct);
}

public sealed class SolutionPackService(ISolutionPackRepository repository,ILogger<SolutionPackService> logger)
{
    public Task<IReadOnlyList<SolutionPackSummary>> List(Guid organizationId,string? segment,string? category,CancellationToken ct)=>repository.ListAsync(Required(organizationId),segment,category,ct);
    public Task<SolutionPackDetails?> Get(Guid id,Guid organizationId,CancellationToken ct)=>repository.GetAsync(Required(id),Required(organizationId),ct);
    public async Task<Guid> Create(Guid organizationId,Guid actor,CreateSolutionPackRequest request,CancellationToken ct){Required(organizationId);Required(actor);Validator.ValidateObject(request,new ValidationContext(request),true);var id=Guid.NewGuid();await repository.CreateAsync(id,organizationId,actor,request,ct);logger.LogInformation("Solution pack {PackId} created in organization {OrganizationId}",id,organizationId);return id;}
    internal static Guid Required(Guid id)=>id==Guid.Empty?throw new ValidationException("Selecione uma organização válida."):id;
}
public sealed class SolutionPackVersionService(ISolutionPackRepository repository,ILogger<SolutionPackVersionService> logger)
{
    public async Task<Guid> NewVersion(Guid packId,Guid organizationId,Guid actor,NewVersionRequest request,CancellationToken ct){SolutionPackService.Required(packId);SolutionPackService.Required(organizationId);SolutionPackService.Required(actor);Validator.ValidateObject(request,new ValidationContext(request),true);if(request.Items.Count==0)throw new ValidationException("Selecione ao menos um item para a versão.");var id=await repository.CreateVersionAsync(packId,organizationId,actor,request,ct);logger.LogInformation("Version {VersionId} created for pack {PackId}",id,packId);return id;}
    public Task<bool> Publish(Guid packId,Guid organizationId,Guid actor,CancellationToken ct)=>repository.PublishAsync(SolutionPackService.Required(packId),SolutionPackService.Required(organizationId),SolutionPackService.Required(actor),ct);
}
public sealed class SolutionPackDependencyService(ISolutionPackRepository repository){public Task<InstallationPreview?> Preview(Guid packId,Guid organizationId,Guid? versionId,CancellationToken ct)=>repository.PreviewAsync(SolutionPackService.Required(packId),SolutionPackService.Required(organizationId),versionId,ct);}
public sealed class SolutionPackInstallationService(ISolutionPackRepository repository,ILogger<SolutionPackInstallationService> logger)
{
    public Task<IReadOnlyList<InstallationDto>> List(Guid organizationId,CancellationToken ct)=>repository.InstallationsAsync(SolutionPackService.Required(organizationId),ct);
    public async Task<Guid> Install(Guid packId,Guid organizationId,Guid actor,InstallSolutionPackRequest request,CancellationToken ct){SolutionPackService.Required(request.VersionId);var preview=await repository.PreviewAsync(packId,organizationId,request.VersionId,ct)??throw new ValidationException("Pacote ou versão indisponível.");if(!preview.CanInstall)throw new ValidationException("As dependências do pacote não foram atendidas.");if(preview.RequiresOverwriteConfirmation&&!request.ConfirmOverwrite)throw new ValidationException("Confirme a preservação ou substituição dos itens existentes no preview.");var id=Guid.NewGuid();await repository.InstallAsync(id,packId,organizationId,actor,request,ct);logger.LogInformation("Pack {PackId} installed as {InstallationId} in {OrganizationId}",packId,id,organizationId);return id;}
}
public sealed class SolutionPackRollbackService(ISolutionPackRepository repository){public Task<bool> Rollback(Guid installationId,Guid organizationId,Guid actor,CancellationToken ct)=>repository.RollbackAsync(SolutionPackService.Required(installationId),SolutionPackService.Required(organizationId),SolutionPackService.Required(actor),ct);}
public sealed class SolutionPackCatalogService(ISolutionPackRepository repository){public Task<IReadOnlyList<UpdateDto>> Updates(Guid organizationId,CancellationToken ct)=>repository.UpdatesAsync(SolutionPackService.Required(organizationId),ct);}
