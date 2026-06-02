using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationApp.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int DonorId { get; set; }
        public Donor Donor { get; set; } = null!;

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
        public string Message { get; set; } = string.Empty;

        [Required(ErrorMessage = "Notification type is required")]
        [RegularExpression("^(Info|Success|Warning)$", ErrorMessage = "Type must be Info, Success, or Warning")]
        public string Type { get; set; } = "Info";

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
