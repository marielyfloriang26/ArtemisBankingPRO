using AutoMapper;
using ArtemisBankingPro.Application.ViewModels.Cliente;
using ArtemisBankingPro.Domain.Entities;

namespace ArtemisBankingPro.Application.Mappings;

public class GeneralProfile : Profile
{
    public GeneralProfile()
    {
        CreateMap<CuentaAhorro, CuentaAhorroViewModel>()
            .ReverseMap();
    }
}