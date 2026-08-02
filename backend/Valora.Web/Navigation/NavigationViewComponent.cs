using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Navigation;

public sealed class NavigationViewComponent(NavigationService navigation) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await navigation.BuildAsync(HttpContext, HttpContext.RequestAborted));
}
