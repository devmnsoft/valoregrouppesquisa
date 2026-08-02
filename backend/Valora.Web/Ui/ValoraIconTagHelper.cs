using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Valora.Web.Ui;

[HtmlTargetElement("valora-icon")]
public sealed class ValoraIconTagHelper(ValoraIconRegistry registry) : TagHelper
{
    [HtmlAttributeName("name")] public required string Name { get; set; }
    [HtmlAttributeName("size")] public int Size { get; set; } = 20;
    [HtmlAttributeName("stroke-width")] public decimal StrokeWidth { get; set; } = 1.75m;
    [HtmlAttributeName("class")] public string? CssClass { get; set; }
    [HtmlAttributeName("title")] public string? Title { get; set; }
    [HtmlAttributeName("decorative")] public bool Decorative { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!registry.TryGet(Name, out var paths)) throw new InvalidOperationException($"Ícone Valora desconhecido: {Name}");
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
        if (Decorative)
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
            output.Attributes.SetAttribute("focusable", "false");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Title))
                throw new InvalidOperationException($"O ícone informativo '{Name}' precisa de um título acessível ou decorative=\"true\".");
            output.Attributes.SetAttribute("role", "img");
            output.Content.AppendHtml($"<title>{System.Net.WebUtility.HtmlEncode(Title)}</title>");
        }
        output.Content.AppendHtml(paths);
    }
}
