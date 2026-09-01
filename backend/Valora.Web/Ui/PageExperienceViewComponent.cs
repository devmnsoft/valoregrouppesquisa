using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Ui;

public sealed class PageExperienceViewComponent(PageExperienceCatalog catalog) : ViewComponent
{
    public IViewComponentResult Invoke() => View(catalog.Create(RouteData.Values["controller"]?.ToString() ?? string.Empty, ViewContext.TempData));
}
