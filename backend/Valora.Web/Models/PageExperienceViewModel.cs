namespace Valora.Web.Models;

public sealed record PageExperienceViewModel(string Purpose, string WhenToUse, string NextStep, string Care,
    string PermissionNote, string? SuccessMessage, string? ErrorMessage, string? WarningMessage, string? InformationMessage);
