using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class ConsumoTarjetaConfiguration : IEntityTypeConfiguration<ConsumoTarjeta>
{
    public void Configure(EntityTypeBuilder<ConsumoTarjeta> builder)
    {
        builder.ToTable("ConsumosTarjeta");

        builder.Property(ct => ct.Monto)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(ct => ct.Comercio)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ct => ct.Estado)
            .HasMaxLength(15)
            .IsRequired();

        builder.HasOne(ct => ct.Tarjeta)
            .WithMany(t => t.Consumos)
            .HasForeignKey(ct => ct.TarjetaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
