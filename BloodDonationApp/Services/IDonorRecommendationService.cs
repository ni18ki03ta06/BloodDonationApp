using System.Collections.Generic;
using System.Threading.Tasks;
using BloodDonationApp.Models;

namespace BloodDonationApp.Services
{
    public class RecommendedDonorDto
    {
        public Donor Donor { get; set; } = null!;
        public int Score { get; set; }
        public double EstimatedDistance { get; set; } // in kilometers
        public List<string> Reasons { get; set; } = new List<string>();
        public bool IsEligible { get; set; }
    }

    public interface IDonorRecommendationService
    {
        Task<List<RecommendedDonorDto>> GetRecommendationsAsync(int requestId);
        Task NotifyTopDonorsAsync(BloodRequest request);
    }
}
