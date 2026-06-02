using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class RedeemedReward
    {
        public int Id { get; set; }

        [Required]
        public int DonorId { get; set; }
        public Donor Donor { get; set; } = null!;

        [Required]
        public int RewardId { get; set; }
        public Reward Reward { get; set; } = null!;

        public DateTime RedeemedAt { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Claimed";
    }
}
