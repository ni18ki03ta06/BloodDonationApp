using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Models;
using BloodDonationApp.Services;
using BloodDonationApp.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace BloodDonationApp.Controllers.Api.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly IDonorRepository _donorRepository;
        private readonly IRepository<Admin> _adminRepository;
        private readonly IJwtService _jwtService;

        public ApiAuthController(
            IDonorRepository donorRepository,
            IRepository<Admin> adminRepository,
            IJwtService jwtService)
        {
            _donorRepository = donorRepository;
            _adminRepository = adminRepository;
            _jwtService = jwtService;
        }

        // POST: api/v1/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hashedPassword = PasswordHasher.HashPassword(dto.Password);

            // 1. Check Admins
            var admins = await _adminRepository.FindAsync(a => a.Email.ToLower() == dto.Email.ToLower());
            var admin = admins.FirstOrDefault();
            if (admin != null && admin.Password == hashedPassword)
            {
                var accessToken = _jwtService.GenerateAccessToken(admin.Id.ToString(), admin.Email, admin.Role);
                var refreshToken = _jwtService.GenerateRefreshToken();

                admin.RefreshToken = refreshToken;
                admin.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                _adminRepository.Update(admin);
                await _adminRepository.SaveChangesAsync();

                return Ok(new TokenResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Expiry = DateTime.UtcNow.AddMinutes(60)
                });
            }

            // 2. Check Donors
            var donor = await _donorRepository.GetDonorByEmailAsync(dto.Email);
            if (donor != null && donor.Password == hashedPassword)
            {
                var accessToken = _jwtService.GenerateAccessToken(donor.Id.ToString(), donor.Email, "Donor");
                var refreshToken = _jwtService.GenerateRefreshToken();

                donor.RefreshToken = refreshToken;
                donor.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                _donorRepository.Update(donor);
                await _donorRepository.SaveChangesAsync();

                return Ok(new TokenResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Expiry = DateTime.UtcNow.AddMinutes(60)
                });
            }

            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = "Invalid email or password.",
                Instance = HttpContext.Request.Path
            });
        }

        // POST: api/v1/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ClaimsPrincipal? principal;
            try
            {
                principal = _jwtService.GetPrincipalFromExpiredToken(dto.AccessToken);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Refresh token validation failed for access token: {AccessToken}", dto.AccessToken);
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = "Invalid access token.",
                    Instance = HttpContext.Request.Path
                });
            }

            if (principal == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = "Invalid access token claims.",
                    Instance = HttpContext.Request.Path
                });
            }

            foreach (var claim in principal.Claims)
            {
                Serilog.Log.Information("Refresh token claim - Type: {Type}, Value: {Value}", claim.Type, claim.Value);
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? principal.FindFirst("role")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                Serilog.Log.Warning("Missing claims: userId={UserId}, role={Role}", userId, role);
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = "Invalid token claims.",
                    Instance = HttpContext.Request.Path
                });
            }

            if (role == "Donor")
            {
                var donor = await _donorRepository.GetByIdAsync(int.Parse(userId));
                if (donor == null || donor.RefreshToken != dto.RefreshToken || donor.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Status = 400,
                        Title = "Bad Request",
                        Detail = "Invalid refresh token or token expired.",
                        Instance = HttpContext.Request.Path
                    });
                }

                var newAccessToken = _jwtService.GenerateAccessToken(donor.Id.ToString(), donor.Email, "Donor");
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                donor.RefreshToken = newRefreshToken;
                donor.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                _donorRepository.Update(donor);
                await _donorRepository.SaveChangesAsync();

                return Ok(new TokenResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Expiry = DateTime.UtcNow.AddMinutes(60)
                });
            }
            else
            {
                var admin = await _adminRepository.GetByIdAsync(int.Parse(userId));
                if (admin == null || admin.RefreshToken != dto.RefreshToken || admin.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Status = 400,
                        Title = "Bad Request",
                        Detail = "Invalid refresh token or token expired.",
                        Instance = HttpContext.Request.Path
                    });
                }

                var newAccessToken = _jwtService.GenerateAccessToken(admin.Id.ToString(), admin.Email, admin.Role);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                admin.RefreshToken = newRefreshToken;
                admin.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                _adminRepository.Update(admin);
                await _adminRepository.SaveChangesAsync();

                return Ok(new TokenResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Expiry = DateTime.UtcNow.AddMinutes(60)
                });
            }
        }

        // POST: api/v1/auth/revoke
        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = "User identifier or role not found in claims.",
                    Instance = HttpContext.Request.Path
                });
            }

            if (role == "Donor")
            {
                var donor = await _donorRepository.GetByIdAsync(int.Parse(userId));
                if (donor != null)
                {
                    donor.RefreshToken = null;
                    donor.RefreshTokenExpiryTime = null;
                    _donorRepository.Update(donor);
                    await _donorRepository.SaveChangesAsync();
                }
            }
            else
            {
                var admin = await _adminRepository.GetByIdAsync(int.Parse(userId));
                if (admin != null)
                {
                    admin.RefreshToken = null;
                    admin.RefreshTokenExpiryTime = null;
                    _adminRepository.Update(admin);
                    await _adminRepository.SaveChangesAsync();
                }
            }

            return NoContent();
        }

        // GET: api/v1/auth/test-exception
        [HttpGet("test-exception")]
        public IActionResult TestException()
        {
            throw new InvalidOperationException("This is a deliberate test exception for RFC 7807 verification.");
        }
    }

    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class TokenRequestDto
    {
        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
    }
}
