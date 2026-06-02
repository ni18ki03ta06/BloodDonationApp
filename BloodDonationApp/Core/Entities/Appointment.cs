using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public int DonorId { get; set; }

        public Donor Donor { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Time Slot")]
        public string TimeSlot { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Pending|Approved|Completed|Cancelled)$", ErrorMessage = "Status must be Pending, Approved, Completed, or Cancelled")]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
