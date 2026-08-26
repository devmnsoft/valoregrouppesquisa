using System.ComponentModel.DataAnnotations;

namespace Valora.Web.Models;

public sealed class PublicResultExperienceViewModel
{
    [Required, RegularExpression("^[a-fA-F0-9]{64}$")] public required string Token { get; init; }
}
