namespace ArtemisBankingPro.Domain.Entities;

public class ComercioUsuario
{
    public int ComercioId { get; set; }
    public int UsuarioId { get; set; }

    // Propiedades de navegación
    public Comercio? Comercio { get; set; }
    public Usuario? Usuario { get; set; }
}
