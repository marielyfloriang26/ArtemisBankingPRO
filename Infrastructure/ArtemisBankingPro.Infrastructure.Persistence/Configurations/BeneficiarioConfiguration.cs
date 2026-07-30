using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class BeneficiarioConfiguration : IEntityTypeConfiguration<Beneficiario>
{
    public void Configure(EntityTypeBuilder<Beneficiario> builder)
    {
        builder.ToTable("Beneficiarios");

        builder.Property(b => b.Nombre)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Apellido)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(b => b.Cliente)
            .WithMany(u => u.Beneficiarios)
            .HasForeignKey(b => b.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CuentaAhorro)
            .WithMany(c => c.BeneficiariosAsociados)
            .HasForeignKey(b => b.CuentaAhorroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
