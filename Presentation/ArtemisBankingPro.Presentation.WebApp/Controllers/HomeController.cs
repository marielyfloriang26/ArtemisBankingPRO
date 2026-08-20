using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers;

public class HomeController : Controller
{
    [Authorize(Roles = "Cliente")]
    public IActionResult Cliente()
    {
        return View();
    }

    [Authorize(Roles = "Cajero")]
    public IActionResult Cajero()
    {
        return View();
    }
}
