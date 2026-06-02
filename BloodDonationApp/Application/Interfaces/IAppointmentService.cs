using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BloodDonationApp.Application.DTOs;

namespace BloodDonationApp.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentDto>> GetDonorAppointmentsAsync(int donorId);
        Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync();
        Task<IEnumerable<string>> GetBookedSlotsForDateAsync(DateTime date);
        Task<AppointmentDto?> BookAppointmentAsync(int donorId, DateTime date, string timeSlot, string? notes, Action<string, string> addModelError);
        Task<bool> ApproveAppointmentAsync(int id);
        Task<bool> CancelAppointmentAsync(int id, int donorId);
        Task<bool> CompleteAppointmentAsync(int id);
        Task<bool> RejectAppointmentAsync(int id);
    }
}
