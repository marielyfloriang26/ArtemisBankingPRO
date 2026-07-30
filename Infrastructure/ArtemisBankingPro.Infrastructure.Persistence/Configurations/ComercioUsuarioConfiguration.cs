using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class ComercioUsuarioConfiguration : IEntityTypeConfiguration<ComercioUsuario>
{
    public void Configure(EntityTypeBuilder<ComercioUsuario> builder)
    {
        builder.ToTable("ComercioUsuarios");

        builder.HasKey(cu => new { cu.ComercioId, cu.UsuarioId });

        builder.HasIndex(cu => cu.UsuarioId)
            .IsUnique();

        builder.HasOne(cu => cu.Comercio)
            .WithOne(c => c.ComercioUsuarioRel)
            .HasForeignKey<ComercioUsuario>(cu => cu.ComercioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cu => cu.Usuario)
            .WithOne()
            .HasForeignKey<ComercioUsuario>(cu => cu.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
