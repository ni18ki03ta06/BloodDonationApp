using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Data;
using BloodDonationApp.Models;

namespace BloodDonationApp.Infrastructure.Repositories
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByDonorIdAsync(int donorId)
        {
            return await _dbSet
                .Where(a => a.DonorId == donorId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetBookedSlotsAsync(DateTime date)
        {
            return await _dbSet
                .Where(a => a.AppointmentDate.Date == date.Date && a.Status != "Cancelled")
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsWithDonorAsync()
        {
            return await _dbSet
                .Include(a => a.Donor)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<bool> HasActiveAppointmentOnDateAsync(int donorId, DateTime date)
        {
            return await _dbSet
                .AnyAsync(a => a.DonorId == donorId && a.AppointmentDate.Date == date.Date && a.Status != "Cancelled");
        }

        public async Task<bool> IsSlotBookedAsync(DateTime date, string timeSlot)
        {
            return await _dbSet
                .AnyAsync(a => a.AppointmentDate.Date == date.Date && a.TimeSlot == timeSlot && a.Status != "Cancelled");
        }
    }
}
