using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Valora.Web.Ui;

[HtmlTargetElement("valora-icon")]
public sealed class ValoraIconTagHelper(ValoraIconRegistry registry) : TagHelper
{
    [HtmlAttributeName("name")] public required string Name { get; set; }
    [HtmlAttributeName("size")] public int Size { get; set; } = 20;
    [HtmlAttributeName("title")] public string? Title { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!registry.TryGet(Name, out var paths)) throw new InvalidOperationException($"Ícone Valora desconhecido: {Name}");
        var size = Math.Clamp(Size, 12, 64);
        output.TagName = "svg";
        output.Attributes.SetAttribute("class", "valora-icon");
        output.Attributes.SetAttribute("width", size);
        output.Attributes.SetAttribute("height", size);
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "1.75");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        if (string.IsNullOrWhiteSpace(Title)) output.Attributes.SetAttribute("aria-hidden", "true");
        else { output.Attributes.SetAttribute("role", "img"); output.Content.AppendHtml($"<title>{System.Net.WebUtility.HtmlEncode(Title)}</title>"); }
        output.Content.AppendHtml(paths);
    }
}
