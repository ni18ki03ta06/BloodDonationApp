using System.Collections.Generic;
using System.Threading.Tasks;
using BloodDonationApp.Models;

namespace BloodDonationApp.Core.Interfaces
{
    public interface IBloodRequestRepository : IRepository<BloodRequest>
    {
        Task<IEnumerable<BloodRequest>> GetBloodRequestsWithFulfilledDonorAsync();
    }
}
