using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class CuentaAhorroConfiguration : IEntityTypeConfiguration<CuentaAhorro>
{
    public void Configure(EntityTypeBuilder<CuentaAhorro> builder)
    {
        builder.ToTable("CuentasAhorro");

        builder.Property(c => c.Balance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0.00m);

        builder.Property(c => c.TipoCuenta)
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(c => c.Estado)
            .HasMaxLength(15)
            .HasDefaultValue("Activa")
            .IsRequired();

        builder.HasOne(c => c.Cliente)
            .WithMany(u => u.CuentasAhorro)
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
