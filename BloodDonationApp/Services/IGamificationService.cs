using System.Threading.Tasks;

namespace BloodDonationApp.Services
{
    public interface IGamificationService
    {
        Task AwardPointsAsync(int donorId, int points, string reason);
        Task CheckAndAwardBadgesAsync(int donorId);
    }
}
