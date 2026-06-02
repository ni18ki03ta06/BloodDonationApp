using System.Collections.Generic;
using System.Threading.Tasks;
using BloodDonationApp.Models;

namespace BloodDonationApp.Core.Interfaces
{
    public interface IDonorRepository : IRepository<Donor>
    {
        Task<Donor?> GetDonorByEmailAsync(string email);
        Task<Donor?> GetDonorByTokenAsync(string token);
        Task<Donor?> GetDonorWithBadgesAndRecordsAsync(int id);
        Task<IEnumerable<Donor>> GetTopDonorsAsync(int count);
    }
}
