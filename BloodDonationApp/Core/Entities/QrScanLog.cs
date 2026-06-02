using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class QrScanLog
    {
        public int Id { get; set; }

        public int? DonorId { get; set; }
        
        public Donor? Donor { get; set; }

        [Required]
        [StringLength(100)]
        public string ScannedBy { get; set; } = string.Empty; // Admin Email / Username

        [Required]
        public DateTime ScannedAt { get; set; } = DateTime.Now;

        public bool IsValid { get; set; }

        [StringLength(200)]
        public string Location { get; set; } = string.Empty; // Browser/IP details or city
    }
}
