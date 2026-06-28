namespace Valora.Web.Models;

public sealed class ApiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5080";
    public int TimeoutMs { get; set; } = 20000;
}
