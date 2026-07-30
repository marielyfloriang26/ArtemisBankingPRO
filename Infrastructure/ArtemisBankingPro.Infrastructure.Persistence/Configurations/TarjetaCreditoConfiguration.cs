using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class TarjetaCreditoConfiguration : IEntityTypeConfiguration<TarjetaCredito>
{
    public void Configure(EntityTypeBuilder<TarjetaCredito> builder)
    {
        builder.ToTable("TarjetasCredito");

        builder.Property(tc => tc.LimiteCredito)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(tc => tc.MontoAdeudado)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(tc => tc.FechaExpiracion)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(tc => tc.CVC)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(tc => tc.Estado)
            .HasMaxLength(15)
            .HasDefaultValue("Activa")
            .IsRequired();

        builder.HasOne(tc => tc.Cliente)
            .WithMany(u => u.TarjetasCliente)
            .HasForeignKey(tc => tc.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tc => tc.Admin)
            .WithMany(u => u.TarjetasAutorizadas)
            .HasForeignKey(tc => tc.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
