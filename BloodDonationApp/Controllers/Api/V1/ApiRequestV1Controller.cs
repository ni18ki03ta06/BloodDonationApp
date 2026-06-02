using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System;
using Microsoft.AspNetCore.Authorization;

namespace BloodDonationApp.Controllers.Api.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/requests")]
    [Authorize]
    public class ApiRequestV1Controller : ControllerBase
    {
        private readonly IBloodRequestService _bloodRequestService;

        public ApiRequestV1Controller(IBloodRequestService bloodRequestService)
        {
            _bloodRequestService = bloodRequestService;
        }

        // GET: api/v1/requests?status=Pending
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
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
                Log.Error(ex, "Error occurred in ApiRequestV1Controller.GetRequests");
                throw;
            }
        }

        // POST: api/v1/requests
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestV1Dto dto)
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
                    UrgencyLevel = string.IsNullOrWhiteSpace(dto.UrgencyLevel) ? "Normal" : dto.UrgencyLevel.Trim(),
                    RequesterName = dto.PatientName.Trim(),
                    IsAnonymous = false
                };

                var created = await _bloodRequestService.CreateRequestAsync(reqDto);
                return StatusCode(201, new { id = created.Id });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ApiRequestV1Controller.CreateRequest");
                throw;
            }
        }
    }

    public class CreateRequestV1Dto
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

        public string? UrgencyLevel { get; set; }
    }
}
