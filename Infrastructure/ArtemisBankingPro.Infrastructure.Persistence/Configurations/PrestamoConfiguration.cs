using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
{
    public void Configure(EntityTypeBuilder<Prestamo> builder)
    {
        builder.ToTable("Prestamos");

        builder.Property(p => p.MontoAprobado)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.TasaInteresAnual)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(p => p.MontoPendiente)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Estado)
            .HasMaxLength(15)
            .HasDefaultValue("Activo")
            .IsRequired();

        builder.HasOne(p => p.Cliente)
            .WithMany(u => u.PrestamosCliente)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Admin)
            .WithMany(u => u.PrestamosAutorizados)
            .HasForeignKey(p => p.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
