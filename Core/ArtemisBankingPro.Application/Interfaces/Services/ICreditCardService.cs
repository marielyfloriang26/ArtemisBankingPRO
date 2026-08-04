using ArtemisBankingPro.Application.ViewModels.CreditCards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface ICreditCardService
{
    Task<List<CreditCardViewModel>> GetAllViewModel();
    Task<SaveCreditCardViewModel> Add(SaveCreditCardViewModel vm);
    Task<SaveCreditCardViewModel> GetByIdSaveViewModel(int id);
    Task Update(SaveCreditCardViewModel vm, int id);
    Task ChangeStatus(int id);
}
