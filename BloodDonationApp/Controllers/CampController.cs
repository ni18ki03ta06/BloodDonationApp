using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Models;
using BloodDonationApp.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BloodDonationApp.Controllers
{
    public class CampController : Controller
    {
        private readonly IRepository<BloodCamp> _campRepository;
        private readonly IRepository<CampRegistration> _registrationRepository;

        public CampController(IRepository<BloodCamp> campRepository, IRepository<CampRegistration> registrationRepository)
        {
            _campRepository = campRepository;
            _registrationRepository = registrationRepository;
        }

        // GET: Camp/Index (List all active upcoming camps)
        public async Task<IActionResult> Index()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                int? loggedInDonorId = null;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    loggedInDonorId = userId;
                }

                var upcomingCamps = (await _campRepository.FindAsync(c => c.IsActive && c.ScheduledDate >= DateTime.Today))
                    .OrderBy(c => c.ScheduledDate)
                    .ToList();

                if (loggedInDonorId.HasValue)
                {
                    var registrations = await _registrationRepository.FindAsync(r => r.DonorId == loggedInDonorId.Value && r.Status == "Registered");
                    var registeredCampIds = registrations.Select(r => r.CampId).ToList();
                    ViewBag.RegisteredCampIds = registeredCampIds;
                }
                else
                {
                    ViewBag.RegisteredCampIds = new List<int>();
                }

                return View(upcomingCamps);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading upcoming blood camps.");
                TempData["ErrorMessage"] = "A database error occurred while fetching upcoming camps.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Camp/Register (Secured registration)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter]
        public async Task<IActionResult> Register(int campId)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var camp = await _campRepository.GetByIdAsync(campId);
                if (camp == null || !camp.IsActive || camp.ScheduledDate < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "Blood camp is not available for registration.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if already registered
                var registrations = await _registrationRepository.FindAsync(r => r.CampId == campId && r.DonorId == userId);
                var existingReg = registrations.FirstOrDefault();

                if (existingReg != null && existingReg.Status == "Registered")
                {
                    TempData["ErrorMessage"] = "You are already registered for this blood camp.";
                    return RedirectToAction(nameof(Index));
                }

                // Check capacity
                if (camp.RegisteredCount >= camp.MaxDonors)
                {
                    TempData["ErrorMessage"] = "This blood camp is already full.";
                    return RedirectToAction(nameof(Index));
                }

                if (existingReg != null)
                {
                    existingReg.Status = "Registered";
                    existingReg.RegisteredAt = DateTime.Now;
                    _registrationRepository.Update(existingReg);
                }
                else
                {
                    var reg = new CampRegistration
                    {
                        CampId = campId,
                        DonorId = userId,
                        RegisteredAt = DateTime.Now,
                        Status = "Registered"
                    };
                    await _registrationRepository.AddAsync(reg);
                }

                camp.RegisteredCount += 1;
                _campRepository.Update(camp);
                await _campRepository.SaveChangesAsync();
                await _registrationRepository.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully registered for {camp.CampName}!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error during camp registration for camp ID {CampId} and donor ID {UserId}", campId, HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "A database error occurred while registering for the blood camp.";
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Camp/Cancel (Secured cancellation)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter]
        public async Task<IActionResult> Cancel(int campId)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                var registrations = await _registrationRepository.FindAsync(r => r.CampId == campId && r.DonorId == userId && r.Status == "Registered");
                var reg = registrations.FirstOrDefault();

                if (reg == null)
                {
                    TempData["ErrorMessage"] = "No active registration found to cancel.";
                    return RedirectToAction(nameof(MyRegistrations));
                }

                reg.Status = "Cancelled";
                _registrationRepository.Update(reg);

                var camp = await _campRepository.GetByIdAsync(campId);
                if (camp != null)
                {
                    camp.RegisteredCount = Math.Max(0, camp.RegisteredCount - 1);
                    _campRepository.Update(camp);
                }

                await _registrationRepository.SaveChangesAsync();
                await _campRepository.SaveChangesAsync();

                TempData["SuccessMessage"] = "Successfully cancelled your registration.";
                return RedirectToAction(nameof(MyRegistrations));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error cancelling camp registration for camp ID {CampId} and donor ID {UserId}", campId, HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "A database error occurred while cancelling your registration.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Camp/MyRegistrations (Secured registration list)
        [AuthFilter]
        public async Task<IActionResult> MyRegistrations()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("UserLogin", "Account");
                }

                // Custom find with eager loading isn't strictly defined on IRepository,
                // but since CampRegistration has small dataset, we can get registrations
                // and then load Camps or we can fetch them since the DbContext tracker will match them.
                // Alternatively, we can load all registrations for user.
                var registrations = (await _registrationRepository.FindAsync(r => r.DonorId == userId)).ToList();
                
                // Let's resolve the Camp for each registration to avoid null Camp reference in view.
                foreach (var r in registrations)
                {
                    if (r.Camp == null)
                    {
                        r.Camp = await _campRepository.GetByIdAsync(r.CampId);
                    }
                }

                var sortedRegs = registrations
                    .Where(r => r.Camp != null)
                    .OrderBy(r => r.Camp!.ScheduledDate)
                    .ToList();

                return View(sortedRegs);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading registrations for donor ID {UserId}", HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "A database error occurred while fetching your registrations.";
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
