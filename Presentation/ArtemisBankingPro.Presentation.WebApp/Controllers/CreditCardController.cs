using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.CreditCards;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers;

public class CreditCardController : Controller
{
    private readonly ICreditCardService _creditCardService;

    public CreditCardController(ICreditCardService creditCardService)
    {
        _creditCardService = creditCardService;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _creditCardService.GetAllViewModel();
        return View(list);
    }

    public IActionResult Create()
    {
        return View(new SaveCreditCardViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveCreditCardViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        try
        {
            await _creditCardService.Add(vm);
            TempData["SuccessMessage"] = "Tarjeta de crédito creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["ErrorMessage"] = "Ocurrió un error al intentar crear la tarjeta de crédito. Por favor, intente de nuevo.";
            return View(vm);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _creditCardService.GetByIdSaveViewModel(id);
        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, SaveCreditCardViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        try
        {
            await _creditCardService.Update(vm, id);
            TempData["SuccessMessage"] = "Tarjeta de crédito actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["ErrorMessage"] = "Ocurrió un error al intentar actualizar la tarjeta de crédito.";
            return View(vm);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id)
    {
        try
        {
            var vm = await _creditCardService.GetByIdSaveViewModel(id);
            if (vm == null) return NotFound();
            
            await _creditCardService.ChangeStatus(id);

            // Fetch the updated entity state conceptually (since we just toggled it)
            // But we can check what it was before to know the success message
            var list = await _creditCardService.GetAllViewModel();
            var updated = list.Find(x => x.Id == id);
            
            if (updated!.Estado == "Inactiva")
                TempData["SuccessMessage"] = "La tarjeta ha sido desactivada exitosamente.";
            else
                TempData["SuccessMessage"] = "La tarjeta ha sido activada exitosamente.";

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["ErrorMessage"] = "Ocurrió un error al intentar cambiar el estado de la tarjeta.";
            return RedirectToAction(nameof(Index));
        }
    }
}
