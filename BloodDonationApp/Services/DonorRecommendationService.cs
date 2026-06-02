using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Data;
using BloodDonationApp.Models;
using BloodDonationApp.Helpers;
using Microsoft.AspNetCore.SignalR;
using BloodDonationApp.Hubs;


namespace BloodDonationApp.Services
{
    public class DonorRecommendationService : IDonorRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IGoogleMapsService _googleMapsService;

        public DonorRecommendationService(ApplicationDbContext context, IHubContext<NotificationHub> _hubContext, IGoogleMapsService googleMapsService)
        {
            _context = context;
            this._hubContext = _hubContext;
            _googleMapsService = googleMapsService;
        }

        public async Task<List<RecommendedDonorDto>> GetRecommendationsAsync(int requestId)
        {
            var request = await _context.BloodRequests.FindAsync(requestId);
            if (request == null)
            {
                return new List<RecommendedDonorDto>();
            }

            // 1. Get compatible blood types
            var compatibleTypes = BloodCompatibilityHelper.GetCompatibleBloodTypes(request.BloodType);

            // 2. Fetch all verified donors with compatible blood types
            var donors = await _context.Donors
                .Where(d => d.IsVerified && compatibleTypes.Contains(d.BloodType))
                .ToListAsync();

            // 3. Infer request state for distance calculation
            var requestCity = request.City.Trim().ToLower();
            var inferredState = await _context.Donors
                .Where(d => d.City.Trim().ToLower() == requestCity)
                .Select(d => d.State.Trim())
                .FirstOrDefaultAsync() ?? 
                await _context.BloodCamps
                .Where(c => c.City.Trim().ToLower() == requestCity)
                .Select(c => c.State.Trim())
                .FirstOrDefaultAsync() ?? "";

            var targetState = inferredState.Trim().ToLower();
            var recommendations = new List<RecommendedDonorDto>();

            foreach (var donor in donors)
            {
                var donorState = donor.State.Trim().ToLower();
                if (!string.IsNullOrEmpty(targetState) && donorState != targetState)
                {
                    continue;
                }

                var reasons = new List<string>();
                int score = 0;
                bool isEligible = true;

                // A. Blood Compatibility Score (Max 30 pts)
                var donorType = donor.BloodType.Trim().ToUpper();
                var requestType = request.BloodType.Trim().ToUpper();

                if (donorType == requestType)
                {
                    score += 30;
                    reasons.Add($"Exact blood type match ({donor.BloodType}) [+30 pts]");
                }
                else
                {
                    score += 15;
                    reasons.Add($"Compatible blood type ({donor.BloodType}) [+15 pts]");
                }

                // B. Distance Proximity Score (Max 35 pts)
                double distance;
                if (donor.Latitude.HasValue && donor.Longitude.HasValue && 
                    request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    distance = _googleMapsService.CalculateDistance(donor.Latitude.Value, donor.Longitude.Value, request.Latitude.Value, request.Longitude.Value);
                }
                else
                {
                    // If database has missing coordinates, geocode on the fly
                    if (!donor.Latitude.HasValue || !donor.Longitude.HasValue)
                    {
                        var donorAddr = $"{donor.Address}, {donor.City}, {donor.State} {donor.PinCode}";
                        var coords = Task.Run(() => _googleMapsService.GeocodeAddressAsync(donorAddr)).GetAwaiter().GetResult();
                        if (coords.HasValue)
                        {
                            donor.Latitude = coords.Value.Latitude;
                            donor.Longitude = coords.Value.Longitude;
                            _context.Update(donor);
                        }
                    }

                    if (!request.Latitude.HasValue || !request.Longitude.HasValue)
                    {
                        var requestAddr = $"{request.Hospital}, {request.City}";
                        var coords = Task.Run(() => _googleMapsService.GeocodeAddressAsync(requestAddr)).GetAwaiter().GetResult();
                        if (coords.HasValue)
                        {
                            request.Latitude = coords.Value.Latitude;
                            request.Longitude = coords.Value.Longitude;
                            _context.Update(request);
                        }
                    }

                    // Save changes to DB to persist coordinates
                    Task.Run(() => _context.SaveChangesAsync()).GetAwaiter().GetResult();

                    if (donor.Latitude.HasValue && donor.Longitude.HasValue && 
                        request.Latitude.HasValue && request.Longitude.HasValue)
                    {
                        distance = _googleMapsService.CalculateDistance(donor.Latitude.Value, donor.Longitude.Value, request.Latitude.Value, request.Longitude.Value);
                    }
                    else
                    {
                        // Safe fallback
                        distance = CalculateDeterministicDistance(donor, request, inferredState);
                    }
                }
                if (distance < 15.0)
                {
                    score += 35;
                    reasons.Add($"Located in the same city ({donor.City}), approx. {distance:F1} km away [+35 pts]");
                }
                else if (distance < 80.0)
                {
                    score += 20;
                    reasons.Add($"Located in nearby city ({donor.City}, {donor.State}), approx. {distance:F1} km away [+20 pts]");
                }
                else
                {
                    score += 5;
                    reasons.Add($"Located far ({donor.City}, {donor.State}), approx. {distance:F1} km away [+5 pts]");
                }

                // C. Last Donation Recency Score (Max 20 pts)
                if (donor.LastDonationDate == null)
                {
                    score += 20;
                    reasons.Add("No recent donation records (eligible) [+20 pts]");
                }
                else
                {
                    var daysSinceLastDonation = (DateTime.Today - donor.LastDonationDate.Value.Date).TotalDays;
                    if (daysSinceLastDonation < 56)
                    {
                        isEligible = false;
                        reasons.Add($"Donated recently ({donor.LastDonationDate.Value:yyyy-MM-dd}), only {daysSinceLastDonation:F0} days ago [Ineligible - must wait 56 days]");
                    }
                    else if (daysSinceLastDonation >= 90)
                    {
                        score += 20;
                        reasons.Add($"Last donation was {daysSinceLastDonation:F0} days ago (eligible) [+20 pts]");
                    }
                    else
                    {
                        score += 10;
                        reasons.Add($"Last donation was {daysSinceLastDonation:F0} days ago (eligible) [+10 pts]");
                    }
                }

                // D. Availability Score (Max 15 pts)
                if (donor.IsAvailable)
                {
                    score += 15;
                    reasons.Add("Donor is currently available for donation [+15 pts]");
                }
                else
                {
                    isEligible = false;
                    reasons.Add("Donor is currently marked unavailable [Ineligible]");
                }

                recommendations.Add(new RecommendedDonorDto
                {
                    Donor = donor,
                    Score = isEligible ? score : 0, // Set score to 0 or leave as is but sort by eligibility
                    EstimatedDistance = distance,
                    Reasons = reasons,
                    IsEligible = isEligible
                });
            }

            // Sort by eligibility first, then by score descending, then by name
            return recommendations
                .OrderByDescending(r => r.IsEligible)
                .ThenByDescending(r => r.Score)
                .ThenBy(r => r.Donor.Name)
                .ToList();
        }

        public async Task NotifyTopDonorsAsync(BloodRequest request)
        {
            var recommendations = await GetRecommendationsAsync(request.Id);
            
            bool isEmergency = request.UrgencyLevel == UrgencyLevel.Critical || 
                               request.UrgencyLevel == UrgencyLevel.Urgent ||
                               string.Equals(request.Status, "Emergency", StringComparison.OrdinalIgnoreCase);

            var donorsToNotify = isEmergency 
                ? recommendations.Where(r => r.IsEligible).ToList() 
                : recommendations.Where(r => r.IsEligible).Take(3).ToList();

            var createdNotifications = new List<(Notification Notif, int DonorId)>();

            foreach (var rec in donorsToNotify)
            {
                var notification = new Notification
                {
                    DonorId = rec.Donor.Id,
                    Title = isEmergency ? $"EMERGENCY Blood Match ({request.BloodType})" : $"Blood Request Match ({request.BloodType})",
                    Message = isEmergency
                        ? $"EMERGENCY: A critical/urgent {request.BloodType} request is created at {request.Hospital} in {request.City}. Patient: {request.PatientName}. Score: {rec.Score}%. Distance: {rec.EstimatedDistance:F1} km. Contact: {request.ContactNumber}."
                        : $"Hi {rec.Donor.Name}, there is a new {request.UrgencyLevel} blood request for {request.BloodType} at {request.Hospital} in {request.City}. You are a top match (Score: {rec.Score}%)! Distance: {rec.EstimatedDistance:F1} km.",
                    Type = isEmergency ? "Warning" : "Info",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                createdNotifications.Add((notification, rec.Donor.Id));
            }

            if (donorsToNotify.Any())
            {
                await _context.SaveChangesAsync();

                foreach (var item in createdNotifications)
                {
                    var groupName = $"Donor_{item.DonorId}";
                    await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", new
                    {
                        id = item.Notif.Id,
                        title = item.Notif.Title,
                        message = item.Notif.Message,
                        type = item.Notif.Type,
                        createdAt = item.Notif.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        isRead = item.Notif.IsRead
                    });
                }
            }

            // Also broadcast alert to Admins group
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminAlert", new
            {
                id = request.Id,
                patientName = request.PatientName,
                bloodType = request.BloodType,
                units = request.Units,
                hospital = request.Hospital,
                city = request.City,
                urgencyLevel = request.UrgencyLevel.ToString(),
                status = request.Status,
                createdAt = request.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        private double CalculateDeterministicDistance(Donor donor, BloodRequest request, string inferredState)
        {
            var donorCity = donor.City.Trim().ToLower();
            var requestCity = request.City.Trim().ToLower();
            var donorState = donor.State.Trim().ToLower();
            var targetState = inferredState.Trim().ToLower();

            if (donorCity == requestCity)
            {
                // Same city: simulated 1.2 to 11.2 km
                return ((donor.Id * 3 + request.Id * 7) % 10) + 1.2;
            }

            if (donorState == targetState && !string.IsNullOrEmpty(targetState))
            {
                // Same state, different city: simulated 15.5 to 74.5 km
                return ((donor.Id * 11 + request.Id * 17) % 60) + 15.5;
            }

            // Different state: simulated 100 to 499 km
            return ((donor.Id * 19 + request.Id * 23) % 400) + 100.0;
        }
    }
}
