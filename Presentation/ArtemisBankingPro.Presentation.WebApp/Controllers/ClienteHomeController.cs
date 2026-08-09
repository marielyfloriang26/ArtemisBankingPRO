using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class ClienteHomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}