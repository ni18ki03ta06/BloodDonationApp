using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class Reward
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int PointsCost { get; set; }

        [Required]
        [StringLength(50)]
        public string Icon { get; set; } = string.Empty;

        [Required]
        public int AvailableQuantity { get; set; } = 100;
    }
}
