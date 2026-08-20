using Microsoft.AspNetCore.Identity;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Application;
using ArtemisBankingPro.Infrastructure.Persistence;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using ArtemisBankingPro.Infrastructure.Shared;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Infrastructure.Persistence.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);

builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddIdentity<Usuario, IdentityRole<int>>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(30);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "ArtemisBankingPro.Account";
    
    // Redirigir al Login mostrando el mensaje exacto si no esta autenticado
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect(context.RedirectUri + (context.RedirectUri.Contains("?") ? "&" : "?") + "message=" + Uri.EscapeDataString("No tiene permiso para acceder a esta sección."));
        return Task.CompletedTask;
    };
});

builder.Services.AddHostedService<ArtemisBankingPro.Presentation.WebApp.BackgroundServices.CuotasAtrasadasBackgroundService>();

var app = builder.Build();

// Seeding de roles y usuarios por defecto activos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<Usuario>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        string[] roleNames = { "Administrador", "Cajero", "Cliente", "Comercio" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }

        // Administrador por defecto
        var adminUser = await userManager.FindByNameAsync("admin_default");
        if (adminUser == null)
        {
            adminUser = new Usuario
            {
                UserName = "admin_default",
                Email = "admin@artemis.com",
                Nombre = "Admin",
                Apellido = "Sistema",
                Cedula = "001-0000000-1",
                Telefono = "809-555-0001",
                EsActivo = true,
                EmailConfirmed = true,
                TipoUsuario = "Administrador"
            };
            var createAdmin = await userManager.CreateAsync(adminUser, "Admin123*");
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrador");
            }
        }

        // Cajero por defecto
        var cajeroUser = await userManager.FindByNameAsync("cajero_default");
        if (cajeroUser == null)
        {
            cajeroUser = new Usuario
            {
                UserName = "cajero_default",
                Email = "cajero@artemis.com",
                Nombre = "Cajero",
                Apellido = "Sistema",
                Cedula = "001-0000000-2",
                Telefono = "809-555-0002",
                EsActivo = true,
                EmailConfirmed = true,
                TipoUsuario = "Cajero"
            };
            var createCajero = await userManager.CreateAsync(cajeroUser, "Cajero123*");
            if (createCajero.Succeeded)
            {
                await userManager.AddToRoleAsync(cajeroUser, "Cajero");
            }
        }

        // Cliente por defecto
        var clienteUser = await userManager.FindByNameAsync("cliente_default");
        if (clienteUser == null)
        {
            clienteUser = new Usuario
            {
                UserName = "cliente_default",
                Email = "cliente@artemis.com",
                Nombre = "Cliente",
                Apellido = "Sistema",
                Cedula = "001-0000000-3",
                Telefono = "809-555-0003",
                EsActivo = true,
                EmailConfirmed = true,
                TipoUsuario = "Cliente"
            };
            var createCliente = await userManager.CreateAsync(clienteUser, "Cliente123*");
            if (createCliente.Succeeded)
            {
                await userManager.AddToRoleAsync(clienteUser, "Cliente");
            }
        }

        // Comercio por defecto
        var comercioUser = await userManager.FindByNameAsync("comercio_default");
        if (comercioUser == null)
        {
            comercioUser = new Usuario
            {
                UserName = "comercio_default",
                Email = "comercio@artemis.com",
                Nombre = "Comercio",
                Apellido = "Sistema",
                Cedula = "001-0000000-4",
                Telefono = "809-555-0004",
                EsActivo = true,
                EmailConfirmed = true,
                TipoUsuario = "Comercio"
            };
            var createComercio = await userManager.CreateAsync(comercioUser, "Comercio123*");
            if (createComercio.Succeeded)
            {
                await userManager.AddToRoleAsync(comercioUser, "Comercio");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al realizar el seeding de la base de datos.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
