using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class CertificatesController : Controller
{
    public IActionResult Details(string id) { ViewBag.ResponseId = id; return View(); }
    [Route("Certificates/Validate/{certificateCode?}")]
    public IActionResult Validate(string? certificateCode) { ViewBag.CertificateCode = certificateCode; return View(); }
}
