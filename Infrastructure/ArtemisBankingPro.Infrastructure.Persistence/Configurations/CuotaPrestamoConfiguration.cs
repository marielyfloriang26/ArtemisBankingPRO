using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class CuotaPrestamoConfiguration : IEntityTypeConfiguration<CuotaPrestamo>
{
    public void Configure(EntityTypeBuilder<CuotaPrestamo> builder)
    {
        builder.ToTable("CuotasPrestamo");

        builder.Property(cp => cp.ValorCuota)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(cp => cp.MontoInteres)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(cp => cp.MontoCapital)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(cp => cp.SaldoPendiente)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(cp => cp.EstadoPago)
            .HasMaxLength(20)
            .HasDefaultValue("Pendiente")
            .IsRequired();

        builder.HasOne(cp => cp.Prestamo)
            .WithMany(p => p.Cuotas)
            .HasForeignKey(cp => cp.PrestamoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
