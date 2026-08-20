namespace ArtemisBankingPro.Application.ViewModels.Prestamos;

public enum TipoRiesgoPrestamo
{
    Ninguno,
    RiesgoActual,
    RiesgoProyectado
}

public class RiesgoPrestamoResultado
{
    public TipoRiesgoPrestamo Tipo { get; set; }
    public decimal DeudaActual { get; set; }
    public decimal DeudaProyectada { get; set; }
    public decimal DeudaPromedio { get; set; }

    public bool EsAltoRiesgo => Tipo != TipoRiesgoPrestamo.Ninguno;
}
