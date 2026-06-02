using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BloodDonationApp.Models;

namespace BloodDonationApp.Core.Interfaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<IEnumerable<Appointment>> GetAppointmentsByDonorIdAsync(int donorId);
        Task<IEnumerable<Appointment>> GetBookedSlotsAsync(DateTime date);
        Task<IEnumerable<Appointment>> GetAppointmentsWithDonorAsync();
        Task<bool> HasActiveAppointmentOnDateAsync(int donorId, DateTime date);
        Task<bool> IsSlotBookedAsync(DateTime date, string timeSlot);
    }
}
