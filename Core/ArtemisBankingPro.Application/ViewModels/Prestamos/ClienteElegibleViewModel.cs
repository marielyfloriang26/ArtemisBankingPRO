using System;

namespace ArtemisBankingPro.Application.ViewModels.Prestamos;

public class ClienteElegibleViewModel
{
    public int Id { get; set; }
    public string Cedula { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public decimal DeudaTotal { get; set; }
}
