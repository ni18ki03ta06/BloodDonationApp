using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActorType { get; set; } = string.Empty; // 'Admin' or 'Donor'

        [Required]
        public int ActorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ActorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // e.g. 'DeleteDonor', 'ApproveRequest'

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty; // e.g. 'Donor', 'BloodRequest'

        public int? EntityId { get; set; }

        public string Details { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
