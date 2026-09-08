using System.ComponentModel.DataAnnotations;

namespace Valora.Web.Models.ViewModels;

public sealed class RequestModuleUpgradeViewModel
{
    [Required, StringLength(80)] public string ModuleCode { get; set; } = string.Empty;
    [Required(ErrorMessage = "Conte como o módulo será utilizado."), StringLength(1000, MinimumLength = 10)] public string Reason { get; set; } = string.Empty;
}
