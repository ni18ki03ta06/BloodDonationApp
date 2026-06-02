using System.Collections.Generic;
using System.Threading.Tasks;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Models;

namespace BloodDonationApp.Application.Interfaces
{
    public interface IDonorService
    {
        Task<DonorDto?> RegisterDonorAsync(Donor donor);
        Task<DonorDto?> LoginAsync(string email, string password);
        Task<DonorDto?> GetDonorProfileAsync(int id);
        Task<DonorDto?> UpdateProfileAsync(int id, Donor donor);
        Task<IEnumerable<DonorDto>> SearchDonorsAsync(string bloodType, string city, string state, int? minAge, int? maxAge, bool isVerified);
        Task<IEnumerable<dynamic>> GetLeaderboardAsync(int count);
        Task<bool> ToggleAvailabilityAndSelfReportAsync(int id, bool isAvailable);
        Task<DonorDto?> GetDonorByTokenAsync(string token);
        Task<bool> VerifyDonorProfileAsync(int id);
        Task<IEnumerable<DonationRecord>> GetDonationHistoryAsync(int donorId);
        Task<DonationRecord?> GetDonationRecordAsync(int donationId);
        Task<IEnumerable<Notification>> GetNotificationsAsync(int donorId);
        Task<bool> MarkNotificationReadAsync(int notificationId, int donorId);
        Task<(List<Badge> AllBadges, List<Reward> AllRewards, List<dynamic> Leaderboard, Donor Donor)?> GetGamificationDataAsync(int donorId);
        Task<(bool Success, string Message, string? SuccessMsg)> RedeemRewardAsync(int donorId, int rewardId);
        Task<IEnumerable<DonorDto>> GetAllDonorsAsync();
        Task<bool> DeleteDonorAsync(int id);
        Task<IEnumerable<Feedback>> GetAllFeedbackAsync();
        Task<bool> MarkFeedbackReadAsync(int id);
        Task<IEnumerable<QrScanLog>> GetQrScanHistoryAsync();
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int count);
        Task<bool> LogQrScanAsync(int? donorId, string scannedBy, bool isValid, string location);
        Task<Donor?> GetDonorByVerificationTokenAsync(string token);
    }
}
