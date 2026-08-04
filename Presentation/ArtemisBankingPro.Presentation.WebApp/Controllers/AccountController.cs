using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers;

public class AccountController : Controller
{
    public IActionResult AccessDenied()
    {
        return View();
    }
}
