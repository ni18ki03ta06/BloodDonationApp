using System;

namespace BloodDonationApp.Application.DTOs
{
    public class BloodInventoryDto
    {
        public int Id { get; set; }
        public string BloodType { get; set; } = string.Empty;
        public int UnitsAvailable { get; set; }
        public int UnitsReserved { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
