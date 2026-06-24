using System.Collections.Generic;
using System.Threading.Tasks;
using AvecADeskApi.Model;
namespace AvecADeskApi.Interfaces
{
    public interface ICardStatusRepository
    {
        Task<List<CardStatusResponse>> GetCardStatusesAsync();
    }
}
