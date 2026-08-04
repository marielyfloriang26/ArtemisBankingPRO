using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Application.ViewModels.Prestamos;

public class RiskWarningViewModel
{
    public int ClienteId { get; set; }
    public string NombreCliente { get; set; } = null!;
    public string MensajeRiesgo { get; set; } = null!;
    public decimal TotalDeudaActual { get; set; }
    public int CantidadPrestamosActivos { get; set; }
    public int TarjetasAlLimite { get; set; }
    public bool TieneAtrasos { get; set; }
}
