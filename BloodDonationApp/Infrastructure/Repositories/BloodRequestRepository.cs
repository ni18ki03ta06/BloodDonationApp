using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Data;
using BloodDonationApp.Models;

namespace BloodDonationApp.Infrastructure.Repositories
{
    public class BloodRequestRepository : Repository<BloodRequest>, IBloodRequestRepository
    {
        public BloodRequestRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<BloodRequest>> GetBloodRequestsWithFulfilledDonorAsync()
        {
            return await _dbSet
                .Include(br => br.FulfilledByDonor)
                .OrderByDescending(br => br.CreatedAt)
                .ToListAsync();
        }
    }
}
