using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Valora.Web.Ui;

[HtmlTargetElement("valora-icon")]
public sealed class ValoraIconTagHelper(
    ValoraIconRegistry registry,
    ILogger<ValoraIconTagHelper> logger,
    IWebHostEnvironment environment) : TagHelper
{
    [HtmlAttributeName("name")] public string? Name { get; set; }
    [HtmlAttributeName("size")] public int Size { get; set; } = 20;
    [HtmlAttributeName("stroke-width")] public decimal StrokeWidth { get; set; } = 1.75m;
    [HtmlAttributeName("class")] public string? CssClass { get; set; }
    [HtmlAttributeName("title")] public string? Title { get; set; }
    [HtmlAttributeName("decorative")] public bool Decorative { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var resolvedName = registry.GetOrFallback(Name);
        if (!registry.Contains(Name) && environment.IsDevelopment())
        {
            logger.LogWarning("Ícone Valora desconhecido: {IconName}. Renderizando fallback {FallbackIcon}.",
                string.IsNullOrWhiteSpace(Name) ? "(vazio)" : Name, resolvedName);
        }
        var paths = registry.GetRequired(resolvedName);
        var size = Math.Clamp(Size, 12, 64);
        output.TagName = "svg";
        var cssClass = string.Join(' ', new[] { "valora-icon", CssClass }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        output.Attributes.SetAttribute("class", cssClass);
        output.Attributes.SetAttribute("width", size);
        output.Attributes.SetAttribute("height", size);
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", Math.Clamp(StrokeWidth, 0.5m, 4m).ToString(System.Globalization.CultureInfo.InvariantCulture));
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        output.Attributes.SetAttribute("data-valora-icon", resolvedName);
        if (!string.Equals(Name, resolvedName, StringComparison.OrdinalIgnoreCase))
            output.Attributes.SetAttribute("data-icon-fallback", "true");
        if (Decorative)
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
            output.Attributes.SetAttribute("focusable", "false");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Title))
                throw new InvalidOperationException($"O ícone informativo '{resolvedName}' precisa de um título acessível ou decorative=\"true\".");
            output.Attributes.SetAttribute("role", "img");
            output.Content.AppendHtml($"<title>{System.Net.WebUtility.HtmlEncode(Title)}</title>");
        }
        output.Content.AppendHtml(paths);
    }
}
