using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Data;
using Microsoft.ML;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace BloodDonationApp.Services
{
    public class BloodAnalyticsService : IBloodAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public BloodAnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BloodAnalyticsViewModel> GetAnalyticsAsync()
        {
            var viewModel = new BloodAnalyticsViewModel();

            var today = DateTime.Today;
            var requests = await _context.BloodRequests.AsNoTracking().ToListAsync();
            var inventory = await _context.BloodInventory.AsNoTracking().ToListAsync();

            if (!requests.Any())
            {
                return viewModel;
            }

            // Core KPIs
            viewModel.TotalHistoricalRequests = requests.Count;
            viewModel.TotalHistoricalUnits = requests.Sum(r => r.Units);
            viewModel.AverageRequestSize = viewModel.TotalHistoricalRequests > 0 
                ? Math.Round((double)viewModel.TotalHistoricalUnits / viewModel.TotalHistoricalRequests, 2) 
                : 0;

            // Monthly Insights (last 6 months)
            viewModel.MonthlyLabels = new List<string>();
            viewModel.MonthlyUnits = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = today.AddMonths(-i);
                var label = monthDate.ToString("MMM yyyy");
                var monthRequests = requests.Where(r => r.CreatedAt.Year == monthDate.Year && r.CreatedAt.Month == monthDate.Month).ToList();
                var monthUnitsSum = monthRequests.Sum(r => r.Units);

                viewModel.MonthlyLabels.Add(label);
                viewModel.MonthlyUnits.Add(monthUnitsSum);

                viewModel.MonthlyInsights.Add(new MonthlyInsightModel
                {
                    MonthLabel = label,
                    RequestsCount = monthRequests.Count,
                    TotalUnits = monthUnitsSum,
                    AverageUnits = monthRequests.Count > 0 ? Math.Round((double)monthUnitsSum / monthRequests.Count, 2) : 0
                });
            }

            // Proportions of requests per blood group
            string[] bloodTypes = { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };
            foreach (var bt in bloodTypes)
            {
                var count = requests.Count(r => r.BloodType.Trim() == bt);
                viewModel.BloodGroupDistribution[bt] = count;
            }

            // --- ML.NET Weekly Demand Prediction Pipeline ---
            // Prepare training data (aggregating requests by week of creation)
            var trainingData = new List<BloodDemandData>();
            var last24WeeksRequests = requests.Where(r => r.CreatedAt >= today.AddDays(-168)).ToList();

            for (int w = 0; w < 24; w++)
            {
                var weekStart = today.AddDays(-168 + w * 7);
                var weekEnd = weekStart.AddDays(7);
                var weekRequests = last24WeeksRequests.Where(r => r.CreatedAt >= weekStart && r.CreatedAt < weekEnd).ToList();

                foreach (var bt in bloodTypes)
                {
                    var totalUnits = weekRequests.Where(r => r.BloodType == bt).Sum(r => r.Units);
                    trainingData.Add(new BloodDemandData
                    {
                        BloodType = bt,
                        WeekOffset = w + 1, // 1 to 24
                        TotalUnits = totalUnits
                    });
                }
            }

            // Run ML.NET training
            var mlContext = new MLContext(seed: 42);
            var trainingDataView = mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = mlContext.Transforms.Categorical.OneHotEncoding("BloodTypeEncoded", nameof(BloodDemandData.BloodType))
                .Append(mlContext.Transforms.Concatenate("Features", "BloodTypeEncoded", nameof(BloodDemandData.WeekOffset)))
                .Append(mlContext.Regression.Trainers.LbfgsPoissonRegression(labelColumnName: nameof(BloodDemandData.TotalUnits), featureColumnName: "Features"));

            var model = pipeline.Fit(trainingDataView);
            var predictionEngine = mlContext.Model.CreatePredictionEngine<BloodDemandData, BloodDemandPrediction>(model);

            // Predict demand & shortage for each blood group
            double nextWeekPredictedTotal = 0;
            int optimalStockCount = 0;

            foreach (var bt in bloodTypes)
            {
                var invItem = inventory.FirstOrDefault(i => i.BloodType.Trim() == bt);
                int currentStock = invItem?.UnitsAvailable ?? 0;

                // Predict next 4 weeks
                double pred1Week = 0;
                double pred4WeeksSum = 0;

                for (int w = 1; w <= 4; w++)
                {
                    var prediction = predictionEngine.Predict(new BloodDemandData
                    {
                        BloodType = bt,
                        WeekOffset = 24 + w // Forecast offsets 25, 26, 27, 28
                    });

                    // Avoid negative forecasts from regression anomalies
                    var predictedVal = Math.Max(0, prediction.PredictedUnits);
                    pred4WeeksSum += predictedVal;

                    if (w == 1)
                    {
                        pred1Week = predictedVal;
                    }
                }

                nextWeekPredictedTotal += pred1Week;

                // Determine stock status and alert styles
                double recommendedStock = Math.Round(pred4WeeksSum + 5.0, 1); // 4-week demand + 5 units buffer
                string status;
                string alertClass;

                if (currentStock < pred4WeeksSum / 2)
                {
                    status = "Critical Shortage Warning";
                    alertClass = "danger";
                }
                else if (currentStock < pred4WeeksSum)
                {
                    status = "Low Stock Alert";
                    alertClass = "warning";
                }
                else
                {
                    status = "Optimal";
                    alertClass = "success";
                    optimalStockCount++;
                }

                viewModel.ShortagePredictions.Add(new ShortagePredictionModel
                {
                    BloodType = bt,
                    CurrentStock = currentStock,
                    Predicted1WeekDemand = Math.Round(pred1Week, 1),
                    Predicted4WeekDemand = Math.Round(pred4WeeksSum, 1),
                    RecommendedStockLevel = recommendedStock,
                    StockStatus = status,
                    AlertClass = alertClass
                });

                // Add to high-demand mapping
                viewModel.HighDemandBloodGroups.Add(new HighDemandModel
                {
                    BloodType = bt,
                    PredictedWeeklyDemand = Math.Round(pred1Week, 1),
                    DemandLevel = pred1Week >= 3 ? "High" : (pred1Week >= 1.5 ? "Medium" : "Low")
                });
            }

            viewModel.NextWeekPredictedTotalUnits = Math.Round(nextWeekPredictedTotal, 1);
            viewModel.StockHealthIndex = bloodTypes.Length > 0 
                ? Math.Round(((double)optimalStockCount / bloodTypes.Length) * 100, 1) 
                : 100;

            // Sort high-demand list by descending predicted demand
            viewModel.HighDemandBloodGroups = viewModel.HighDemandBloodGroups
                .OrderByDescending(h => h.PredictedWeeklyDemand)
                .ToList();

            // Populate additional platform statistics for general dashboard
            var donations = await _context.DonationRecords.AsNoTracking().ToListAsync();
            var donors = await _context.Donors.AsNoTracking().ToListAsync();

            viewModel.DonationsLabels = new List<string>();
            viewModel.DonationsUnits = new List<int>();
            for (int i = 11; i >= 0; i--)
            {
                var monthDate = today.AddMonths(-i);
                var label = monthDate.ToString("MMM yyyy");
                var monthDonations = donations.Where(d => d.DonationDate.Year == monthDate.Year && d.DonationDate.Month == monthDate.Month && string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase)).ToList();
                viewModel.DonationsLabels.Add(label);
                viewModel.DonationsUnits.Add(monthDonations.Count);
            }

            foreach (var bt in bloodTypes)
            {
                var donorCount = donors.Count(d => d.BloodType.Trim().ToUpper() == bt);
                viewModel.DonorBloodGroupDistribution[bt] = donorCount;
            }

            viewModel.RequestFulfillmentStats["Fulfilled"] = requests.Count(r => string.Equals(r.Status, "Fulfilled", StringComparison.OrdinalIgnoreCase));
            viewModel.RequestFulfillmentStats["Pending"] = requests.Count(r => string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Status, "Emergency", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase));
            viewModel.RequestFulfillmentStats["Cancelled"] = requests.Count(r => string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase));

            viewModel.ActiveDonorsCount = donors.Count(d => d.IsAvailable);
            viewModel.BusyDonorsCount = donors.Count(d => !d.IsAvailable);

            return viewModel;
        }
    }

    public class BloodDemandData
    {
        public float TotalUnits { get; set; }
        public string BloodType { get; set; } = string.Empty;
        public float WeekOffset { get; set; }
    }

    public class BloodDemandPrediction
    {
        [Microsoft.ML.Data.ColumnName("Score")]
        public float PredictedUnits { get; set; }
    }
}
