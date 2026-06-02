using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Models;
using BloodDonationApp.Core.Interfaces;
using System.Diagnostics;
using BloodDonationApp.Filters;
using System;
using System.Threading.Tasks;

namespace BloodDonationApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDonorRepository _donorRepository;
        private readonly IRepository<Feedback> _feedbackRepository;

        public HomeController(IDonorRepository donorRepository, IRepository<Feedback> feedbackRepository)
        {
            _donorRepository = donorRepository;
            _feedbackRepository = feedbackRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            var feedback = new Feedback();
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                var donor = await _donorRepository.GetByIdAsync(userId);
                if (donor != null)
                {
                    feedback.DonorId = donor.Id;
                    feedback.Name = donor.Name;
                    feedback.Email = donor.Email;
                }
            }
            return View(feedback);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(Feedback feedback)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                feedback.DonorId = userId;
            }

            feedback.SubmittedAt = DateTime.Now;
            feedback.IsRead = false;

            ModelState.Remove("Donor");

            if (ModelState.IsValid)
            {
                try
                {
                    await _feedbackRepository.AddAsync(feedback);
                    await _feedbackRepository.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thank you for your feedback! We will get back to you soon.";
                    return RedirectToAction(nameof(Contact));
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error occurred while saving feedback to database.");
                    TempData["ErrorMessage"] = "An error occurred while saving your feedback. Please try again later.";
                    return RedirectToAction("Error", "Home");
                }
            }

            TempData["ErrorMessage"] = "Please correct the errors in the form and try again.";
            return View("Contact", feedback);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode == 404)
            {
                return View("NotFound");
            }
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public new IActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }
    }
}
