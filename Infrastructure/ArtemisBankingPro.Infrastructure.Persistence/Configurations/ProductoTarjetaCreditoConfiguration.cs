using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class ProductoTarjetaCreditoConfiguration : IEntityTypeConfiguration<ProductoTarjetaCredito>
{
    public void Configure(EntityTypeBuilder<ProductoTarjetaCredito> builder)
    {
        builder.ToTable("ProductosTarjetaCredito");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LimiteCredito)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.TasaInteres)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(x => x.CostoEmision)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Descripcion)
            .HasMaxLength(500);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasMaxLength(20);
    }
}
