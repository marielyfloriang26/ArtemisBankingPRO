using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class ComercioConfiguration : IEntityTypeConfiguration<Comercio>
{
    public void Configure(EntityTypeBuilder<Comercio> builder)
    {
        builder.ToTable("Comercios");

        builder.Property(c => c.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Descripcion)
            .HasMaxLength(255);

        builder.Property(c => c.Correo)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(c => c.Correo)
            .IsUnique();

        builder.Property(c => c.Telefono)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.RNC)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.RNC)
            .IsUnique();

        builder.Property(c => c.EsActivo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne(c => c.Admin)
            .WithMany(u => u.ComerciosRegistrados)
            .HasForeignKey(c => c.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
