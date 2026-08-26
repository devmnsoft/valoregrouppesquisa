using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class CertificatesController(ILogger<CertificatesController> logger) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Certificados";
        return View();
    }

    [Route("certificado/{certificateId}")]
    [Route("public/results/{certificateId}/certificate")]
    public IActionResult Public(string certificateId)
    {
        try
        {
            ViewData["Title"] = "Certificado";
            ViewData["ResponseId"] = certificateId;
            return View("Details");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar CertificatesController.Public no Valora.Web.");
            throw;
        }
    }

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
    [Route("certificado/validar/{certificateCode?}")]
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
