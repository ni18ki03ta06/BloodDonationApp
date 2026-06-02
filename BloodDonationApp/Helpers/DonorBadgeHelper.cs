using System;

namespace BloodDonationApp.Helpers
{
    public class BadgeInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public int NextTierDonations { get; set; }
        public string NextTierName { get; set; } = string.Empty;
        public int DonationsNeeded { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsMaxTier { get; set; }
    }

    public static class DonorBadgeHelper
    {
        public static BadgeInfo GetBadge(int totalDonations)
        {
            var info = new BadgeInfo();

            if (totalDonations <= 0)
            {
                info.Name = "New Donor";
                info.ColorClass = "bg-secondary";
                info.NextTierDonations = 1;
                info.NextTierName = "First Drop";
                info.DonationsNeeded = 1;
                info.ProgressPercentage = 0.0;
                info.IsMaxTier = false;
            }
            else if (totalDonations >= 1 && totalDonations <= 2)
            {
                info.Name = "First Drop";
                info.ColorClass = "bg-primary";
                info.NextTierDonations = 3;
                info.NextTierName = "Regular Hero";
                info.DonationsNeeded = 3 - totalDonations;
                info.ProgressPercentage = (double)totalDonations / 3.0 * 100.0;
                info.IsMaxTier = false;
            }
            else if (totalDonations >= 3 && totalDonations <= 5)
            {
                info.Name = "Regular Hero";
                info.ColorClass = "bg-success";
                info.NextTierDonations = 6;
                info.NextTierName = "Blood Champion";
                info.DonationsNeeded = 6 - totalDonations;
                info.ProgressPercentage = (double)totalDonations / 6.0 * 100.0;
                info.IsMaxTier = false;
            }
            else if (totalDonations >= 6 && totalDonations <= 10)
            {
                info.Name = "Blood Champion";
                info.ColorClass = "bg-warning text-dark";
                info.NextTierDonations = 11;
                info.NextTierName = "Lifesaver Legend";
                info.DonationsNeeded = 11 - totalDonations;
                info.ProgressPercentage = (double)totalDonations / 11.0 * 100.0;
                info.IsMaxTier = false;
            }
            else // 11+
            {
                info.Name = "Lifesaver Legend";
                info.ColorClass = "bg-danger";
                info.NextTierDonations = 11;
                info.NextTierName = string.Empty;
                info.DonationsNeeded = 0;
                info.ProgressPercentage = 100.0;
                info.IsMaxTier = true;
            }

            return info;
        }
    }
}
