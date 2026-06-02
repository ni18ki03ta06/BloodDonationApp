using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Data;
using BloodDonationApp.Models;

namespace BloodDonationApp.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly ApplicationDbContext _context;

        public GamificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AwardPointsAsync(int donorId, int points, string reason)
        {
            var donor = await _context.Donors.FindAsync(donorId);
            if (donor == null) return;

            int oldLevel = donor.Level;
            donor.RewardPoints += points;
            donor.LifetimePoints += points;

            // Recalculate Level
            int newLevel = 1;
            string levelName = "Novice Donor";

            if (donor.LifetimePoints >= 1000)
            {
                newLevel = 5;
                levelName = "Legendary Hero";
            }
            else if (donor.LifetimePoints >= 600)
            {
                newLevel = 4;
                levelName = "Gold Lifesaver";
            }
            else if (donor.LifetimePoints >= 300)
            {
                newLevel = 3;
                levelName = "Silver Guardian";
            }
            else if (donor.LifetimePoints >= 100)
            {
                newLevel = 2;
                levelName = "Bronze Savior";
            }

            if (newLevel > oldLevel)
            {
                donor.Level = newLevel;
                donor.LevelName = levelName;

                // Create level up notification
                var notification = new Notification
                {
                    DonorId = donorId,
                    Title = "Level Up!",
                    Message = $"Congratulations! You have leveled up to Level {newLevel} ({levelName})!",
                    Type = "Success",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            // Run badge checks after points change, but check badges after saving so state is clean
            await CheckAndAwardBadgesAsync(donorId);
        }

        public async Task CheckAndAwardBadgesAsync(int donorId)
        {
            // Fetch donor with current badges and donation records
            var donor = await _context.Donors
                .Include(d => d.DonorBadges)
                .Include(d => d.DonationRecords)
                .FirstOrDefaultAsync(d => d.Id == donorId);

            if (donor == null) return;

            var unlockedBadgeIds = donor.DonorBadges.Select(db => db.BadgeId).ToList();
            var allBadges = await _context.Badges.ToListAsync();

            int completedDonationsCount = donor.DonationRecords.Count(dr => dr.Status == "Completed");
            bool hasEmergencyDonation = donor.DonationRecords.Any(dr => dr.Status == "Completed" && 
                dr.Notes != null && dr.Notes.Contains("Emergency", StringComparison.OrdinalIgnoreCase));

            foreach (var badge in allBadges)
            {
                if (unlockedBadgeIds.Contains(badge.Id))
                    continue;

                bool meetsCriteria = false;
                var parts = badge.Criteria.Split(':');
                if (parts.Length == 2)
                {
                    string criteriaType = parts[0].ToLower().Trim();
                    string criteriaVal = parts[1].Trim();

                    if (criteriaType == "donations" && int.TryParse(criteriaVal, out int reqDonations))
                    {
                        meetsCriteria = completedDonationsCount >= reqDonations;
                    }
                    else if (criteriaType == "level" && int.TryParse(criteriaVal, out int reqLevel))
                    {
                        meetsCriteria = donor.Level >= reqLevel;
                    }
                    else if (criteriaType == "emergency" && criteriaVal == "1")
                    {
                        meetsCriteria = hasEmergencyDonation;
                    }
                }

                if (meetsCriteria)
                {
                    // Add badge to donor
                    var donorBadge = new DonorBadge
                    {
                        DonorId = donorId,
                        BadgeId = badge.Id,
                        UnlockedAt = DateTime.Now
                    };
                    _context.DonorBadges.Add(donorBadge);

                    // Add Notification
                    var notification = new Notification
                    {
                        DonorId = donorId,
                        Title = "Badge Unlocked!",
                        Message = $"You unlocked the '{badge.Name}' badge: {badge.Description}! +{badge.PointsAwarded} Points.",
                        Type = "Success",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    _context.Notifications.Add(notification);

                    // Save this badge unlock first to avoid duplicate evaluation
                    await _context.SaveChangesAsync();

                    // Award badge points (non-recursively because it's marked as unlocked now)
                    donor.RewardPoints += badge.PointsAwarded;
                    donor.LifetimePoints += badge.PointsAwarded;
                    
                    // Recheck level
                    int finalLevel = 1;
                    string finalLevelName = "Novice Donor";
                    if (donor.LifetimePoints >= 1000) { finalLevel = 5; finalLevelName = "Legendary Hero"; }
                    else if (donor.LifetimePoints >= 600) { finalLevel = 4; finalLevelName = "Gold Lifesaver"; }
                    else if (donor.LifetimePoints >= 300) { finalLevel = 3; finalLevelName = "Silver Guardian"; }
                    else if (donor.LifetimePoints >= 100) { finalLevel = 2; finalLevelName = "Bronze Savior"; }

                    if (finalLevel > donor.Level)
                    {
                        donor.Level = finalLevel;
                        donor.LevelName = finalLevelName;
                        _context.Notifications.Add(new Notification
                        {
                            DonorId = donorId,
                            Title = "Level Up!",
                            Message = $"Congratulations! You have leveled up to Level {finalLevel} ({finalLevelName})!",
                            Type = "Success",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
