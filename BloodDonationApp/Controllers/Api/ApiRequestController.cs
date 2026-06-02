using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System;

namespace BloodDonationApp.Controllers.Api
{
    [ApiController]
    [Route("api/requests")]
    public class ApiRequestController : ControllerBase
    {
        private readonly IBloodRequestService _bloodRequestService;

        public ApiRequestController(IBloodRequestService bloodRequestService)
        {
            _bloodRequestService = bloodRequestService;
        }

        // GET: api/requests?status=Pending
        [HttpGet]
        public async Task<IActionResult> GetRequests([FromQuery] string? status)
        {
            try
            {
                var dtos = await _bloodRequestService.GetRequestsFilteredAsync(status, null);
                var requests = dtos.Select(r => new
                {
                    r.Id,
                    r.PatientName,
                    r.BloodType,
                    r.Units,
                    r.Hospital,
                    r.City,
                    r.RequiredDate,
                    r.Status,
                    r.UrgencyLevel,
                    RequesterName = r.IsAnonymous ? "Anonymous" : r.RequesterName,
                    r.IsAnonymous,
                    r.CreatedAt
                }).ToList();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ApiRequestController.GetRequests");
                return StatusCode(500, new { error = "An error occurred while fetching blood requests." });
            }
        }

        // POST: api/requests
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (dto.Units <= 0)
                {
                    ModelState.AddModelError("Units", "Units requested must be greater than zero.");
                    return BadRequest(ModelState);
                }

                var reqDto = new BloodRequestDto
                {
                    PatientName = dto.PatientName.Trim(),
                    BloodType = dto.BloodType.Trim(),
                    Units = dto.Units,
                    Hospital = dto.Hospital.Trim(),
                    City = dto.City.Trim(),
                    ContactNumber = dto.ContactNumber.Trim(),
                    RequiredDate = dto.RequiredDate,
                    Status = "Pending",
                    UrgencyLevel = "Normal",
                    RequesterName = dto.PatientName.Trim(),
                    IsAnonymous = false
                };

                var created = await _bloodRequestService.CreateRequestAsync(reqDto);
                return StatusCode(201, new { id = created.Id });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ApiRequestController.CreateRequest");
                return StatusCode(500, new { error = "An error occurred while creating the blood request." });
            }
        }
    }

    public class CreateRequestDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string PatientName { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(A\\+|A-|B\\+|B-|AB\\+|AB-|O\\+|O-)$", ErrorMessage = "Invalid blood type")]
        public string BloodType { get; set; } = string.Empty;

        [Required]
        [Range(1, 100, ErrorMessage = "Units requested must be between 1 and 100")]
        public int Units { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Hospital { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string City { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime RequiredDate { get; set; }
    }
}
