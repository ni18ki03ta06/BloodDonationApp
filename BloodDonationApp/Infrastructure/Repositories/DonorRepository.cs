using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Data;
using BloodDonationApp.Models;

namespace BloodDonationApp.Infrastructure.Repositories
{
    public class DonorRepository : Repository<Donor>, IDonorRepository
    {
        public DonorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Donor?> GetDonorByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.Email.ToLower() == email.ToLower());
        }

        public async Task<Donor?> GetDonorByTokenAsync(string token)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.VerificationToken == token);
        }

        public async Task<Donor?> GetDonorWithBadgesAndRecordsAsync(int id)
        {
            return await _dbSet
                .Include(d => d.DonorBadges)
                    .ThenInclude(db => db.Badge)
                .Include(d => d.DonationRecords)
                .Include(d => d.RedeemedRewards)
                    .ThenInclude(rr => rr.Reward)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Donor>> GetTopDonorsAsync(int count)
        {
            return await _dbSet
                .Include(d => d.DonationRecords)
                .Include(d => d.DonorBadges)
                .OrderByDescending(d => d.LifetimePoints)
                .Take(count)
                .ToListAsync();
        }
    }
}
