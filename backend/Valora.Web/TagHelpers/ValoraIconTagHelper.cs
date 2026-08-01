using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Valora.Web.TagHelpers;

[HtmlTargetElement("valora-icon")]
public sealed class ValoraIconTagHelper : TagHelper
{
    [HtmlAttributeName("name")]
    public required string Name { get; set; }

    [HtmlAttributeName("size")]
    public int Size { get; set; } = 20;

    [HtmlAttributeName("title")]
    public string? Title { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!ValoraIconRegistry.TryGet(Name, out var paths))
            throw new InvalidOperationException($"O ícone Valora '{Name}' não está registrado.");
        if (Size is < 12 or > 64)
            throw new ArgumentOutOfRangeException(nameof(Size), "O tamanho deve estar entre 12 e 64 pixels.");

        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"valora-icon valora-icon--{HtmlEncoder.Default.Encode(Name)}");
        output.Attributes.SetAttribute("width", Size);
        output.Attributes.SetAttribute("height", Size);
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "1.75");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        if (string.IsNullOrWhiteSpace(Title)) output.Attributes.SetAttribute("aria-hidden", "true");
        else { output.Attributes.SetAttribute("role", "img"); output.Content.AppendHtml($"<title>{HtmlEncoder.Default.Encode(Title)}</title>"); }
        output.Content.AppendHtml(paths);
    }
}
