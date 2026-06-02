using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Models;
using BloodDonationApp.Services;
using BloodDonationApp.Data;

namespace BloodDonationApp.Application.Services
{
    public class DonorService : IDonorService
    {
        private readonly IDonorRepository _donorRepository;
        private readonly IRepository<DonationRecord> _donationRecordRepository;
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly IGoogleMapsService _googleMapsService;
        private readonly IGamificationService _gamificationService;
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IRepository<Badge> _badgeRepository;
        private readonly IRepository<Reward> _rewardRepository;
        private readonly IRepository<RedeemedReward> _redeemedRewardRepository;
        private readonly IRepository<Feedback> _feedbackRepository;
        private readonly IRepository<QrScanLog> _qrScanLogRepository;
        private readonly IMapper _mapper;

        public DonorService(
            IDonorRepository donorRepository,
            IRepository<DonationRecord> donationRecordRepository,
            IRepository<AuditLog> auditLogRepository,
            IGoogleMapsService googleMapsService,
            IGamificationService gamificationService,
            IRepository<Notification> notificationRepository,
            IRepository<Badge> badgeRepository,
            IRepository<Reward> rewardRepository,
            IRepository<RedeemedReward> redeemedRewardRepository,
            IRepository<Feedback> feedbackRepository,
            IRepository<QrScanLog> qrScanLogRepository,
            IMapper mapper)
        {
            _donorRepository = donorRepository;
            _donationRecordRepository = donationRecordRepository;
            _auditLogRepository = auditLogRepository;
            _googleMapsService = googleMapsService;
            _gamificationService = gamificationService;
            _notificationRepository = notificationRepository;
            _badgeRepository = badgeRepository;
            _rewardRepository = rewardRepository;
            _redeemedRewardRepository = redeemedRewardRepository;
            _feedbackRepository = feedbackRepository;
            _qrScanLogRepository = qrScanLogRepository;
            _mapper = mapper;
        }

        public async Task<DonorDto?> RegisterDonorAsync(Donor donor)
        {
            // Check for duplicate email
            var existing = await _donorRepository.GetDonorByEmailAsync(donor.Email);
            if (existing != null) return null;

            donor.Password = PasswordHasher.HashPassword(donor.Password);
            donor.IsAvailable = true;
            donor.TotalDonations = 0;
            donor.IsVerified = false;
            donor.VerificationToken = Guid.NewGuid().ToString();

            // Geocode
            var addressStr = $"{donor.Address}, {donor.City}, {donor.State} {donor.PinCode}";
            var coords = await _googleMapsService.GeocodeAddressAsync(addressStr);
            if (coords.HasValue)
            {
                donor.Latitude = coords.Value.Latitude;
                donor.Longitude = coords.Value.Longitude;
            }

            await _donorRepository.AddAsync(donor);
            await _donorRepository.SaveChangesAsync();

            // Signup points
            await _gamificationService.AwardPointsAsync(donor.Id, 100, "Sign-up Bonus");

            return _mapper.Map<DonorDto>(donor);
        }

        public async Task<DonorDto?> LoginAsync(string email, string password)
        {
            var donor = await _donorRepository.GetDonorByEmailAsync(email);
            if (donor == null) return null;

            bool isPasswordCorrect = PasswordHasher.HashPassword(password) == donor.Password;
            if (!isPasswordCorrect) return null;

            return _mapper.Map<DonorDto>(donor);
        }

        public async Task<DonorDto?> GetDonorProfileAsync(int id)
        {
            var donor = await _donorRepository.GetDonorWithBadgesAndRecordsAsync(id);
            if (donor == null) return null;

            return _mapper.Map<DonorDto>(donor);
        }

        public async Task<DonorDto?> UpdateProfileAsync(int id, Donor donorData)
        {
            var existing = await _donorRepository.GetByIdAsync(id);
            if (existing == null) return null;

            bool cityChanged = existing.City != donorData.City;
            bool missingCoords = !existing.Latitude.HasValue || !existing.Longitude.HasValue;

            existing.Name = donorData.Name;
            existing.BloodType = donorData.BloodType;
            existing.Phone = donorData.Phone;
            existing.Email = donorData.Email;
            existing.City = donorData.City;
            existing.LastDonationDate = donorData.LastDonationDate;
            existing.IsAvailable = donorData.IsAvailable;

            if (cityChanged || missingCoords)
            {
                var addressStr = $"{existing.Address}, {existing.City}, {existing.State} {existing.PinCode}";
                var coords = await _googleMapsService.GeocodeAddressAsync(addressStr);
                if (coords.HasValue)
                {
                    existing.Latitude = coords.Value.Latitude;
                    existing.Longitude = coords.Value.Longitude;
                }
            }

            _donorRepository.Update(existing);
            await _donorRepository.SaveChangesAsync();

            return _mapper.Map<DonorDto>(existing);
        }

        public async Task<IEnumerable<DonorDto>> SearchDonorsAsync(string bloodType, string city, string state, int? minAge, int? maxAge, bool isVerified)
        {
            var results = await _donorRepository.FindAsync(d => d.IsAvailable);

            if (!string.IsNullOrEmpty(bloodType) && bloodType != "Any")
            {
                string normalizedBlood = bloodType.Replace(" ", "+").Trim().ToUpper();
                results = results.Where(d => d.BloodType.ToUpper() == normalizedBlood);
            }

            if (!string.IsNullOrEmpty(city))
            {
                string normalizedCity = city.Trim().ToUpper();
                results = results.Where(d => d.City.ToUpper().Contains(normalizedCity));
            }

            if (!string.IsNullOrEmpty(state) && state != "Any")
            {
                string normalizedState = state.Trim().ToUpper();
                results = results.Where(d => d.State.ToUpper() == normalizedState);
            }

            if (minAge.HasValue)
            {
                results = results.Where(d => d.Age >= minAge.Value);
            }

            if (maxAge.HasValue)
            {
                results = results.Where(d => d.Age <= maxAge.Value);
            }

            if (isVerified)
            {
                results = results.Where(d => d.IsVerified);
            }

            // Ordering
            if (!string.IsNullOrEmpty(city))
            {
                string normalizedCity = city.Trim().ToUpper();
                results = results.OrderBy(d => d.City.ToUpper() == normalizedCity ? 0 : 1)
                                 .ThenByDescending(d => d.TotalDonations);
            }
            else
            {
                results = results.OrderByDescending(d => d.TotalDonations);
            }

            return _mapper.Map<IEnumerable<DonorDto>>(results);
        }

        public async Task<IEnumerable<dynamic>> GetLeaderboardAsync(int count)
        {
            var topDonors = await _donorRepository.GetTopDonorsAsync(count);
            return topDonors.Select(d => new
            {
                d.Id,
                d.Name,
                d.BloodType,
                d.Level,
                d.LevelName,
                d.LifetimePoints,
                d.TotalDonations,
                BadgeCount = d.DonorBadges.Count,
                DonationStreak = d.DonationStreak
            }).ToList();
        }

        public async Task<bool> ToggleAvailabilityAndSelfReportAsync(int id, bool isAvailable)
        {
            var donor = await _donorRepository.GetByIdAsync(id);
            if (donor == null) return false;

            bool wasAvailable = donor.IsAvailable;
            donor.IsAvailable = isAvailable;

            if (wasAvailable && !isAvailable)
            {
                var record = new DonationRecord
                {
                    DonorId = donor.Id,
                    DonationDate = DateTime.Today,
                    Units = 1,
                    BloodType = donor.BloodType,
                    Hospital = "Self Reported",
                    City = donor.City ?? "Self Reported",
                    Notes = "Self reported donation",
                    Status = "Completed"
                };

                await _donationRecordRepository.AddAsync(record);
                donor.LastDonationDate = DateTime.Today;
                donor.TotalDonations += 1;
            }

            _donorRepository.Update(donor);
            await _donorRepository.SaveChangesAsync();

            if (wasAvailable && !isAvailable)
            {
                await _gamificationService.AwardPointsAsync(donor.Id, 200, "Completed self-reported donation");
            }

            return true;
        }

        public async Task<DonorDto?> GetDonorByTokenAsync(string token)
        {
            var donor = await _donorRepository.GetDonorByTokenAsync(token);
            if (donor == null) return null;

            return _mapper.Map<DonorDto>(donor);
        }

        public async Task<bool> VerifyDonorProfileAsync(int id)
        {
            var donor = await _donorRepository.GetByIdAsync(id);
            if (donor == null) return false;

            donor.IsVerified = true;
            _donorRepository.Update(donor);
            await _donorRepository.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<DonationRecord>> GetDonationHistoryAsync(int donorId)
        {
            var records = await _donationRecordRepository.FindAsync(r => r.DonorId == donorId);
            return records.OrderByDescending(r => r.DonationDate).ToList();
        }

        public async Task<DonationRecord?> GetDonationRecordAsync(int donationId)
        {
            var records = await _donationRecordRepository.FindAsync(r => r.Id == donationId);
            return records.FirstOrDefault();
        }

        public async Task<IEnumerable<Notification>> GetNotificationsAsync(int donorId)
        {
            var notifications = await _notificationRepository.FindAsync(n => n.DonorId == donorId);
            return notifications.OrderByDescending(n => n.CreatedAt).ToList();
        }

        public async Task<bool> MarkNotificationReadAsync(int notificationId, int donorId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.DonorId != donorId) return false;

            notification.IsRead = true;
            _notificationRepository.Update(notification);
            await _notificationRepository.SaveChangesAsync();
            return true;
        }

        public async Task<(List<Badge> AllBadges, List<Reward> AllRewards, List<dynamic> Leaderboard, Donor Donor)?> GetGamificationDataAsync(int donorId)
        {
            var donor = await _donorRepository.GetDonorWithBadgesAndRecordsAsync(donorId);
            if (donor == null) return null;

            var allBadges = (await _badgeRepository.GetAllAsync()).ToList();
            var allRewards = (await _rewardRepository.FindAsync(r => r.AvailableQuantity > 0)).ToList();

            var topDonors = await _donorRepository.GetTopDonorsAsync(10);
            var leaderboard = topDonors.Select(d => (dynamic)new
            {
                d.Id,
                d.Name,
                d.BloodType,
                d.Level,
                d.LevelName,
                d.LifetimePoints,
                d.TotalDonations,
                BadgeCount = d.DonorBadges.Count
            }).ToList();

            return (allBadges, allRewards, leaderboard, donor);
        }

        public async Task<(bool Success, string Message, string? SuccessMsg)> RedeemRewardAsync(int donorId, int rewardId)
        {
            var donor = await _donorRepository.GetByIdAsync(donorId);
            if (donor == null) return (false, "Donor not found.", null);

            var reward = await _rewardRepository.GetByIdAsync(rewardId);
            if (reward == null) return (false, "The selected reward could not be found.", null);

            if (reward.AvailableQuantity <= 0) return (false, "This reward is currently out of stock.", null);

            if (donor.RewardPoints < reward.PointsCost)
            {
                return (false, $"Insufficient points. You need {reward.PointsCost} points, but you have only {donor.RewardPoints} points.", null);
            }

            // Deduct points
            donor.RewardPoints -= reward.PointsCost;
            reward.AvailableQuantity -= 1;

            var redeemed = new RedeemedReward
            {
                DonorId = donorId,
                RewardId = rewardId,
                RedeemedAt = DateTime.Now,
                Status = "Claimed"
            };
            await _redeemedRewardRepository.AddAsync(redeemed);

            // Add Notification
            var notification = new Notification
            {
                DonorId = donorId,
                Title = "Reward Redeemed!",
                Message = $"Congratulations! You redeemed '{reward.Name}' for {reward.PointsCost} points. Remaining points: {donor.RewardPoints}.",
                Type = "Success",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            await _notificationRepository.AddAsync(notification);

            // Write Audit Log
            var auditLog = new AuditLog
            {
                ActorId = donorId,
                ActorName = donor.Name,
                ActorType = "Donor",
                Action = "Redeem Reward",
                Details = $"Redeemed {reward.Name} for {reward.PointsCost} points. Remainder: {donor.RewardPoints}.",
                EntityType = "Reward",
                EntityId = rewardId,
                Timestamp = DateTime.Now
            };
            await _auditLogRepository.AddAsync(auditLog);

            await _donorRepository.SaveChangesAsync();

            return (true, string.Empty, $"Successfully redeemed '{reward.Name}'! {reward.PointsCost} points have been deducted.");
        }

        public async Task<IEnumerable<DonorDto>> GetAllDonorsAsync()
        {
            var donors = await _donorRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<DonorDto>>(donors);
        }

        public async Task<bool> DeleteDonorAsync(int id)
        {
            var donor = await _donorRepository.GetByIdAsync(id);
            if (donor == null) return false;

            _donorRepository.Remove(donor);
            await _donorRepository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Feedback>> GetAllFeedbackAsync()
        {
            var feedbacks = (await _feedbackRepository.GetAllAsync()).ToList();
            foreach (var f in feedbacks)
            {
                if (f.Donor == null && f.DonorId.HasValue)
                {
                    f.Donor = await _donorRepository.GetByIdAsync(f.DonorId.Value);
                }
            }
            return feedbacks.OrderByDescending(f => f.SubmittedAt).ToList();
        }

        public async Task<bool> MarkFeedbackReadAsync(int id)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(id);
            if (feedback == null) return false;

            feedback.IsRead = true;
            _feedbackRepository.Update(feedback);
            await _feedbackRepository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<QrScanLog>> GetQrScanHistoryAsync()
        {
            var logs = (await _qrScanLogRepository.GetAllAsync()).ToList();
            foreach (var l in logs)
            {
                if (l.Donor == null && l.DonorId.HasValue)
                {
                    l.Donor = await _donorRepository.GetByIdAsync(l.DonorId.Value);
                }
            }
            return logs.OrderByDescending(l => l.ScannedAt).ToList();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int count)
        {
            var logs = await _auditLogRepository.GetAllAsync();
            return logs.OrderByDescending(l => l.Timestamp).Take(count).ToList();
        }

        public async Task<bool> LogQrScanAsync(int? donorId, string scannedBy, bool isValid, string location)
        {
            var scanLog = new QrScanLog
            {
                DonorId = donorId,
                ScannedBy = scannedBy,
                ScannedAt = DateTime.Now,
                IsValid = isValid,
                Location = location
            };
            await _qrScanLogRepository.AddAsync(scanLog);
            await _qrScanLogRepository.SaveChangesAsync();
            return true;
        }

        public async Task<Donor?> GetDonorByVerificationTokenAsync(string token)
        {
            return await _donorRepository.GetDonorByTokenAsync(token);
        }
    }
}
