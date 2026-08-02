using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Navigation;

namespace Valora.Web.Controllers;

[Authorize]
[ApiController]
[Route("bff/navigation")]
public sealed class BffNavigationController(NavigationService navigation) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NavigationViewModel>> Get(CancellationToken cancellationToken) =>
        Ok(await navigation.BuildAsync(HttpContext, cancellationToken));
}
