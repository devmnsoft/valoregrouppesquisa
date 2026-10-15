namespace Valora.Web.Models.ViewModels;

public sealed record InDevelopmentViewModel(
    string Title,
    string Description,
    string DetailedDescription,
    string DiagnosticJourneyRelation,
    string JourneyStage,
    string? ReturnUrl = null);
