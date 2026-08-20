namespace ArtemisBankingPro.Application.ViewModels.Comercios;

public enum ComercioOperacionEstado
{
    Exitoso,
    DatosInvalidos,
    NoEncontrado,
    Conflicto
}

public class ComercioOperacionResultado
{
    public ComercioOperacionEstado Estado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public ComercioViewModel? Comercio { get; set; }

    public bool EsExitoso => Estado == ComercioOperacionEstado.Exitoso;

    public static ComercioOperacionResultado Fallo(ComercioOperacionEstado estado, string mensaje) =>
        new() { Estado = estado, Mensaje = mensaje };

    public static ComercioOperacionResultado Exito(ComercioViewModel? comercio = null) =>
        new() { Estado = ComercioOperacionEstado.Exitoso, Comercio = comercio };
}
