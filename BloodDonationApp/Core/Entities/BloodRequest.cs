using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonationApp.Models
{
    public class BloodRequest
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Patient Name")]
        public string PatientName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Blood Type Required")]
        public string BloodType { get; set; } = string.Empty;

        [Required]
        public int Units { get; set; }

        [Required]
        public string Hospital { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime RequiredDate { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Fulfilled, Cancelled

        [Required]
        public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Normal; // Normal, Urgent, Critical

        [Required]
        [Display(Name = "Requester Name")]
        public string RequesterName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Requester Email")]
        public string? RequesterEmail { get; set; }

        public string? Diagnosis { get; set; }

        [Display(Name = "Request Anonymously")]
        public bool IsAnonymous { get; set; } = false;

        [Display(Name = "Fulfilled By")]
        public int? FulfilledBy { get; set; }

        [ForeignKey("FulfilledBy")]
        [Display(Name = "Fulfilled By Donor")]
        public Donor? FulfilledByDonor { get; set; }

        [Display(Name = "Fulfilled At")]
        public DateTime? FulfilledAt { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public enum UrgencyLevel
    {
        Normal,
        Urgent,
        Critical
    }
}

