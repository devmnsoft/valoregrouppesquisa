using System.ComponentModel.DataAnnotations;

namespace Valora.Web.Models;

public sealed class RespondentExperienceViewModel
{
    [Required, RegularExpression("^[a-fA-F0-9]{64}$")] public required string Token { get; init; }
    public required string Step { get; init; }
    public int ProgressPercent => Step switch { "questions" => 35, "review" => 90, "completed" => 100, _ => 0 };
}
