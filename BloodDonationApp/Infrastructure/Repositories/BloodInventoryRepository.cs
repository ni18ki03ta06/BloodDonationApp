using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Data;
using BloodDonationApp.Models;

namespace BloodDonationApp.Infrastructure.Repositories
{
    public class BloodInventoryRepository : Repository<BloodInventory>, IBloodInventoryRepository
    {
        public BloodInventoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<BloodInventory?> GetByBloodTypeAsync(string bloodType)
        {
            var normalizedType = bloodType.Replace(" ", "+").Trim().ToUpper();
            return await _dbSet.FirstOrDefaultAsync(bi => bi.BloodType.ToUpper() == normalizedType);
        }
    }
}
