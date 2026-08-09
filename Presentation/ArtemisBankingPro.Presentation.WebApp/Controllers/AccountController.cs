using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Application.ViewModels.Account;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        public AccountController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            // Mensaje requerido estrictamente
            ViewData["ErrorMessage"] = "No posee permisos para acceder a esta sección.";
            return View();
        }

        [HttpGet]
        public IActionResult Login(string? message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                ModelState.AddModelError(string.Empty, message);
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirigirSegunRolAutenticado();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(string.Empty, "Los datos de acceso son inválidos.");
                return View(model);
            }

            if (!user.EsActivo)
            {
                ModelState.AddModelError(string.Empty, "Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace enviado a su correo electrónico registrado para poder acceder al sistema.");
                return View(model);
            }

            if (user.TipoUsuario == "Comercio" || 
                (user.TipoUsuario != "Administrador" && user.TipoUsuario != "Cajero" && user.TipoUsuario != "Cliente"))
            {
                ModelState.AddModelError(string.Empty, "Este usuario no tiene permisos para acceder a la aplicación web.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirigirSegunRol(user.TipoUsuario);
            }

            ModelState.AddModelError(string.Empty, "Los datos de acceso son inválidos.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ActivarCuenta(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "El enlace de activación no es válido.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "El enlace de activación no es válido.";
                return RedirectToAction(nameof(Login));
            }

            if (user.EsActivo)
            {
                TempData["Error"] = "Este enlace de activación ya fue utilizado.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                user.EsActivo = true;
                await _userManager.UpdateAsync(user);

                TempData["Success"] = "Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }

            TempData["Error"] = "El enlace de activación no es válido.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarRestablecimiento(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "El nombre de usuario es requerido." });

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return Json(new { success = false, message = "No existe un usuario registrado con este nombre de usuario." });
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                return Json(new { success = false, message = "Este usuario no tiene un correo electrónico registrado. No es posible enviar la solicitud de restablecimiento." });
            }

            // Desactivar temporalmente la cuenta del usuario
            user.EsActivo = false;
            await _userManager.UpdateAsync(user);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Aquí puedes integrar el envío del correo electrónico

            return Json(new { success = true, message = "Se ha enviado un enlace de restablecimiento de contraseña al correo electrónico registrado." });
        }

        [HttpGet]
        public IActionResult NuevaContrasena(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "El enlace de restablecimiento no es válido.";
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaContrasena(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "El enlace de restablecimiento no es válido.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                // Activar nuevamente la cuenta tras el cambio exitoso
                user.EsActivo = true;
                await _userManager.UpdateAsync(user);

                TempData["Success"] = "Su contraseña ha sido restablecida correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }

            ModelState.AddModelError(string.Empty, "El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirigirSegunRolAutenticado()
        {
            if (User.IsInRole("Administrador"))
                return RedirectToAction("Index", "AdminHome");
            if (User.IsInRole("Cajero"))
                return RedirectToAction("Index", "CajeroHome");
            if (User.IsInRole("Cliente"))
                return RedirectToAction("Index", "ClienteHome");

            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirigirSegunRol(string tipoUsuario)
        {
            return tipoUsuario switch
            {
                "Administrador" => RedirectToAction("Index", "AdminHome"),
                "Cajero" => RedirectToAction("Index", "CajeroHome"),
                "Cliente" => RedirectToAction("Index", "ClienteHome"),
                _ => RedirectToAction(nameof(Login))
            };
        }
    }
}