using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class DonationRecord
    {
        public int Id { get; set; }

        [Required]
        public int DonorId { get; set; }

        public Donor Donor { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Donation Date")]
        public DateTime DonationDate { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Units must be between 1 and 10")]
        public int Units { get; set; }

        [Required]
        [Display(Name = "Blood Type")]
        public string BloodType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Hospital { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        [RegularExpression("^(Completed|Cancelled|Pending)$", ErrorMessage = "Status must be Completed, Cancelled, or Pending")]
        public string Status { get; set; } = "Pending";
    }
}
