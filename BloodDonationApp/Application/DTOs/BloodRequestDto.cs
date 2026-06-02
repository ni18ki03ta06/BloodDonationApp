using System;

namespace BloodDonationApp.Application.DTOs
{
    public class BloodRequestDto
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public int Units { get; set; }
        public string Hospital { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public DateTime RequiredDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public string UrgencyLevel { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? FulfilledBy { get; set; }
        public DateTime? FulfilledAt { get; set; }
    }
}
