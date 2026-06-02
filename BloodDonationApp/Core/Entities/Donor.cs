using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace BloodDonationApp.Models
{
    public class Donor
    {
        public int Id { get; set; }

        [Required]
        [StringLength(60, MinimumLength = 2)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name can only contain letters")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Blood Type")]
        public string BloodType { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Range(18, 65, ErrorMessage = "Age must be between 18 and 65")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Full Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pin Code is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pin Code must be a 6-digit number")]
        [Display(Name = "Pin Code")]
        public string PinCode { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Last Donation Date")]
        [DataType(DataType.Date)]
        public DateTime? LastDonationDate { get; set; }

        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Profile Photo")]
        public string? ProfilePhotoPath { get; set; }

        [Display(Name = "Total Donations")]
        public int TotalDonations { get; set; } = 0;

        [Display(Name = "Is Verified")]
        public bool IsVerified { get; set; } = false;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string VerificationToken { get; set; } = string.Empty;

        [Display(Name = "Reward Points")]
        public int RewardPoints { get; set; } = 0;

        [Display(Name = "Lifetime Points")]
        public int LifetimePoints { get; set; } = 0;

        public int Level { get; set; } = 1;

        [StringLength(50)]
        [Display(Name = "Level Title")]
        public string LevelName { get; set; } = "Novice Donor";

        [NotMapped]
        [Display(Name = "Next Eligibility Date")]
        public DateTime NextEligibilityDate => LastDonationDate?.AddDays(56) ?? DateTime.Today.AddDays(-1);

        [NotMapped]
        [Display(Name = "Donation Streak")]
        public int DonationStreak
        {
            get
            {
                if (DonationRecords == null || !DonationRecords.Any()) return 0;
                
                var completed = DonationRecords
                    .Where(r => string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.DonationDate)
                    .ToList();

                if (!completed.Any()) return 0;

                int streak = 1;
                for (int i = 0; i < completed.Count - 1; i++)
                {
                    var gap = (completed[i].DonationDate - completed[i + 1].DonationDate).TotalDays;
                    if (gap <= 180)
                    {
                        streak++;
                    }
                    else
                    {
                        break;
                    }
                }
                return streak;
            }
        }

        [Display(Name = "Donor Badges")]
        public ICollection<DonorBadge> DonorBadges { get; set; } = new List<DonorBadge>();

        [Display(Name = "Redeemed Rewards")]
        public ICollection<RedeemedReward> RedeemedRewards { get; set; } = new List<RedeemedReward>();

        [NotMapped]
        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhoto { get; set; }

        [Display(Name = "Donation Records")]
        public ICollection<DonationRecord> DonationRecords { get; set; } = new List<DonationRecord>();

        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

        public ICollection<CampRegistration> CampRegistrations { get; set; } = new List<CampRegistration>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$",
            ErrorMessage = "Password must have uppercase, lowercase, number, and special character")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [NotMapped]
        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Password and Confirmation Password do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

