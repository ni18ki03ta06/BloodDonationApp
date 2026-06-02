using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        [Required]
        public int DonorId { get; set; }

        public Donor Donor { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public bool IsUsed { get; set; } = false;
    }
}
