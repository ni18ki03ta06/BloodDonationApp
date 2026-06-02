using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Models;
using BloodDonationApp.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BloodDonationApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDonorRepository _donorRepository;
        private readonly IRepository<Admin> _adminRepository;
        private readonly IRepository<PasswordResetToken> _tokenRepository;

        public AccountController(
            IDonorRepository donorRepository,
            IRepository<Admin> adminRepository,
            IRepository<PasswordResetToken> tokenRepository)
        {
            _donorRepository = donorRepository;
            _adminRepository = adminRepository;
            _tokenRepository = tokenRepository;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Redirect default login to the landing page role selection
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "User") return RedirectToAction("Dashboard", "Donor");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and password are required.");
                return View();
            }

            try
            {
                string hashedPassword = PasswordHasher.HashPassword(password);
                var admins = await _adminRepository.FindAsync(a => a.Email.ToLower() == email.ToLower() && a.Password == hashedPassword);
                var admin = admins.FirstOrDefault();

                if (admin != null)
                {
                    HttpContext.Session.SetString("UserRole", "Admin");
                    HttpContext.Session.SetString("AdminRole", admin.Role);
                    HttpContext.Session.SetString("AdminId", admin.Id.ToString());
                    HttpContext.Session.SetString("AdminName", admin.Name);
                    HttpContext.Session.SetString("UserName", admin.Name);
                    HttpContext.Session.SetString("UserEmail", admin.Email);
                    HttpContext.Session.SetString("LastActivityAt", DateTime.UtcNow.ToString("o"));

                    admin.LastLogin = DateTime.Now;
                    _adminRepository.Update(admin);
                    await _adminRepository.SaveChangesAsync();

                    return RedirectToAction("Index", "Admin");
                }

                ModelState.AddModelError("", "Invalid Admin credentials.");
                return View();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error during admin login attempt for {Email}", email);
                TempData["ErrorMessage"] = "A database error occurred during login. Please try again later.";
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public IActionResult UserLogin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "User") return RedirectToAction("Dashboard", "Donor");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserLogin(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and password are required.");
                return View();
            }

            try
            {
                // Query donor by Email and hashed Password
                string hashedPassword = PasswordHasher.HashPassword(password);
                var donor = await _donorRepository.GetDonorByEmailAsync(email);
                if (donor != null && donor.Password == hashedPassword)
                {
                    HttpContext.Session.SetString("UserRole", "User");
                    HttpContext.Session.SetString("UserName", donor.Name);
                    HttpContext.Session.SetString("UserEmail", donor.Email);
                    HttpContext.Session.SetString("UserId", donor.Id.ToString());
                    HttpContext.Session.SetString("UserBloodType", donor.BloodType);
                    HttpContext.Session.SetString("LastActivityAt", DateTime.UtcNow.ToString("o"));
                    return RedirectToAction("Dashboard", "Donor");
                }

                ModelState.AddModelError("", "Invalid credentials. Please enter your registered Email and Password.");
                return View();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error during user login attempt for {Email}", email);
                TempData["ErrorMessage"] = "A database error occurred during login. Please try again later.";
                return RedirectToAction("Error", "Home");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear all session data
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "User") return RedirectToAction("Dashboard", "Donor");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View();
            }

            try
            {
                var donor = await _donorRepository.GetDonorByEmailAsync(email);
                if (donor == null)
                {
                    ModelState.AddModelError("", "This email is not registered in our database.");
                    return View();
                }

                // Generate reset token
                var token = Guid.NewGuid().ToString();
                var resetToken = new PasswordResetToken
                {
                    DonorId = donor.Id,
                    Token = token,
                    ExpiresAt = DateTime.Now.AddHours(1),
                    IsUsed = false
                };

                await _tokenRepository.AddAsync(resetToken);
                await _tokenRepository.SaveChangesAsync();

                // Simulate email link
                var resetLink = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);
                TempData["ResetLink"] = resetLink;
                TempData["SuccessMessage"] = "A password reset link has been generated. Please use the simulator box below.";

                return View();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error during forgot password for {Email}", email);
                TempData["ErrorMessage"] = "A database error occurred while processing your request. Please try again later.";
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Reset token is missing.";
                return RedirectToAction("UserLogin");
            }

            try
            {
                var tokens = await _tokenRepository.FindAsync(t => t.Token == token);
                var resetToken = tokens.FirstOrDefault();

                if (resetToken == null)
                {
                    TempData["ErrorMessage"] = "Invalid password reset token.";
                    return RedirectToAction("UserLogin");
                }

                if (resetToken.IsUsed)
                {
                    TempData["ErrorMessage"] = "This reset token has already been used.";
                    return RedirectToAction("UserLogin");
                }

                if (resetToken.ExpiresAt < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "This reset token has expired.";
                    return RedirectToAction("UserLogin");
                }

                ViewBag.Token = token;
                return View();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error looking up reset token {Token}", token);
                TempData["ErrorMessage"] = "A database error occurred. Please try again later.";
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Token is missing.";
                return RedirectToAction("UserLogin");
            }

            ViewBag.Token = token;

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                ModelState.AddModelError("", "Password must be at least 8 characters long.");
                return View();
            }

            // Password complexity check (must have uppercase, lowercase, number, and special character)
            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));
            if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
            {
                ModelState.AddModelError("", "Password must have uppercase, lowercase, number, and special character.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View();
            }

            try
            {
                var tokens = await _tokenRepository.FindAsync(t => t.Token == token);
                var resetToken = tokens.FirstOrDefault();

                if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "The reset token is invalid, used, or expired.";
                    return RedirectToAction("UserLogin");
                }

                var donor = await _donorRepository.GetByIdAsync(resetToken.DonorId);
                if (donor == null)
                {
                    TempData["ErrorMessage"] = "Donor account associated with this token could not be found.";
                    return RedirectToAction("UserLogin");
                }

                // Hash new password and save
                string hashedPassword = PasswordHasher.HashPassword(password);
                donor.Password = hashedPassword;
                donor.UpdatedAt = DateTime.Now;
                resetToken.IsUsed = true;

                _donorRepository.Update(donor);
                _tokenRepository.Update(resetToken);

                await _donorRepository.SaveChangesAsync();
                await _tokenRepository.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your password has been successfully reset! You can now log in.";
                return RedirectToAction("UserLogin");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error resetting password with token {Token}", token);
                TempData["ErrorMessage"] = "A database error occurred while resetting your password. Please try again later.";
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
