using Valora.Application.SolutionPacks;
namespace Valora.Web.Models.ViewModels;
public sealed record SolutionPackMarketplaceViewModel(IReadOnlyList<SolutionPackSummary> Packs,string? Segment,string? Category);
public sealed record SolutionPackInstallationsViewModel(IReadOnlyList<InstallationDto> Installations,IReadOnlyList<UpdateDto> Updates);
