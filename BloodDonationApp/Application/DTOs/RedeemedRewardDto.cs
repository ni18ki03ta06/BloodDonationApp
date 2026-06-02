using System;

namespace BloodDonationApp.Application.DTOs
{
    public class RedeemedRewardDto
    {
        public int Id { get; set; }
        public int DonorId { get; set; }
        public int RewardId { get; set; }
        public RewardDto Reward { get; set; } = null!;
        public DateTime RedeemedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
