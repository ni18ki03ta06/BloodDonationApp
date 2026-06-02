using System;

namespace BloodDonationApp.Application.DTOs
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public int DonorId { get; set; }
        public DonorDto Donor { get; set; } = null!;
        public DateTime AppointmentDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
