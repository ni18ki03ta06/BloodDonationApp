using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Models;
using BloodDonationApp.Filters;
using BloodDonationApp.Helpers;
using BloodDonationApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using BloodDonationApp.Hubs;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Core.Interfaces;
using AutoMapper;

namespace BloodDonationApp.Controllers
{
    [AuthFilter(RequiredRole = "Admin")]
    public class AdminController : Controller
    {
        private readonly IDonorService _donorService;
        private readonly IBloodRequestService _bloodRequestService;
        private readonly IAppointmentService _appointmentService;
        private readonly IBloodInventoryService _bloodInventoryService;
        private readonly IDonorRecommendationService _recommendationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IBloodAnalyticsService _analyticsService;
        private readonly IGamificationService _gamificationService;
        private readonly IRepository<Admin> _adminRepository;
        private readonly IRepository<DonationRecord> _donationRecordRepository;
        private readonly IRepository<BloodCamp> _bloodCampRepository;
        private readonly IMapper _mapper;

        public AdminController(
            IDonorService donorService,
            IBloodRequestService bloodRequestService,
            IAppointmentService appointmentService,
            IBloodInventoryService bloodInventoryService,
            IDonorRecommendationService recommendationService,
            IHubContext<NotificationHub> hubContext,
            IBloodAnalyticsService analyticsService,
            IGamificationService gamificationService,
            IRepository<Admin> adminRepository,
            IRepository<DonationRecord> donationRecordRepository,
            IRepository<BloodCamp> bloodCampRepository,
            IMapper mapper)
        {
            _donorService = donorService;
            _bloodRequestService = bloodRequestService;
            _appointmentService = appointmentService;
            _bloodInventoryService = bloodInventoryService;
            _recommendationService = recommendationService;
            _hubContext = hubContext;
            _analyticsService = analyticsService;
            _gamificationService = gamificationService;
            _adminRepository = adminRepository;
            _donationRecordRepository = donationRecordRepository;
            _bloodCampRepository = bloodCampRepository;
            _mapper = mapper;
        }

        // GET: Admin/Index (Dashboard Landing)
        public async Task<IActionResult> Index()
        {
            try
            {
                var donorsDto = await _donorService.GetAllDonorsAsync();
                var donorsList = _mapper.Map<List<Donor>>(donorsDto);
                var donorsCount = donorsList.Count;

                var requestsDto = await _bloodRequestService.GetAllRequestsAsync();
                var requests = _mapper.Map<List<BloodRequest>>(requestsDto);

                ViewBag.EmergencyCount = requests.Count(r => r.Status == "Emergency");
                ViewBag.ApprovedCount = requests.Count(r => r.Status == "Approved" || r.Status == "Fulfilled");
                ViewBag.PendingCount = requests.Count(r => r.Status == "Pending");
                ViewBag.RejectedCount = requests.Count(r => r.Status == "Rejected");
                ViewBag.TotalDonors = donorsCount;

                var inventoryDto = await _bloodInventoryService.GetAllInventoryAsync();
                var inventory = _mapper.Map<List<BloodInventory>>(inventoryDto);
                ViewBag.BloodInventory = inventory;

                // Group donors by BloodType for dashboard bar chart
                var bloodTypes = new[] { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };
                var bloodTypeStats = bloodTypes.ToDictionary(t => t, t => donorsList.Count(d => d.BloodType.Trim() == t));
                ViewBag.BloodTypeStats = bloodTypeStats;

                // Group donors by registration month (last 6 months) for dashboard line chart
                var registrationStats = new Dictionary<string, int>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthDate = DateTime.Today.AddMonths(-i);
                    var label = monthDate.ToString("MMM yyyy");
                    var count = donorsList.Count(d => d.CreatedAt.Year == monthDate.Year && d.CreatedAt.Month == monthDate.Month);
                    registrationStats[label] = count;
                }
                ViewBag.RegistrationStats = registrationStats;

                // Populate active recommendations for pending or emergency requests (max 3)
                var activeRequests = requests
                    .Where(r => r.Status == "Pending" || r.Status == "Emergency")
                    .OrderByDescending(r => r.UrgencyLevel)
                    .ThenBy(r => r.RequiredDate)
                    .Take(3)
                    .ToList();

                var activeRecommendations = new List<ActiveRequestRecommendationViewModel>();
                foreach (var req in activeRequests)
                {
                    var recs = await _recommendationService.GetRecommendationsAsync(req.Id);
                    var topDonor = recs.FirstOrDefault(r => r.IsEligible);
                    activeRecommendations.Add(new ActiveRequestRecommendationViewModel
                    {
                        Request = req,
                        TopDonor = topDonor
                    });
                }

                // Pass recent activities/requests to dashboard
                var viewModel = new AdminDashboardViewModel
                {
                    Donors = donorsList.Take(5).ToList(),
                    Requests = requests.OrderByDescending(r => r.RequiredDate).Take(5).ToList(),
                    ActiveRecommendations = activeRecommendations
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading admin dashboard index.");
                TempData["ErrorMessage"] = "A database error occurred while loading the admin dashboard.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/Analytics (ML Analytics Dashboard)
        public async Task<IActionResult> Analytics()
        {
            try
            {
                var viewModel = await _analyticsService.GetAnalyticsAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error loading admin analytics dashboard.");
                TempData["ErrorMessage"] = "An error occurred while loading the ML analytics dashboard.";
                return RedirectToAction("Index");
            }
        }

        // GET: Admin/BloodRequests
        public async Task<IActionResult> BloodRequests(string? status, string? urgency)
        {
            try
            {
                var requestsDto = await _bloodRequestService.GetRequestsFilteredAsync(status, urgency);
                var requests = _mapper.Map<List<BloodRequest>>(requestsDto);

                var sortedRequests = requests
                    .OrderByDescending(r => r.UrgencyLevel)
                    .ThenBy(r => r.RequiredDate)
                    .ToList();

                ViewBag.StatusFilter = status;
                ViewBag.UrgencyFilter = urgency;

                return View(sortedRequests);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading blood requests status: {Status}, urgency: {Urgency}", status, urgency);
                TempData["ErrorMessage"] = "A database error occurred while fetching blood requests.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Admin/ApproveRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            try
            {
                var requestDto = await _bloodRequestService.GetRequestByIdAsync(id);
                if (requestDto == null) return NotFound();

                var success = await _bloodRequestService.ApproveRequestAsync(id);
                if (!success) return BadRequest("Could not approve blood request.");

                AuditService.LogAction(null, HttpContext, "ApproveRequest", "BloodRequest", id, $"Approved blood request for {requestDto.PatientName} ({requestDto.BloodType}, {requestDto.Units} units).");

                TempData["SuccessMessage"] = $"Blood request for {requestDto.PatientName} has been approved.";
                return RedirectToAction(nameof(BloodRequests));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error approving blood request with ID: {RequestId}", id);
                TempData["ErrorMessage"] = "A database error occurred while approving the request.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Admin/RejectRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id)
        {
            try
            {
                var requestDto = await _bloodRequestService.GetRequestByIdAsync(id);
                if (requestDto == null) return NotFound();

                var success = await _bloodRequestService.RejectRequestAsync(id);
                if (!success) return BadRequest("Could not reject blood request.");

                AuditService.LogAction(null, HttpContext, "RejectRequest", "BloodRequest", id, $"Rejected blood request for {requestDto.PatientName} ({requestDto.BloodType}, {requestDto.Units} units).");

                TempData["SuccessMessage"] = $"Blood request for {requestDto.PatientName} has been rejected.";
                return RedirectToAction(nameof(BloodRequests));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error rejecting blood request with ID: {RequestId}", id);
                TempData["ErrorMessage"] = "A database error occurred while rejecting the request.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Admin/MakeEmergency
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeEmergency(int id)
        {
            try
            {
                var requestDto = await _bloodRequestService.GetRequestByIdAsync(id);
                if (requestDto == null) return NotFound();

                var success = await _bloodRequestService.MakeEmergencyAsync(id);
                if (!success) return BadRequest("Could not elevate blood request to Emergency.");

                var request = _mapper.Map<BloodRequest>(requestDto);
                request.Status = "Emergency";
                request.UrgencyLevel = UrgencyLevel.Critical;

                AuditService.LogAction(null, HttpContext, "MakeEmergency", "BloodRequest", id, $"Marked blood request for {request.PatientName} ({request.BloodType}, {request.Units} units) as Emergency.");

                // Trigger real-time notifications for matching donors
                await _recommendationService.NotifyTopDonorsAsync(request);

                TempData["SuccessMessage"] = $"Blood request for {request.PatientName} marked as Emergency and all matching donors notified!";
                return RedirectToAction(nameof(BloodRequests));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error marking request {RequestId} as emergency.", id);
                TempData["ErrorMessage"] = "A database error occurred while making the request an emergency.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            try
            {
                var donorsDto = await _donorService.GetAllDonorsAsync();
                var users = _mapper.Map<List<Donor>>(donorsDto);
                return View(users);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error retrieving users/donors list.");
                TempData["ErrorMessage"] = "A database error occurred while loading users.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/Reports
        public async Task<IActionResult> Reports(int? filterMonth, int? filterYear)
        {
            try
            {
                var donorsDto = await _donorService.GetAllDonorsAsync();
                var donors = _mapper.Map<List<Donor>>(donorsDto);

                var requestsDto = await _bloodRequestService.GetAllRequestsAsync();
                var requests = _mapper.Map<List<BloodRequest>>(requestsDto);

                var donationRecords = (await _donationRecordRepository.GetAllAsync()).ToList();

                var inventoryDto = await _bloodInventoryService.GetAllInventoryAsync();
                var inventory = _mapper.Map<List<BloodInventory>>(inventoryDto);

                // Apply filters
                if (filterMonth.HasValue && filterMonth.Value >= 1 && filterMonth.Value <= 12)
                {
                    donors = donors.Where(d => d.CreatedAt.Month == filterMonth.Value).ToList();
                    requests = requests.Where(r => r.CreatedAt.Month == filterMonth.Value).ToList();
                    donationRecords = donationRecords.Where(r => r.DonationDate.Month == filterMonth.Value).ToList();
                }

                if (filterYear.HasValue)
                {
                    donors = donors.Where(d => d.CreatedAt.Year == filterYear.Value).ToList();
                    requests = requests.Where(r => r.CreatedAt.Year == filterYear.Value).ToList();
                    donationRecords = donationRecords.Where(r => r.DonationDate.Year == filterYear.Value).ToList();
                }

                // Totals & Statuses
                ViewBag.TotalDonors = donors.Count;
                ViewBag.AvailableDonors = donors.Count(d => d.IsAvailable);
                ViewBag.TotalRequests = requests.Count;
                ViewBag.PendingRequests = requests.Count(r => r.Status == "Pending");
                ViewBag.ApprovedRequests = requests.Count(r => r.Status == "Approved" || r.Status == "Fulfilled");
                ViewBag.EmergencyRequests = requests.Count(r => r.Status == "Emergency");
                ViewBag.RejectedRequests = requests.Count(r => r.Status == "Rejected");

                // Donation Metrics
                ViewBag.TotalDonationsCount = donationRecords.Count;
                ViewBag.CompletedDonationsCount = donationRecords.Count(dr => dr.Status == "Completed");
                ViewBag.PendingDonationsCount = donationRecords.Count(dr => dr.Status == "Pending");
                ViewBag.CancelledDonationsCount = donationRecords.Count(dr => dr.Status == "Cancelled");
                ViewBag.TotalUnitsDonated = donationRecords.Where(dr => dr.Status == "Completed").Sum(dr => dr.Units);

                // Group donors by BloodType for report
                var bloodTypeStats = donors.GroupBy(d => d.BloodType)
                                           .Select(g => new { BloodType = g.Key, Count = g.Count() })
                                           .ToDictionary(k => k.BloodType, v => v.Count);
                ViewBag.BloodTypeStats = bloodTypeStats;

                // Group donors by badge tier for report
                var tierStats = new Dictionary<string, int>
                {
                    { "New Donor", 0 },
                    { "First Drop", 0 },
                    { "Regular Hero", 0 },
                    { "Blood Champion", 0 },
                    { "Lifesaver Legend", 0 }
                };

                foreach (var donor in donors)
                {
                    var badge = BloodDonationApp.Helpers.DonorBadgeHelper.GetBadge(donor.TotalDonations);
                    if (tierStats.ContainsKey(badge.Name))
                    {
                        tierStats[badge.Name]++;
                    }
                }
                ViewBag.TierStats = tierStats;

                // Blood Inventory available stats
                ViewBag.Inventory = inventory;

                // Group donation records by BloodType
                var donationTypeStats = donationRecords.Where(dr => dr.Status == "Completed")
                                                       .GroupBy(dr => dr.BloodType)
                                                       .Select(g => new { BloodType = g.Key, Units = g.Sum(dr => dr.Units) })
                                                       .ToDictionary(k => k.BloodType, v => v.Units);
                ViewBag.DonationTypeStats = donationTypeStats;

                // Group requests by month for donation/request trend charts (Last 6 months)
                var monthlyRequestStats = new Dictionary<string, int>();
                var monthlyDonationStats = new Dictionary<string, int>();

                // Get all-time records to build last 6 months trend accurately regardless of filter
                var allRequestsDto = await _bloodRequestService.GetAllRequestsAsync();
                var allRequests = _mapper.Map<List<BloodRequest>>(allRequestsDto);
                var allDonations = (await _donationRecordRepository.GetAllAsync()).Where(dr => dr.Status == "Completed").ToList();

                for (int i = 5; i >= 0; i--)
                {
                    var monthDate = DateTime.Today.AddMonths(-i);
                    var label = monthDate.ToString("MMM yyyy");
                    
                    monthlyRequestStats[label] = allRequests.Count(r => r.CreatedAt.Year == monthDate.Year && r.CreatedAt.Month == monthDate.Month);
                    monthlyDonationStats[label] = allDonations.Count(dr => dr.DonationDate.Year == monthDate.Year && dr.DonationDate.Month == monthDate.Month);
                }
                ViewBag.MonthlyRequestStats = monthlyRequestStats;
                ViewBag.MonthlyDonationStats = monthlyDonationStats;

                // Pass filters back to view
                ViewBag.FilterMonth = filterMonth;
                ViewBag.FilterYear = filterYear;

                // Build a list of available years in database to populate filter dropdown
                var minDonorYear = donors.OrderBy(d => d.CreatedAt).Select(d => (int?)d.CreatedAt.Year).FirstOrDefault() ?? DateTime.Today.Year - 1;
                var minRequestYear = requests.OrderBy(r => r.CreatedAt).Select(r => (int?)r.CreatedAt.Year).FirstOrDefault() ?? DateTime.Today.Year - 1;
                var minYear = Math.Min(minDonorYear, minRequestYear);
                if (minYear < 2000) minYear = 2000;
                var maxYear = DateTime.Today.Year;
                var years = Enumerable.Range(minYear, maxYear - minYear + 1).ToList();
                ViewBag.AvailableYears = years;

                return View();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error generating reports.");
                TempData["ErrorMessage"] = "A database error occurred while generating reports.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/Feedback
        public async Task<IActionResult> Feedback()
        {
            try
            {
                var feedbacks = await _donorService.GetAllFeedbackAsync();
                return View(feedbacks);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error fetching feedbacks list.");
                TempData["ErrorMessage"] = "A database error occurred while fetching feedback records.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Admin/MarkFeedbackRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFeedbackRead(int id)
        {
            try
            {
                var success = await _donorService.MarkFeedbackReadAsync(id);
                if (!success) return NotFound();

                TempData["SuccessMessage"] = "Feedback marked as read.";
                return RedirectToAction(nameof(Feedback));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error marking feedback {FeedbackId} as read.", id);
                TempData["ErrorMessage"] = "A database error occurred while updating feedback status.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/ManageDonors
        public async Task<IActionResult> ManageDonors()
        {
            try
            {
                var donorsDto = await _donorService.GetAllDonorsAsync();
                var donors = _mapper.Map<List<Donor>>(donorsDto);
                return View(donors);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading donors management list.");
                TempData["ErrorMessage"] = "A database error occurred while loading donors.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/EditDonor
        public async Task<IActionResult> EditDonor(int? id)
        {
            try
            {
                if (id == null) return NotFound();
                var donorDto = await _donorService.GetDonorProfileAsync(id.Value);
                if (donorDto == null) return NotFound();
                var donor = _mapper.Map<Donor>(donorDto);
                return View(donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading donor {DonorId} for editing.", id);
                TempData["ErrorMessage"] = "A database error occurred while retrieving donor details.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Admin/EditDonor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDonor(int id, [Bind("Id,Name,BloodType,Phone,Email,City,LastDonationDate,IsAvailable")] Donor donor)
        {
            if (id != donor.Id) return NotFound();

            try
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
                ModelState.Remove("Age");
                ModelState.Remove("Gender");
                ModelState.Remove("Address");
                ModelState.Remove("State");
                ModelState.Remove("PinCode");

                if (ModelState.IsValid)
                {
                    var updated = await _donorService.UpdateProfileAsync(id, donor);
                    if (updated == null) return NotFound();

                    AuditService.LogAction(null, HttpContext, "EditDonor", "Donor", id, $"Updated donor: {donor.Name} ({donor.Email}, {donor.BloodType}).");
                    TempData["SuccessMessage"] = "Donor updated successfully.";
                    return RedirectToAction(nameof(ManageDonors));
                }
                return View(donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error saving updates for donor {DonorId}.", id);
                TempData["ErrorMessage"] = "A database error occurred while updating donor information.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Admin/DeleteDonor
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter(RequiredRole = "SuperAdmin")]
        public async Task<IActionResult> DeleteDonor(int id)
        {
            try
            {
                var donorDto = await _donorService.GetDonorProfileAsync(id);
                if (donorDto == null) return NotFound();

                var details = $"Deleted donor: {donorDto.Name} ({donorDto.Email}, {donorDto.BloodType}).";

                var success = await _donorService.DeleteDonorAsync(id);
                if (!success) return BadRequest("Could not delete donor.");

                AuditService.LogAction(null, HttpContext, "DeleteDonor", "Donor", id, details);

                TempData["SuccessMessage"] = "Donor profile deleted.";
                return RedirectToAction(nameof(ManageDonors));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error deleting donor {DonorId}.", id);
                TempData["ErrorMessage"] = "A database error occurred while deleting the donor.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/ManageAdmins (SuperAdmin only)
        [AuthFilter(RequiredRole = "SuperAdmin")]
        public async Task<IActionResult> ManageAdmins()
        {
            try
            {
                var admins = (await _adminRepository.GetAllAsync()).OrderByDescending(a => a.CreatedAt).ToList();
                return View(admins);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error retrieving admin list.");
                TempData["ErrorMessage"] = "A database error occurred while loading admins.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/Settings
        public IActionResult Settings()
        {
            return View();
        }

        // GET: Admin/MatchDonors/{requestId}
        [HttpGet("Admin/MatchDonors/{requestId}")]
        public async Task<IActionResult> MatchDonors(int requestId)
        {
            try
            {
                var requestDto = await _bloodRequestService.GetRequestByIdAsync(requestId);
                if (requestDto == null)
                {
                    return NotFound();
                }
                var request = _mapper.Map<BloodRequest>(requestDto);

                var rankedDonors = await _recommendationService.GetRecommendationsAsync(requestId);

                // Infer state of the request based on any Donor or BloodCamp in the same city
                var cityTrimmed = request.City.Trim();
                var inferredState = "";

                var donorsDto = await _donorService.GetAllDonorsAsync();
                var sameCityDonor = donorsDto
                    .FirstOrDefault(d => d.City != null && d.City.Trim().ToLower() == cityTrimmed.ToLower());
                if (sameCityDonor != null)
                {
                    var fullDonor = await _donorService.GetDonorProfileAsync(sameCityDonor.Id);
                    inferredState = fullDonor?.State?.Trim() ?? "";
                }
                else
                {
                    var campsList = await _bloodCampRepository.GetAllAsync();
                    var sameCityCamp = campsList
                        .FirstOrDefault(c => c.City.Trim().ToLower() == cityTrimmed.ToLower());
                    if (sameCityCamp != null)
                    {
                        inferredState = sameCityCamp.State.Trim();
                    }
                }

                var viewModel = new MatchDonorsViewModel
                {
                    BloodRequest = request,
                    RankedDonors = rankedDonors,
                    InferredState = inferredState
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error matching donors for request {RequestId}.", requestId);
                TempData["ErrorMessage"] = "A database error occurred while matching donors.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Admin/AssignDonor
        [HttpPost("Admin/AssignDonor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDonor(int requestId, int donorId)
        {
            try
            {
                var requestDto = await _bloodRequestService.GetRequestByIdAsync(requestId);
                var donorDto = await _donorService.GetDonorProfileAsync(donorId);

                if (requestDto == null || donorDto == null)
                {
                    return NotFound();
                }

                var success = await _bloodRequestService.AssignDonorAsync(requestId, donorId);
                if (!success) return BadRequest("Could not assign donor.");

                TempData["SuccessMessage"] = $"Donor {donorDto.Name} has been successfully assigned to fulfill the request for {requestDto.PatientName}.";
                return RedirectToAction(nameof(BloodRequests));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error assigning donor {DonorId} to request {RequestId}.", donorId, requestId);
                TempData["ErrorMessage"] = "A database error occurred while assigning the donor.";
                return RedirectToAction("Error", "Home");
            }
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        // GET: Admin/ExportDonors
        [HttpGet]
        public async Task<IActionResult> ExportDonors()
        {
            try
            {
                var donorsDto = await _donorService.GetAllDonorsAsync();
                var donors = _mapper.Map<List<Donor>>(donorsDto);
                var builder = new StringBuilder();
                builder.AppendLine("Id,Name,BloodType,Phone,Email,City,State,TotalDonations,IsAvailable,IsVerified,CreatedAt");
                foreach (var d in donors)
                {
                    builder.AppendLine($"{d.Id},{EscapeCsvField(d.Name)},{EscapeCsvField(d.BloodType)},{EscapeCsvField(d.Phone)},{EscapeCsvField(d.Email)},{EscapeCsvField(d.City)},{EscapeCsvField(d.State)},{d.TotalDonations},{d.IsAvailable},{d.IsVerified},{d.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "donors_export.csv");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error exporting donors.");
                TempData["ErrorMessage"] = "A database error occurred while exporting donors.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/ExportRequests
        [HttpGet]
        public async Task<IActionResult> ExportRequests()
        {
            try
            {
                var requestsDto = await _bloodRequestService.GetAllRequestsAsync();
                var requests = _mapper.Map<List<BloodRequest>>(requestsDto);
                var builder = new StringBuilder();
                builder.AppendLine("Id,PatientName,BloodType,Units,Hospital,City,UrgencyLevel,Status,RequiredDate,CreatedAt");
                foreach (var r in requests)
                {
                    builder.AppendLine($"{r.Id},{EscapeCsvField(r.PatientName)},{EscapeCsvField(r.BloodType)},{r.Units},{EscapeCsvField(r.Hospital)},{EscapeCsvField(r.City)},{EscapeCsvField(r.UrgencyLevel.ToString())},{EscapeCsvField(r.Status)},{r.RequiredDate:yyyy-MM-dd},{r.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "requests_export.csv");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error exporting requests.");
                TempData["ErrorMessage"] = "A database error occurred while exporting requests.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/ExportDonationHistory
        [HttpGet]
        public async Task<IActionResult> ExportDonationHistory()
        {
            try
            {
                var records = (await _donationRecordRepository.GetAllAsync()).ToList();
                foreach (var r in records)
                {
                    if (r.Donor == null)
                    {
                        var donorDto = await _donorService.GetDonorProfileAsync(r.DonorId);
                        if (donorDto != null)
                        {
                            r.Donor = _mapper.Map<Donor>(donorDto);
                        }
                    }
                }
                var builder = new StringBuilder();
                builder.AppendLine("DonorName,BloodType,DonationDate,Hospital,Units,Status");
                foreach (var r in records)
                {
                    builder.AppendLine($"{EscapeCsvField(r.Donor?.Name)},{EscapeCsvField(r.BloodType)},{r.DonationDate:yyyy-MM-dd},{EscapeCsvField(r.Hospital)},{r.Units},{EscapeCsvField(r.Status)}");
                }
                return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "donation_history_export.csv");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error exporting donation history.");
                TempData["ErrorMessage"] = "A database error occurred while exporting donation history.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/AuditLog
        [HttpGet]
        public async Task<IActionResult> AuditLog()
        {
            try
            {
                var logs = await _donorService.GetAuditLogsAsync(100);
                return View(logs);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error retrieving audit logs.");
                TempData["ErrorMessage"] = "A database error occurred while retrieving audit logs.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/Inventory
        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            try
            {
                var inventoryDto = await _bloodInventoryService.GetAllInventoryAsync();
                var inventory = _mapper.Map<List<BloodInventory>>(inventoryDto);
                return View(inventory);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading blood inventory.");
                TempData["ErrorMessage"] = "A database error occurred while loading blood inventory.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Admin/UpdateInventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventory(int id, int units, string? returnUrl)
        {
            if (units < 0)
            {
                TempData["ErrorMessage"] = "Units available cannot be negative.";
                return Redirect(returnUrl ?? Url.Action(nameof(Inventory)) ?? "/Admin/Inventory");
            }

            try
            {
                var result = await _bloodInventoryService.UpdateInventoryStockAsync(id, units);
                if (!result.Success) return NotFound();

                // Log action in audit log
                AuditService.LogAction(null, HttpContext, "UpdateInventory", "BloodInventory", id, 
                    $"Updated {result.BloodType} stock from {result.OldUnits} to {units} units.");

                // Determine stock status and styling parameters to broadcast
                var statusLabel = "Good Standing";
                var badgeClass = "bg-success text-white";
                var textClass = "text-success";
                var bgLight = "#F4FAF6";
                if (units < 5)
                {
                    statusLabel = "Critical Stock";
                    badgeClass = "bg-danger text-white";
                    textClass = "text-danger";
                    bgLight = "#FFF5F5";
                }
                else if (units <= 15)
                {
                    statusLabel = "Low Stock";
                    badgeClass = "bg-warning text-dark";
                    textClass = "text-warning-dark";
                    bgLight = "#FFFDF0";
                }

                // Broadcast live inventory update via SignalR to all connected users
                await _hubContext.Clients.All.SendAsync("ReceiveInventoryUpdate", new
                {
                    id = id,
                    bloodType = result.BloodType,
                    unitsAvailable = units,
                    unitsReserved = result.UnitsReserved,
                    statusLabel = statusLabel,
                    badgeClass = badgeClass,
                    textClass = textClass,
                    bgLight = bgLight,
                    lastUpdated = result.LastUpdated.ToString("MMMM dd, yyyy HH:mm:ss")
                });

                TempData["SuccessMessage"] = $"Successfully updated {result.BloodType} stock to {units} units.";

                return Redirect(returnUrl ?? Url.Action(nameof(Inventory)) ?? "/Admin/Inventory");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error updating stock for inventory ID {InventoryId} to {Units} units.", id, units);
                TempData["ErrorMessage"] = "A database error occurred while updating the inventory stock.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Admin/VerifyDonor/{token}
        [HttpGet("Admin/VerifyDonor/{token}")]
        public async Task<IActionResult> VerifyDonor(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Verification token is missing.";
                return RedirectToAction("Index");
            }

            try
            {
                var donor = await _donorService.GetDonorByVerificationTokenAsync(token);
                var adminEmail = HttpContext.Session.GetString("UserEmail") ?? "unknown-admin";
                
                // Gather location/IP details
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();
                var location = $"IP: {ipAddress} | Browser: {userAgent}";
                if (location.Length > 200)
                {
                    location = location.Substring(0, 197) + "...";
                }

                if (donor != null)
                {
                    await _donorService.LogQrScanAsync(donor.Id, adminEmail, donor.IsVerified, location);

                    AuditService.LogAction(null, HttpContext, "VerifyDonor", "Donor", donor.Id, 
                        $"Scanned donor QR code for {donor.Name}. Status: {(donor.IsVerified ? "Verified" : "Pending Verification")}.");

                    return View(donor);
                }
                else
                {
                    await _donorService.LogQrScanAsync(null, adminEmail, false, location);

                    AuditService.LogAction(null, HttpContext, "VerifyDonor", "Donor", null, 
                        $"Scanned invalid donor token: {token}.");

                    return View((Donor?)null);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error during QR code donor verification. Token: {Token}", token);
                TempData["ErrorMessage"] = "A database error occurred during verification.";
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/VerifyUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyUser(int id)
        {
            try
            {
                var donorDto = await _donorService.GetDonorProfileAsync(id);
                if (donorDto == null) return NotFound();

                var success = await _donorService.VerifyDonorProfileAsync(id);
                if (!success) return BadRequest("Could not verify donor profile.");

                AuditService.LogAction(null, HttpContext, "VerifyUser", "Donor", id, 
                    $"Verified donor profile for {donorDto.Name} ({donorDto.Email}).");

                TempData["SuccessMessage"] = $"Donor profile for {donorDto.Name} has been successfully verified.";
                return RedirectToAction("VerifyDonor", new { token = donorDto.VerificationToken });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error verifying donor profile ID: {DonorId}", id);
                TempData["ErrorMessage"] = "A database error occurred while verifying the donor.";
                return RedirectToAction("Index");
            }
        }

        // GET: Admin/ScanHistory
        [HttpGet]
        public async Task<IActionResult> ScanHistory()
        {
            try
            {
                var logs = await _donorService.GetQrScanHistoryAsync();
                return View(logs);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading scan history.");
                TempData["ErrorMessage"] = "A database error occurred while loading scan history.";
                return RedirectToAction("Index");
            }
        }

        // GET: Admin/Appointments
        [HttpGet]
        public async Task<IActionResult> Appointments()
        {
            try
            {
                var appointmentsDto = await _appointmentService.GetAllAppointmentsAsync();
                var appointments = _mapper.Map<List<Appointment>>(appointmentsDto);
                return View(appointments);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading appointments for admin.");
                TempData["ErrorMessage"] = "A database error occurred while loading appointments.";
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/ApproveAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            try
            {
                var appointmentDto = (await _appointmentService.GetAllAppointmentsAsync()).FirstOrDefault(a => a.Id == id);
                if (appointmentDto == null) return NotFound();

                if (appointmentDto.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "Only pending appointments can be approved.";
                    return RedirectToAction(nameof(Appointments));
                }

                var success = await _appointmentService.ApproveAppointmentAsync(id);
                if (!success) return BadRequest("Could not approve appointment.");

                var donorDto = await _donorService.GetDonorProfileAsync(appointmentDto.DonorId);

                AuditService.LogAction(null, HttpContext, "ApproveAppointment", "Appointment", id, 
                    $"Approved appointment for {donorDto?.Name} on {appointmentDto.AppointmentDate:yyyy-MM-dd} {appointmentDto.TimeSlot}.");

                TempData["SuccessMessage"] = $"Appointment for {donorDto?.Name} approved.";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error approving appointment. ID: {AppointmentId}", id);
                TempData["ErrorMessage"] = "A database error occurred while approving the appointment.";
                return RedirectToAction(nameof(Appointments));
            }
        }

        // POST: Admin/RejectAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAppointment(int id)
        {
            try
            {
                var appointmentDto = (await _appointmentService.GetAllAppointmentsAsync()).FirstOrDefault(a => a.Id == id);
                if (appointmentDto == null) return NotFound();

                if (appointmentDto.Status == "Completed" || appointmentDto.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = "This appointment cannot be cancelled/rejected.";
                    return RedirectToAction(nameof(Appointments));
                }

                var success = await _appointmentService.RejectAppointmentAsync(id);
                if (!success) return BadRequest("Could not reject appointment.");

                var donorDto = await _donorService.GetDonorProfileAsync(appointmentDto.DonorId);

                AuditService.LogAction(null, HttpContext, "RejectAppointment", "Appointment", id, 
                    $"Cancelled/Rejected appointment for {donorDto?.Name} on {appointmentDto.AppointmentDate:yyyy-MM-dd} {appointmentDto.TimeSlot}.");

                TempData["SuccessMessage"] = $"Appointment for {donorDto?.Name} has been cancelled/rejected.";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error rejecting appointment. ID: {AppointmentId}", id);
                TempData["ErrorMessage"] = "A database error occurred while rejecting the appointment.";
                return RedirectToAction(nameof(Appointments));
            }
        }

        // POST: Admin/CompleteAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            try
            {
                var appointmentDto = (await _appointmentService.GetAllAppointmentsAsync()).FirstOrDefault(a => a.Id == id);
                if (appointmentDto == null) return NotFound();

                if (appointmentDto.Status != "Approved")
                {
                    TempData["ErrorMessage"] = "Only approved appointments can be marked as completed.";
                    return RedirectToAction(nameof(Appointments));
                }

                var success = await _appointmentService.CompleteAppointmentAsync(id);
                if (!success) return BadRequest("Could not complete appointment.");

                var donorDto = await _donorService.GetDonorProfileAsync(appointmentDto.DonorId);

                AuditService.LogAction(null, HttpContext, "CompleteAppointment", "Appointment", id, 
                    $"Marked appointment for {donorDto?.Name} on {appointmentDto.AppointmentDate:yyyy-MM-dd} as Completed and created donation record.");

                TempData["SuccessMessage"] = $"Appointment for {donorDto?.Name} marked as Completed. Profile history and badge counts updated!";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error completing appointment. ID: {AppointmentId}", id);
                TempData["ErrorMessage"] = "A database error occurred while completing the appointment.";
                return RedirectToAction(nameof(Appointments));
            }
        }
    }

    public class AdminDashboardViewModel
    {
        public List<Donor> Donors { get; set; } = new();
        public List<BloodRequest> Requests { get; set; } = new();
        public List<ActiveRequestRecommendationViewModel> ActiveRecommendations { get; set; } = new();
    }

    public class ActiveRequestRecommendationViewModel
    {
        public BloodRequest Request { get; set; } = null!;
        public RecommendedDonorDto? TopDonor { get; set; }
    }

    public class MatchDonorsViewModel
    {
        public BloodRequest BloodRequest { get; set; } = null!;
        public List<RecommendedDonorDto> RankedDonors { get; set; } = new();
        public string InferredState { get; set; } = string.Empty;
    }
}
