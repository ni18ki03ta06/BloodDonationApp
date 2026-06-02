using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class BloodCamp
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 3)]
        [Display(Name = "Camp Name")]
        public string CampName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Organized By")]
        public string OrganizedBy { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Date")]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Max donors must be between 1 and 1000")]
        [Display(Name = "Max Donors")]
        public int MaxDonors { get; set; }

        [Required]
        [Display(Name = "Registered Count")]
        public int RegisteredCount { get; set; } = 0;

        [Required]
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [EmailAddress(ErrorMessage = "Invalid contact email address")]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required]
        [Phone(ErrorMessage = "Invalid contact phone number")]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
