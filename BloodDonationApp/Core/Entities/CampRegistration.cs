using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class CampRegistration
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Camp")]
        public int CampId { get; set; }

        public BloodCamp? Camp { get; set; }

        [Required]
        [Display(Name = "Donor")]
        public int DonorId { get; set; }

        public Donor? Donor { get; set; }

        [Required]
        [Display(Name = "Registered At")]
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Registered"; // Registered, Cancelled, Attended
    }
}
