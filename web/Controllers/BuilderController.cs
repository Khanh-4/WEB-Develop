using Microsoft.AspNetCore.Mvc;
using TechSpecs.Services;
using TechSpecs.ViewModels.Builder;

namespace TechSpecs.Controllers;

public class BuilderController : Controller
{
    private readonly ICompatibilityEngine _engine;

    public BuilderController(ICompatibilityEngine engine) => _engine = engine;

    // GET /Builder — main PC builder page (loads with empty state)
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var initialResult = await _engine.FilterAsync(new BuildState());
        return View(initialResult);
    }

    // POST /Builder/Filter — AJAX endpoint called on every component selection
    [HttpPost]
    public async Task<IActionResult> Filter([FromBody] BuildState state)
    {
        if (state is null)
            return BadRequest();

        var result = await _engine.FilterAsync(state);
        return Json(result);
    }
}
