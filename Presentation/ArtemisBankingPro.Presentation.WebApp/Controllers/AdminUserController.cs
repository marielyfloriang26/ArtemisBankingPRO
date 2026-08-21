using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Administrador")]
public class AdminUserController : Controller
{
    private readonly UserManager<Usuario> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public AdminUserController(UserManager<Usuario> userManager, ApplicationDbContext context, IEmailService emailService)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
    }

    // GET: AdminUser/Index
    public async Task<IActionResult> Index(string rolFiltro, int page = 1)
    {
        int pageSize = 20;

        var query = _userManager.Users
            .Where(u => u.TipoUsuario != "Comercio")
            .OrderByDescending(u => u.Id);

        if (!string.IsNullOrEmpty(rolFiltro) && rolFiltro != "Todos")
        {
            query = (IOrderedQueryable<Usuario>)query.Where(u => u.TipoUsuario == rolFiltro);
        }

        int totalUsers = await query.CountAsync();
        var usuarios = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.RolFiltro = rolFiltro ?? "Todos";
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

        return View(usuarios);
    }

    // POST: AdminUser/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string nombre, string apellido, string cedula, string email, string userName, string password, string confirmPassword, string tipoUsuario, decimal? montoInicial)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) || string.IsNullOrWhiteSpace(cedula) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Todos los campos obligatorios deben ser completados.";
            return RedirectToAction(nameof(Index));
        }

        if (password != confirmPassword)
        {
            TempData["Error"] = "La contraseña y la confirmación de contraseña deben coincidir.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(tipoUsuario) || (tipoUsuario != "Administrador" && tipoUsuario != "Cajero" && tipoUsuario != "Cliente"))
        {
            TempData["Error"] = "El tipo de usuario debe ser Administrador, Cajero o Cliente.";
            return RedirectToAction(nameof(Index));
        }

        if (tipoUsuario == "Cliente" && montoInicial.HasValue && montoInicial < 0)
        {
            TempData["Error"] = "El monto inicial no puede ser negativo.";
            return RedirectToAction(nameof(Index));
        }

        if (tipoUsuario != "Cliente" && montoInicial.HasValue && montoInicial > 0)
        {
            TempData["Error"] = "Solo los clientes pueden tener un monto inicial asignado.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.Users.AnyAsync(u => u.Cedula == cedula))
        {
            TempData["Error"] = "Ya existe un usuario registrado con esta cédula.";
            return RedirectToAction(nameof(Index));
        }
        if (await _userManager.Users.AnyAsync(u => u.Email == email))
        {
            TempData["Error"] = "Ya existe un usuario registrado con este correo electrónico.";
            return RedirectToAction(nameof(Index));
        }
        if (await _userManager.Users.AnyAsync(u => u.UserName == userName))
        {
            TempData["Error"] = "Ya existe un usuario registrado con este nombre de usuario.";
            return RedirectToAction(nameof(Index));
        }

        var nuevoUsuario = new Usuario
        {
            Nombre = nombre,
            Apellido = apellido,
            Cedula = cedula,
            Email = email,
            UserName = userName,
            TipoUsuario = tipoUsuario,
            EsActivo = false
        };

        var result = await _userManager.CreateAsync(nuevoUsuario, password);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _userManager.AddToRoleAsync(nuevoUsuario, tipoUsuario);

        if (tipoUsuario == "Cliente")
        {
            decimal saldoInicial = montoInicial ?? 0.00m;
            string numeroCuenta;
            
            Random rnd = new Random();
            do
            {
                numeroCuenta = rnd.Next(100000000, 1000000000).ToString();
            } while (await _context.Set<CuentaAhorro>().AnyAsync(c => c.NumeroCuenta == numeroCuenta) ||
                     await _context.Set<Prestamo>().AnyAsync(p => p.NumeroPrestamo == numeroCuenta));

            var cuentaPrincipal = new CuentaAhorro
            {
                NumeroCuenta = numeroCuenta,
                ClienteId = nuevoUsuario.Id,
                Balance = saldoInicial,
                TipoCuenta = "Principal",
                Estado = "Activa",
                FechaCreacion = DateTime.Now
            };

            _context.Add(cuentaPrincipal);
            await _context.SaveChangesAsync();

            if (saldoInicial > 0)
            {
                var transaccion = new Transaccion
                {
                    CuentaDestinoId = cuentaPrincipal.Id,
                    Monto = saldoInicial,
                    TipoTransaccion = "CRÉDITO",
                    Origen = "APERTURA",
                    Beneficiario = numeroCuenta,
                    Estado = "APROBADA",
                    FechaTransaccion = DateTime.Now
                };
                _context.Add(transaccion);
                await _context.SaveChangesAsync();
            }
        }

        try
        {
            string token = await _userManager.GenerateEmailConfirmationTokenAsync(nuevoUsuario);
            string? enlace = Url.Action("ActivarCuenta", "Account", new { userId = nuevoUsuario.Id, token = token }, Request.Scheme);
            string cuerpo = $"Hola {nombre},\n\nSu cuenta ha sido creada correctamente en Artemis Banking.\nPara activar su usuario, haga clic en el siguiente enlace:\n{enlace}\n\nSi usted no esperaba la creación de esta cuenta, ignore este mensaje.";
            
            await _emailService.SendEmailAsync(email, "Activación de cuenta", cuerpo);
        }
        catch
        {
            TempData["Error"] = "No fue posible enviar el correo de activación. Intente nuevamente más tarde.";
        }

        TempData["Success"] = "Usuario creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // POST: AdminUser/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var usuarioActualId = int.Parse(_userManager.GetUserId(User) ?? "0");
        if (id == usuarioActualId)
        {
            TempData["Error"] = "No puede modificar el estado de su propia cuenta.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario == null)
        {
            TempData["Error"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(Index));
        }

        usuario.EsActivo = !usuario.EsActivo;
        await _userManager.UpdateAsync(usuario);

        TempData["Success"] = "El estado del usuario ha sido actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: AdminUser/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var usuarioActualId = int.Parse(_userManager.GetUserId(User) ?? "0");
        if (id == usuarioActualId)
        {
            TempData["Error"] = "No puede editar su propia cuenta desde este módulo.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario == null)
        {
            TempData["Error"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(Index));
        }

        return View(usuario);
    }

    // POST: AdminUser/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string nombre, string apellido, string cedula, string email, string userName, string password, string confirmPassword, decimal? montoAdicional)
    {
        var usuarioActualId = int.Parse(_userManager.GetUserId(User) ?? "0");
        if (id == usuarioActualId)
        {
            TempData["Error"] = "No puede editar su propia cuenta desde este módulo.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) || string.IsNullOrWhiteSpace(cedula) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName))
        {
            TempData["Error"] = "Todos los campos obligatorios deben ser completados.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        if (montoAdicional.HasValue && montoAdicional < 0)
        {
            TempData["Error"] = "El monto adicional no puede ser negativo.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario == null)
        {
            TempData["Error"] = "El usuario seleccionado no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.Users.AnyAsync(u => u.Id != id && u.Cedula == cedula))
        {
            TempData["Error"] = "Ya existe otro usuario registrado con esta cédula.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        if (await _userManager.Users.AnyAsync(u => u.Id != id && u.Email == email))
        {
            TempData["Error"] = "Ya existe otro usuario registrado con este correo electrónico.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        if (await _userManager.Users.AnyAsync(u => u.Id != id && u.UserName == userName))
        {
            TempData["Error"] = "Ya existe otro usuario registrado con este nombre de usuario.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        if (!string.IsNullOrEmpty(password))
        {
            if (string.IsNullOrEmpty(confirmPassword))
            {
                TempData["Error"] = "Debe confirmar la nueva contraseña.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            if (password != confirmPassword)
            {
                TempData["Error"] = "La contraseña y la confirmación de contraseña deben coincidir.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            await _userManager.ResetPasswordAsync(usuario, token, password);
        }

        usuario.Nombre = nombre;
        usuario.Apellido = apellido;
        usuario.Cedula = cedula;
        usuario.Email = email;
        usuario.UserName = userName;

        await _userManager.UpdateAsync(usuario);

        if (usuario.TipoUsuario == "Cliente" && montoAdicional.HasValue && montoAdicional > 0)
        {
            var cuentaPrincipal = await _context.Set<CuentaAhorro>()
                .FirstOrDefaultAsync(c => c.ClienteId == usuario.Id && c.TipoCuenta == "Principal");

            if (cuentaPrincipal != null)
            {
                cuentaPrincipal.Balance += montoAdicional.Value;
                
                var transaccion = new Transaccion
                {
                    CuentaDestinoId = cuentaPrincipal.Id,
                    Monto = montoAdicional.Value,
                    TipoTransaccion = "CRÉDITO",
                    Origen = "AJUSTE ADMINISTRATIVO",
                    Beneficiario = cuentaPrincipal.NumeroCuenta,
                    Estado = "APROBADA",
                    FechaTransaccion = DateTime.Now
                };
                _context.Add(transaccion);
                await _context.SaveChangesAsync();
            }
        }

        TempData["Success"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}