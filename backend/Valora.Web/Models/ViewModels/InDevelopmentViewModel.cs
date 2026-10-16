namespace Valora.Web.Models.ViewModels;

/// <summary>
/// Modelo explicativo canônico para páginas planejadas em desenvolvimento.
/// Não exibe botões fictícios ou datas sem aprovação, informando com clareza
/// o propósito, público, insumos, entregas e a alternativa funcional ativa.
/// </summary>
public sealed record InDevelopmentViewModel(
    string Title,
    string Description,
    string Purpose,
    string TargetAudience,
    string InputInformation,
    string ExpectedOutcome,
    string CurrentAlternative,
    string? AlternativeUrl = null,
    string? ReturnUrl = null);
