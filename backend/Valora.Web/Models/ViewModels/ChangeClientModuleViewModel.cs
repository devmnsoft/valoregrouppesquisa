using System.ComponentModel.DataAnnotations;

namespace Valora.Web.Models.ViewModels;

public sealed class ChangeClientModuleViewModel {
    [Required] public Guid ClientId { get; set; }
    [Required, StringLength(80)] public string ModuleCode { get; set; } = string.Empty;
    [Required, RegularExpression("^(active|read_only|suspended|expired|cancelled)$")] public string Status { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o motivo da alteração."), StringLength(1000, MinimumLength = 10)] public string Reason { get; set; } = string.Empty;
}
