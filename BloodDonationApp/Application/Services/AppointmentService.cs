using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Models;
using BloodDonationApp.Services;

namespace BloodDonationApp.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDonorRepository _donorRepository;
        private readonly IRepository<DonationRecord> _donationRecordRepository;
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly IGamificationService _gamificationService;
        private readonly IMapper _mapper;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IDonorRepository donorRepository,
            IRepository<DonationRecord> donationRecordRepository,
            IRepository<Notification> notificationRepository,
            IRepository<AuditLog> auditLogRepository,
            IGamificationService gamificationService,
            IMapper mapper)
        {
            _appointmentRepository = appointmentRepository;
            _donorRepository = donorRepository;
            _donationRecordRepository = donationRecordRepository;
            _notificationRepository = notificationRepository;
            _auditLogRepository = auditLogRepository;
            _gamificationService = gamificationService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AppointmentDto>> GetDonorAppointmentsAsync(int donorId)
        {
            var appts = await _appointmentRepository.GetAppointmentsByDonorIdAsync(donorId);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appts);
        }

        public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
        {
            var appts = await _appointmentRepository.GetAppointmentsWithDonorAsync();
            return _mapper.Map<IEnumerable<AppointmentDto>>(appts);
        }

        public async Task<IEnumerable<string>> GetBookedSlotsForDateAsync(DateTime date)
        {
            var booked = await _appointmentRepository.GetBookedSlotsAsync(date);
            return booked.Select(a => a.TimeSlot).ToList();
        }

        public async Task<AppointmentDto?> BookAppointmentAsync(int donorId, DateTime date, string timeSlot, string? notes, Action<string, string> addModelError)
        {
            var targetDate = date.Date;
            if (targetDate < DateTime.Today)
            {
                addModelError("", "Appointment date cannot be in the past.");
                return null;
            }

            if (string.IsNullOrEmpty(timeSlot))
            {
                addModelError("TimeSlot", "Please select a time slot.");
                return null;
            }

            // 1. Prevent Double Booking: Check if donor already has an active appointment on the same date
            bool hasActiveAppt = await _appointmentRepository.HasActiveAppointmentOnDateAsync(donorId, targetDate);
            if (hasActiveAppt)
            {
                addModelError("", "You already have an active appointment booked for this date.");
                return null;
            }

            // 2. Prevent Double Booking: Check if the slot is already booked by another donor
            bool isSlotTaken = await _appointmentRepository.IsSlotBookedAsync(targetDate, timeSlot);
            if (isSlotTaken)
            {
                addModelError("TimeSlot", "The selected time slot is already booked by another donor.");
                return null;
            }

            var appointment = new Appointment
            {
                DonorId = donorId,
                AppointmentDate = targetDate,
                TimeSlot = timeSlot,
                Notes = notes,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            await _appointmentRepository.AddAsync(appointment);
            
            // Create Notification Reminder
            var notification = new Notification
            {
                DonorId = donorId,
                Title = "Appointment Booked",
                Message = $"Your donation appointment for {targetDate:dd-MMM-yyyy} at {timeSlot} has been successfully requested. It is currently pending administrator approval.",
                Type = "Info",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            await _notificationRepository.AddAsync(notification);
            
            await _appointmentRepository.SaveChangesAsync();

            // Award Booking Points
            await _gamificationService.AwardPointsAsync(donorId, 50, "Booked appointment");

            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<bool> ApproveAppointmentAsync(int id)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null || appointment.Status != "Pending") return false;

            appointment.Status = "Approved";
            _appointmentRepository.Update(appointment);

            var notification = new Notification
            {
                DonorId = appointment.DonorId,
                Title = "Appointment Approved",
                Message = $"Your blood donation appointment scheduled for {appointment.AppointmentDate:dd-MMM-yyyy} at {appointment.TimeSlot} has been approved. Please remember to carry your Digital ID card.",
                Type = "Success",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            await _notificationRepository.AddAsync(notification);

            await _appointmentRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAppointmentAsync(int id, int donorId)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null || appointment.DonorId != donorId || appointment.Status == "Completed") return false;

            appointment.Status = "Cancelled";
            _appointmentRepository.Update(appointment);

            var notification = new Notification
            {
                DonorId = donorId,
                Title = "Appointment Cancelled",
                Message = $"Your donation appointment scheduled for {appointment.AppointmentDate:dd-MMM-yyyy} at {appointment.TimeSlot} has been cancelled.",
                Type = "Warning",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            await _notificationRepository.AddAsync(notification);

            await _appointmentRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteAppointmentAsync(int id)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null || appointment.Status != "Approved") return false;

            appointment.Status = "Completed";
            _appointmentRepository.Update(appointment);

            var donor = await _donorRepository.GetByIdAsync(appointment.DonorId);
            if (donor != null)
            {
                var record = new DonationRecord
                {
                    DonorId = donor.Id,
                    DonationDate = appointment.AppointmentDate,
                    Units = 1,
                    BloodType = donor.BloodType,
                    Hospital = "Donora Center",
                    City = donor.City ?? "Donora Center",
                    Notes = $"Appointment completed donation. Notes: {appointment.Notes}",
                    Status = "Completed"
                };
                await _donationRecordRepository.AddAsync(record);

                donor.LastDonationDate = appointment.AppointmentDate;
                donor.TotalDonations += 1;
                _donorRepository.Update(donor);

                var notification = new Notification
                {
                    DonorId = donor.Id,
                    Title = "Donation Completed",
                    Message = $"Thank you for your donation on {appointment.AppointmentDate:dd-MMM-yyyy}. A new donation record has been added to your profile history. Your lifesaving badge progress has been updated!",
                    Type = "Success",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                await _notificationRepository.AddAsync(notification);
            }

            await _appointmentRepository.SaveChangesAsync();

            if (donor != null)
            {
                await _gamificationService.AwardPointsAsync(donor.Id, 200, "Completed appointment donation");
            }

            return true;
        }

        public async Task<bool> RejectAppointmentAsync(int id)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null || appointment.Status == "Completed" || appointment.Status == "Cancelled") return false;

            appointment.Status = "Cancelled";
            _appointmentRepository.Update(appointment);

            var notification = new Notification
            {
                DonorId = appointment.DonorId,
                Title = "Appointment Cancelled",
                Message = $"Your blood donation appointment scheduled for {appointment.AppointmentDate:dd-MMM-yyyy} at {appointment.TimeSlot} has been rejected or cancelled by the administrator.",
                Type = "Warning",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            await _notificationRepository.AddAsync(notification);

            await _appointmentRepository.SaveChangesAsync();
            return true;
        }
    }
}
