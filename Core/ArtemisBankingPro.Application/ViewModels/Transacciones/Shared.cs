using System.Collections.Generic;

namespace ArtemisBankingPro.Application.ViewModels.Transacciones;

public class CuentaSelectItemViewModel
{
    public int Id { get; set; }
    public string NumeroCuenta { get; set; } = null!;
    public decimal Balance { get; set; }
}

public class TarjetaSelectItemViewModel
{
    public int Id { get; set; }
    public string UltimosDigitos { get; set; } = null!;
    public decimal MontoAdeudado { get; set; }
}

public class PrestamoSelectItemViewModel
{
    public int Id { get; set; }
    public string NumeroPrestamo { get; set; } = null!;
    public decimal MontoPendiente { get; set; }
}

public class BeneficiarioSelectItemViewModel
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public string NumeroCuenta { get; set; } = null!;
}

public class OperationResultViewModel
{
    public bool Success { get; set; }
    public bool CorreoFallido { get; set; }
    public string Message { get; set; } = null!;
}
