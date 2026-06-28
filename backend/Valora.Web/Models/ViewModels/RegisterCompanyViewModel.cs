namespace Valora.Web.Models.ViewModels;

public sealed class RegisterCompanyViewModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string ResponsibleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
