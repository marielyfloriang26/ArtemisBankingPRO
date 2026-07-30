using ArtemisBankingPro.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ArtemisBankingPro.Infrastructure.Persistence.Contexts;

public class ApplicationDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<CuentaAhorro> CuentasAhorro { get; set; }
    public DbSet<Prestamo> Prestamos { get; set; }
    public DbSet<CuotaPrestamo> CuotasPrestamo { get; set; }
    public DbSet<TarjetaCredito> TarjetasCredito { get; set; }
    public DbSet<ConsumoTarjeta> ConsumosTarjeta { get; set; }
    public DbSet<Comercio> Comercios { get; set; }
    public DbSet<ComercioUsuario> ComercioUsuarios { get; set; }
    public DbSet<Beneficiario> Beneficiarios { get; set; }
    public DbSet<Transaccion> Transacciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas las configuraciones de IEntityTypeConfiguration encontradas en este ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
