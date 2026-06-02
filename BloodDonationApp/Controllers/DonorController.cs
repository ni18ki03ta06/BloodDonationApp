using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Data;
using BloodDonationApp.Models;
using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Filters;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using BloodDonationApp.Services;
using BloodDonationApp.Application.Interfaces;
using AutoMapper;

namespace BloodDonationApp.Controllers
{
    public class DonorController : Controller
    {
        private readonly IMemoryCache _cache;
        private readonly IGoogleMapsService _googleMapsService;
        private readonly IQrCodeService _qrCodeService;
        private readonly IDonorService _donorService;
        private readonly IAppointmentService _appointmentService;
        private readonly IMapper _mapper;

        public DonorController(
            IMemoryCache cache, 
            IGoogleMapsService googleMapsService, 
            IQrCodeService qrCodeService, 
            IDonorService donorService,
            IAppointmentService appointmentService,
            IMapper mapper)
        {
            _cache = cache;
            _googleMapsService = googleMapsService;
            _qrCodeService = qrCodeService;
            _donorService = donorService;
            _appointmentService = appointmentService;
            _mapper = mapper;
        }

        // GET: Donor/Register (Public)
        public IActionResult Register()
        {
            return View();
        }

        // POST: Donor/Register (Public)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Donor donor)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var cacheKey = $"RegCount_{ipAddress}";

                if (_cache.TryGetValue(cacheKey, out int count))
                {
                    if (count >= 3)
                    {
                        return StatusCode(429, "Too many registration attempts. Please try again after an hour.");
                    }
                }

                if (ModelState.IsValid)
                {
                    // Handle Profile Photo Upload if present
                    if (donor.ProfilePhoto != null && donor.ProfilePhoto.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(donor.ProfilePhoto.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await donor.ProfilePhoto.CopyToAsync(fileStream);
                        }
                        donor.ProfilePhotoPath = "/uploads/" + uniqueFileName;
                    }

                    var registered = await _donorService.RegisterDonorAsync(donor);
                    if (registered == null)
                    {
                        ModelState.AddModelError("Email", "A donor with this email address is already registered.");
                        return View(donor);
                    }

                    // Increment registration count in cache
                    int currentCount = _cache.TryGetValue(cacheKey, out int existingCount) ? existingCount : 0;
                    currentCount++;
                    _cache.Set(cacheKey, currentCount, TimeSpan.FromHours(1));
                    
                    // Redirect to a confirmation page on success (PRG pattern)
                    return RedirectToAction(nameof(RegistrationSuccess));
                }
                
                return View(donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error during donor registration for email: {Email}", donor?.Email);
                TempData["ErrorMessage"] = "A database error occurred during registration. Please try again later.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Donor/RegistrationSuccess (Public)
        public IActionResult RegistrationSuccess()
        {
            return View();
        }

        // GET: Donor/Dashboard (Secured)
        [AuthFilter]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var donorDto = await _donorService.GetDonorProfileAsync(userId);
                if (donorDto == null)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("UserLogin", "Account");
                }

                var donor = _mapper.Map<Donor>(donorDto);
                return View(donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading donor dashboard.");
                TempData["ErrorMessage"] = "A database error occurred while loading your dashboard.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Donor/Profile (Secured)
        [AuthFilter]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var donorDto = await _donorService.GetDonorProfileAsync(userId);
                if (donorDto == null) return NotFound();

                var donor = _mapper.Map<Donor>(donorDto);
                return View(donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading donor profile.");
                TempData["ErrorMessage"] = "A database error occurred while loading your profile.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Donor/DigitalCard (Secured)
        [AuthFilter]
        public async Task<IActionResult> DigitalCard()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var donorDto = await _donorService.GetDonorProfileAsync(userId);
                if (donorDto == null) return NotFound();

                var donor = _mapper.Map<Donor>(donorDto);

                // Generate secure URL for scanning
                var scheme = Request.Scheme;
                var host = Request.Host.Value;
                var verifyUrl = $"{scheme}://{host}/Admin/VerifyDonor/{donor.VerificationToken}";

                // Generate QR Code Base64
                var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(verifyUrl);

                ViewBag.QrCodeBase64 = qrCodeBase64;
                ViewBag.VerifyUrl = verifyUrl;

                return View(donor);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "A database error occurred while loading your digital card.";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Donor/MyCard (Secured)
        [AuthFilter]
        public async Task<IActionResult> MyCard()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var donorDto = await _donorService.GetDonorProfileAsync(userId);
                if (donorDto == null) return NotFound();

                var donor = _mapper.Map<Donor>(donorDto);

                // Generate secure URL for scanning
                var scheme = Request.Scheme;
                var host = Request.Host.Value;
                var verifyUrl = $"{scheme}://{host}/Admin/VerifyDonor/{donor.VerificationToken}";

                // Generate QR Code Base64
                var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(verifyUrl);

                ViewBag.QrCodeBase64 = qrCodeBase64;
                ViewBag.VerifyUrl = verifyUrl;

                return View(donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading donor MyCard.");
                TempData["ErrorMessage"] = "A database error occurred while loading your card.";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Donor/Leaderboard (Secured)
        [AuthFilter]
        public async Task<IActionResult> Leaderboard()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var leaderboard = await _donorService.GetLeaderboardAsync(20);
                ViewBag.CurrentDonorId = userId;

                return View(leaderboard);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading leaderboard.");
                TempData["ErrorMessage"] = "A database error occurred while loading the leaderboard.";
                return RedirectToAction("Dashboard");
            }
        }

        // POST: Donor/Profile (Secured)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter]
        public async Task<IActionResult> Profile(int id, [Bind("Id,Name,BloodType,Phone,Email,City,LastDonationDate,IsAvailable")] Donor donor)
        {
            try
            {
                if (id != donor.Id) return NotFound();
 
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
                ModelState.Remove("Age");
                ModelState.Remove("Gender");
                ModelState.Remove("Address");
                ModelState.Remove("State");
                ModelState.Remove("PinCode");
 
                if (ModelState.IsValid)
                {
                    var updatedDto = await _donorService.UpdateProfileAsync(id, donor);
                    if (updatedDto == null) return NotFound();

                    var existingDonor = _mapper.Map<Donor>(updatedDto);
                    
                    // Update session variables in case details changed
                    HttpContext.Session.SetString("UserName", existingDonor.Name);
                    HttpContext.Session.SetString("UserEmail", existingDonor.Email);
                    HttpContext.Session.SetString("UserBloodType", existingDonor.BloodType);
 
                    TempData["SuccessMessage"] = "Profile updated successfully.";
                    return RedirectToAction(nameof(Dashboard));
                }
 
                var donorDto = await _donorService.GetDonorProfileAsync(id);
                if (donorDto != null)
                {
                    donor.DonationRecords = _mapper.Map<Donor>(donorDto).DonationRecords;
                }
                return View(donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error updating donor profile for ID: {DonorId}", id);
                TempData["ErrorMessage"] = "A database error occurred while updating your profile.";
                return RedirectToAction("Error", "Home");
            }
        }
 
        // GET: Donor/Donate (Secured)
        [AuthFilter]
        public async Task<IActionResult> Donate()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }
 
                var donorDto = await _donorService.GetDonorProfileAsync(userId);
                if (donorDto == null) return NotFound();
 
                var donor = _mapper.Map<Donor>(donorDto);
                return View(donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading Donate view.");
                TempData["ErrorMessage"] = "A database error occurred while retrieving donor details.";
                return RedirectToAction("Error", "Home");
            }
        }
 
        // POST: Donor/Donate (Secured toggle)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter]
        public async Task<IActionResult> Donate(int id, bool isAvailable)
        {
            try
            {
                var success = await _donorService.ToggleAvailabilityAndSelfReportAsync(id, isAvailable);
                if (!success) return NotFound();
 
                TempData["SuccessMessage"] = isAvailable 
                    ? "You have marked yourself as AVAILABLE to donate blood." 
                    : "You have marked yourself as BUSY/UNAVAILABLE and a new donation record has been added to your history.";
 
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error updating availability/donation records for donor ID: {DonorId}", id);
                TempData["ErrorMessage"] = "A database error occurred while updating availability.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Donor/Certificate/{donationId} (Secured)
        [HttpGet("Donor/Certificate/{donationId}")]
        [AuthFilter]
        public async Task<IActionResult> Certificate(int donationId)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var record = await _donorService.GetDonationRecordAsync(donationId);

                if (record == null || record.DonorId != userId)
                {
                    TempData["ErrorMessage"] = "Access Denied: You are not authorized to view this certificate.";
                    return RedirectToAction(nameof(History));
                }

                ViewBag.CertificateNumber = $"BDA-{record.DonorId}-{record.Id}";
                return View(record);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading certificate for donation ID: {DonationId}", donationId);
                TempData["ErrorMessage"] = "A database error occurred while retrieving certificate details.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Donor/History (Secured)
        [AuthFilter]
        public async Task<IActionResult> History()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var records = await _donorService.GetDonationHistoryAsync(userId);
                return View(records);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading history for donor.");
                TempData["ErrorMessage"] = "A database error occurred while fetching your donation history.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Donor/Search
        [AuthFilter]
        public async Task<IActionResult> Search(string bloodType, string city, string state, int? minAge, int? maxAge, bool isVerified = false)
        {
            try
            {
                var donorDtos = await _donorService.SearchDonorsAsync(bloodType, city, state, minAge, maxAge, isVerified);
                var donors = _mapper.Map<List<Donor>>(donorDtos);

                ViewBag.BloodType = bloodType;
                ViewBag.City = city;
                ViewBag.State = state;
                ViewBag.MinAge = minAge;
                ViewBag.MaxAge = maxAge;
                ViewBag.IsVerified = isVerified;

                return View(donors);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error during donor search. bloodType: {BloodType}, city: {City}", bloodType, city);
                TempData["ErrorMessage"] = "A database error occurred while searching for donors.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Donor/Notifications
        [AuthFilter]
        public async Task<IActionResult> Notifications()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var notifications = await _donorService.GetNotificationsAsync(userId);
                return View(notifications);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error retrieving notifications for donor.");
                TempData["ErrorMessage"] = "A database error occurred while retrieving notifications.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Donor/MarkNotificationRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var success = await _donorService.MarkNotificationReadAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] = "Notification marked as read.";
                return RedirectToAction(nameof(Notifications));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error marking notification as read. NotificationID: {NotificationId}", id);
                TempData["ErrorMessage"] = "A database error occurred while updating the notification.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Donor/Appointments (Secured)
        [AuthFilter]
        public async Task<IActionResult> Appointments()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var appointmentsDto = await _appointmentService.GetDonorAppointmentsAsync(userId);
                var appointments = _mapper.Map<IEnumerable<Appointment>>(appointmentsDto);
                return View(appointments);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading donor appointments.");
                TempData["ErrorMessage"] = "A database error occurred while loading your appointments.";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Donor/BookAppointment (Secured)
        [AuthFilter]
        public async Task<IActionResult> BookAppointment(DateTime? date)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                // Default date to tomorrow
                var targetDate = date ?? DateTime.Today.AddDays(1);
                if (targetDate < DateTime.Today)
                {
                    targetDate = DateTime.Today.AddDays(1);
                }

                // Query booked time slots for target date
                var bookedSlots = await _appointmentService.GetBookedSlotsForDateAsync(targetDate);

                // Define all time slots
                var allSlots = new List<string>
                {
                    "09:00 AM - 10:00 AM",
                    "10:00 AM - 11:00 AM",
                    "11:00 AM - 12:00 PM",
                    "12:00 PM - 01:00 PM",
                    "01:00 PM - 02:00 PM",
                    "02:00 PM - 03:00 PM",
                    "03:00 PM - 04:00 PM",
                    "04:00 PM - 05:00 PM"
                };

                ViewBag.TargetDate = targetDate;
                ViewBag.BookedSlots = bookedSlots;
                ViewBag.AllSlots = allSlots;

                return View();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error initializing appointment booking.");
                TempData["ErrorMessage"] = "A database error occurred while initializing appointment booking.";
                return RedirectToAction("Appointments");
            }
        }

        // POST: Donor/BookAppointment (Secured)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter]
        public async Task<IActionResult> BookAppointment(DateTime appointmentDate, string timeSlot, string? notes)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var appointmentDto = await _appointmentService.BookAppointmentAsync(userId, appointmentDate, timeSlot, notes, (key, error) => ModelState.AddModelError(key, error));

                if (ModelState.IsValid && appointmentDto != null)
                {
                    TempData["SuccessMessage"] = "Appointment successfully requested! Pending administrator review.";
                    return RedirectToAction(nameof(Appointments));
                }

                // Re-populate view bag if validation failed
                var targetDate = appointmentDate.Date;
                var bookedSlots = await _appointmentService.GetBookedSlotsForDateAsync(targetDate);

                var allSlots = new List<string>
                {
                    "09:00 AM - 10:00 AM",
                    "10:00 AM - 11:00 AM",
                    "11:00 AM - 12:00 PM",
                    "12:00 PM - 01:00 PM",
                    "01:00 PM - 02:00 PM",
                    "02:00 PM - 03:00 PM",
                    "03:00 PM - 04:00 PM",
                    "04:00 PM - 05:00 PM"
                };

                ViewBag.TargetDate = targetDate;
                ViewBag.BookedSlots = bookedSlots;
                ViewBag.AllSlots = allSlots;

                return View();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error during appointment booking.");
                TempData["ErrorMessage"] = "A database error occurred while booking the appointment.";
                return RedirectToAction("Appointments");
            }
        }

        // POST: Donor/CancelAppointment (Secured)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var success = await _appointmentService.CancelAppointmentAsync(id, userId);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Appointment cannot be cancelled or was not found.";
                    return RedirectToAction(nameof(Appointments));
                }

                TempData["SuccessMessage"] = "Appointment cancelled successfully.";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error cancelling appointment. ID: {AppointmentId}", id);
                TempData["ErrorMessage"] = "A database error occurred while cancelling the appointment.";
                return RedirectToAction(nameof(Appointments));
            }
        }

        // GET: Donor/Gamification (Secured)
        [AuthFilter]
        public async Task<IActionResult> Gamification()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var gamificationData = await _donorService.GetGamificationDataAsync(userId);
                if (gamificationData == null) return NotFound();

                ViewBag.AllBadges = gamificationData.Value.AllBadges;
                ViewBag.AllRewards = gamificationData.Value.AllRewards;
                ViewBag.Leaderboard = gamificationData.Value.Leaderboard;

                return View(gamificationData.Value.Donor);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading gamification dashboard.");
                TempData["ErrorMessage"] = "A database error occurred while loading your achievements dashboard.";
                return RedirectToAction("Dashboard");
            }
        }

        // POST: Donor/RedeemReward (Secured)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter]
        public async Task<IActionResult> RedeemReward(int rewardId)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var result = await _donorService.RedeemRewardAsync(userId, rewardId);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Gamification));
                }

                TempData["SuccessMessage"] = result.SuccessMsg;
                return RedirectToAction(nameof(Gamification));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error redeeming reward.");
                TempData["ErrorMessage"] = "A database error occurred while processing your reward redemption.";
                return RedirectToAction(nameof(Gamification));
            }
        }
    }
}
