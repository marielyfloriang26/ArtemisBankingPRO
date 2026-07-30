using ArtemisBankingPro.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

        // Nombres de tablas
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<CuentaAhorro>().ToTable("CuentasAhorro");
        modelBuilder.Entity<Prestamo>().ToTable("Prestamos");
        modelBuilder.Entity<CuotaPrestamo>().ToTable("CuotasPrestamo");
        modelBuilder.Entity<TarjetaCredito>().ToTable("TarjetasCredito");
        modelBuilder.Entity<ConsumoTarjeta>().ToTable("ConsumosTarjeta");
        modelBuilder.Entity<Comercio>().ToTable("Comercios");
        modelBuilder.Entity<ComercioUsuario>().ToTable("ComercioUsuarios");
        modelBuilder.Entity<Beneficiario>().ToTable("Beneficiarios");
        modelBuilder.Entity<Transaccion>().ToTable("Transacciones");

        // PK Compuesta para ComercioUsuario
        modelBuilder.Entity<ComercioUsuario>()
            .HasKey(cu => new { cu.ComercioId, cu.UsuarioId });

        // Unicidad para ComercioUsuario.UsuarioId
        modelBuilder.Entity<ComercioUsuario>()
            .HasIndex(cu => cu.UsuarioId)
            .IsUnique();

        // Precisión para campos decimales
        modelBuilder.Entity<CuentaAhorro>()
            .Property(c => c.Balance)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Prestamo>()
            .Property(p => p.MontoAprobado)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Prestamo>()
            .Property(p => p.TasaInteresAnual)
            .HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Prestamo>()
            .Property(p => p.MontoPendiente)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<CuotaPrestamo>()
            .Property(cp => cp.ValorCuota)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CuotaPrestamo>()
            .Property(cp => cp.MontoInteres)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CuotaPrestamo>()
            .Property(cp => cp.MontoCapital)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CuotaPrestamo>()
            .Property(cp => cp.SaldoPendiente)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<TarjetaCredito>()
            .Property(tc => tc.LimiteCredito)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<TarjetaCredito>()
            .Property(tc => tc.MontoAdeudado)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ConsumoTarjeta>()
            .Property(ct => ct.Monto)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Transaccion>()
            .Property(t => t.Monto)
            .HasColumnType("decimal(18,2)");

        // Configuración de Relaciones y DeleteBehavior.Restrict para evitar ciclos de cascada
        modelBuilder.Entity<CuentaAhorro>()
            .HasOne(c => c.Cliente)
            .WithMany(u => u.CuentasAhorro)
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prestamo>()
            .HasOne(p => p.Cliente)
            .WithMany(u => u.PrestamosCliente)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prestamo>()
            .HasOne(p => p.Admin)
            .WithMany(u => u.PrestamosAutorizados)
            .HasForeignKey(p => p.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CuotaPrestamo>()
            .HasOne(cp => cp.Prestamo)
            .WithMany(p => p.Cuotas)
            .HasForeignKey(cp => cp.PrestamoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TarjetaCredito>()
            .HasOne(tc => tc.Cliente)
            .WithMany(u => u.TarjetasCliente)
            .HasForeignKey(tc => tc.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TarjetaCredito>()
            .HasOne(tc => tc.Admin)
            .WithMany(u => u.TarjetasAutorizadas)
            .HasForeignKey(tc => tc.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConsumoTarjeta>()
            .HasOne(ct => ct.Tarjeta)
            .WithMany(t => t.Consumos)
            .HasForeignKey(ct => ct.TarjetaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comercio>()
            .HasOne(c => c.Admin)
            .WithMany(u => u.ComerciosRegistrados)
            .HasForeignKey(c => c.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComercioUsuario>()
            .HasOne(cu => cu.Comercio)
            .WithOne(c => c.ComercioUsuarioRel)
            .HasForeignKey<ComercioUsuario>(cu => cu.ComercioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ComercioUsuario>()
            .HasOne(cu => cu.Usuario)
            .WithOne()
            .HasForeignKey<ComercioUsuario>(cu => cu.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Beneficiario>()
            .HasOne(b => b.Cliente)
            .WithMany(u => u.Beneficiarios)
            .HasForeignKey(b => b.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Beneficiario>()
            .HasOne(b => b.CuentaAhorro)
            .WithMany(c => c.BeneficiariosAsociados)
            .HasForeignKey(b => b.CuentaAhorroId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaccion>()
            .HasOne(t => t.CuentaOrigen)
            .WithMany(c => c.TransaccionesOrigen)
            .HasForeignKey(t => t.CuentaOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaccion>()
            .HasOne(t => t.CuentaDestino)
            .WithMany(c => c.TransaccionesDestino)
            .HasForeignKey(t => t.CuentaDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaccion>()
            .HasOne(t => t.UsuarioResponsable)
            .WithMany(u => u.TransaccionesIniciadas)
            .HasForeignKey(t => t.UsuarioResponsableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
