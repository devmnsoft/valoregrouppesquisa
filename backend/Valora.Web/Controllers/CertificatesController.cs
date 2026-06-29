using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class CertificatesController(ILogger<CertificatesController> logger) : Controller
{
    public IActionResult Details(string id)
    {
        try
        {
            ViewData["Title"] = "Details";
            ViewData["ResponseId"] = id;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar CertificatesController.Details no Valora.Web.");
            throw;
        }
    }
    [Route("Certificates/Validate/{certificateCode?}")]
    public IActionResult Validate(string? certificateCode)
    {
        try
        {
            ViewData["Title"] = "Validate";
            ViewData["CertificateCode"] = certificateCode;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar CertificatesController.Validate no Valora.Web.");
            throw;
        }
    }
}
