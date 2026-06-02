namespace BloodDonationApp.Application.DTOs
{
    public class RewardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PointsCost { get; set; }
        public string Icon { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
    }
}
