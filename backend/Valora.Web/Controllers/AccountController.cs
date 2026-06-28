using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class AccountController : Controller
{
    public IActionResult Login() => View();
    public IActionResult Register() => View();
    public IActionResult ForgotPassword() => View();
    public IActionResult ResetPassword() => View();
}
