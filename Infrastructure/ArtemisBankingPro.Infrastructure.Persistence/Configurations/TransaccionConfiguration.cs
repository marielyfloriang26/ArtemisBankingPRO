using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class TransaccionConfiguration : IEntityTypeConfiguration<Transaccion>
{
    public void Configure(EntityTypeBuilder<Transaccion> builder)
    {
        builder.ToTable("Transacciones");

        builder.Property(t => t.Monto)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.TipoTransaccion)
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(t => t.Origen)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Beneficiario)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Estado)
            .HasMaxLength(15)
            .HasDefaultValue("APROBADA")
            .IsRequired();

        builder.HasOne(t => t.CuentaOrigen)
            .WithMany(c => c.TransaccionesOrigen)
            .HasForeignKey(t => t.CuentaOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CuentaDestino)
            .WithMany(c => c.TransaccionesDestino)
            .HasForeignKey(t => t.CuentaDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.UsuarioResponsable)
            .WithMany(u => u.TransaccionesIniciadas)
            .HasForeignKey(t => t.UsuarioResponsableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
