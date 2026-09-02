using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class OrganizationController(ILogger<OrganizationController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Organization";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar OrganizationController.Index no Valora.Web.");
            throw;
        }
    }

    [HttpGet("Organization/Structure")]
    public IActionResult Structure() => Redirect("/Organization#org-structure");

    [HttpGet("Onboarding")]
    public IActionResult Onboarding() => Redirect("/Organization#org-onboarding");

    [ValidateAntiForgeryToken, HttpPost("Organization/Select")]
    public IActionResult Select(Guid? organizationId, string? returnUrl)
    {
        if (organizationId is null || organizationId == Guid.Empty)
            Response.Cookies.Delete("Valora.OrganizationId");
        else
            Response.Cookies.Append("Valora.OrganizationId", organizationId.Value.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromHours(8)
            });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Dashboard");
    }
}
