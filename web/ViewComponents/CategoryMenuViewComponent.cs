using Microsoft.AspNetCore.Mvc;

namespace TechSpecs.ViewComponents;

public class CategoryMenuViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string renderAs = "Dropdown")
    {
        // Pass the render type (Dropdown or Vertical) to the view
        ViewBag.RenderAs = renderAs;
        return View();
    }
}
