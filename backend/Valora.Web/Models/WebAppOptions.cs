namespace Valora.Web.Models;

public sealed class WebAppOptions
{
    public string Name { get; set; } = "Valora Pulse";
    public string Version { get; set; } = "1.0.0-web";
    public string Environment { get; set; } = "Development";
    public string PublicUrl { get; set; } = "http://localhost:5088";
    public bool EnableDebugLogs { get; set; } = true;
}
