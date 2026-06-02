namespace BloodDonationApp.Application.DTOs
{
    public class BadgeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public string Criteria { get; set; } = string.Empty;
        public int PointsAwarded { get; set; }
    }
}
