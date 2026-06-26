using Microsoft.AspNetCore.Mvc;
using Valora.Application.Certificates;
using Valora.Application.Communication;
using Valora.Application.Results;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class CoreController : ControllerBase
{
    [HttpPost("/auth/login")] public IActionResult Login() => Accepted(new { status = "planned", auth = "JWT or Firebase token bridge" });
    [HttpPost("/auth/register-company")] public IActionResult RegisterCompany() => Accepted(new { status = "planned" });
    [HttpPost("/auth/forgot-password")] public IActionResult ForgotPassword() => Accepted(new { status = "planned" });
    [HttpGet("/me")] public IActionResult Me() => Ok(new { status = "planned", tenant = "organization_id" });
    [HttpGet("/plans")] public IActionResult Plans() => Ok(new[] { "free", "essential", "professional", "corporate", "enterprise" });
    [HttpGet("/organizations")] public IActionResult Organizations() => Ok(Array.Empty<object>());
    [HttpGet("/surveys")] public IActionResult Surveys() => Ok(Array.Empty<object>());
    [HttpPost("/surveys")] public IActionResult CreateSurvey() => Accepted(new { status = "planned" });
    [HttpGet("/surveys/public/{token}")] public IActionResult PublicSurvey(string token) => Ok(new { token, status = "planned" });
    [HttpPost("/surveys/{id:guid}/responses")] public IActionResult SaveResponse(Guid id, [FromServices] ValoraInsightCalculator calculator) => Accepted(new { surveyId = id, result = calculator.Calculate(Array.Empty<AnswerScore>()) });
    [HttpGet("/responses/{id:guid}")] public IActionResult Response(Guid id) => Ok(new { id, status = "planned" });
    [HttpGet("/responses/{id:guid}/result")] public IActionResult Result(Guid id) => Ok(new { id, status = "planned" });
    [HttpGet("/responses/{id:guid}/certificate.pdf")] public IActionResult CertificatePdf(Guid id, [FromServices] CertificateService service) => Ok(service.Plan(id, "pdf"));
    [HttpGet("/responses/{id:guid}/certificate.png")] public IActionResult CertificatePng(Guid id, [FromServices] CertificateService service) => Ok(service.Plan(id, "png"));
    [HttpPost("/communications/result/send")] public IActionResult SendResult([FromServices] EmailJobService jobs) => Accepted(jobs.EnqueueResultEmailPlan());
    [HttpGet("/communications")] public IActionResult Communications() => Ok(Array.Empty<object>());
    [HttpPost("/communications/{id:guid}/retry")] public IActionResult Retry(Guid id) => Accepted(new { id, status = "retry_scheduled" });
}
