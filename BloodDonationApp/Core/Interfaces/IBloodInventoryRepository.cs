using System.Threading.Tasks;
using BloodDonationApp.Models;

namespace BloodDonationApp.Core.Interfaces
{
    public interface IBloodInventoryRepository : IRepository<BloodInventory>
    {
        Task<BloodInventory?> GetByBloodTypeAsync(string bloodType);
    }
}
