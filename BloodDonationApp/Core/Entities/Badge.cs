using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class Badge
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Icon { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ColorHex { get; set; } = "#B5894A";

        [Required]
        [StringLength(100)]
        public string Criteria { get; set; } = string.Empty;

        public int PointsAwarded { get; set; } = 100;
    }
}
