using System;

namespace BloodDonationApp.Application.DTOs
{
    public class DonorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime? LastDonationDate { get; set; }
        public bool IsAvailable { get; set; }
        public string? ProfilePhotoPath { get; set; }
        public int TotalDonations { get; set; }
        public bool IsVerified { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int RewardPoints { get; set; }
        public int LifetimePoints { get; set; }
        public int Level { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public string VerificationToken { get; set; } = string.Empty;
        public DateTime NextEligibilityDate { get; set; }
        public int DonationStreak { get; set; }
    }
}
