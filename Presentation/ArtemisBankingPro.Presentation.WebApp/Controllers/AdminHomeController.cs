using ArtemisBankingPro.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminHomeController : Controller
{
    private readonly IAdminService _adminService;

    public AdminHomeController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _adminService.GetDashboardIndicatorsAsync();
        return View(model);
    }
}