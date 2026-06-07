using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace TechSpecs.Controllers;

public class LanguageController : Controller
{
    // GET /Language/Set?culture=vi&returnUrl=/
    [HttpGet, HttpPost]
    public IActionResult Set(string culture, string returnUrl = "/")
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
        );
        return LocalRedirect(returnUrl);
    }
}
