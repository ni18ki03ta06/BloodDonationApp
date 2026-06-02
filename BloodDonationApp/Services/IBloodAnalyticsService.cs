using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BloodDonationApp.Services
{
    public interface IBloodAnalyticsService
    {
        Task<BloodAnalyticsViewModel> GetAnalyticsAsync();
    }

    public class BloodAnalyticsViewModel
    {
        // Core KPIs
        public int TotalHistoricalRequests { get; set; }
        public int TotalHistoricalUnits { get; set; }
        public double AverageRequestSize { get; set; }
        public double NextWeekPredictedTotalUnits { get; set; }
        public double StockHealthIndex { get; set; } // Percentage of blood groups with Optimal stock

        // Shortage Predictions
        public List<ShortagePredictionModel> ShortagePredictions { get; set; } = new();

        // High-Demand Blood Groups
        public List<HighDemandModel> HighDemandBloodGroups { get; set; } = new();

        // Monthly Insights (last 6 months)
        public List<MonthlyInsightModel> MonthlyInsights { get; set; } = new();

        // Proportions for doughnut chart
        public Dictionary<string, int> BloodGroupDistribution { get; set; } = new();

        // Charts serializable data
        public List<string> MonthlyLabels { get; set; } = new();
        public List<int> MonthlyUnits { get; set; } = new();

        // New Platform Stats properties for general admin dashboard
        public List<string> DonationsLabels { get; set; } = new();
        public List<int> DonationsUnits { get; set; } = new();
        public Dictionary<string, int> DonorBloodGroupDistribution { get; set; } = new();
        public Dictionary<string, int> RequestFulfillmentStats { get; set; } = new();
        public int ActiveDonorsCount { get; set; }
        public int BusyDonorsCount { get; set; }
    }

    public class ShortagePredictionModel
    {
        public string BloodType { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public double Predicted1WeekDemand { get; set; }
        public double Predicted4WeekDemand { get; set; }
        public double RecommendedStockLevel { get; set; } // Predicted4WeekDemand + 5 safety units
        public string StockStatus { get; set; } = string.Empty; // "Optimal", "Low Stock Alert", "Critical Shortage Warning"
        public string AlertClass { get; set; } = string.Empty; // "success", "warning", "danger"
    }

    public class HighDemandModel
    {
        public string BloodType { get; set; } = string.Empty;
        public double PredictedWeeklyDemand { get; set; }
        public string DemandLevel { get; set; } = string.Empty; // "High", "Medium", "Low"
    }

    public class MonthlyInsightModel
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int RequestsCount { get; set; }
        public int TotalUnits { get; set; }
        public double AverageUnits { get; set; }
    }
}
