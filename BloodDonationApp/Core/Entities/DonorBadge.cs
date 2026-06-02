using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class DonorBadge
    {
        public int Id { get; set; }

        [Required]
        public int DonorId { get; set; }
        public Donor Donor { get; set; } = null!;

        [Required]
        public int BadgeId { get; set; }
        public Badge Badge { get; set; } = null!;

        public DateTime UnlockedAt { get; set; } = DateTime.Now;
    }
}
