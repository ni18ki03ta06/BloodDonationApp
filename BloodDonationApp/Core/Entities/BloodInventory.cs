using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class BloodInventory
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression("^(A\\+|A-|B\\+|B-|AB\\+|AB-|O\\+|O-)$", ErrorMessage = "Invalid blood type")]
        [Display(Name = "Blood Type")]
        public string BloodType { get; set; } = string.Empty;

        [Required]
        [Range(0, 10000, ErrorMessage = "Units available must be non-negative")]
        [Display(Name = "Units Available")]
        public int UnitsAvailable { get; set; }

        [Required]
        [Range(0, 10000, ErrorMessage = "Units reserved must be non-negative")]
        [Display(Name = "Units Reserved")]
        public int UnitsReserved { get; set; }

        [Required]
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
