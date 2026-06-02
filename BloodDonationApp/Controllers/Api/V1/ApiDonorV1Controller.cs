using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Models;
using AutoMapper;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BloodDonationApp.Controllers.Api.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/donors")]
    [Authorize]
    public class ApiDonorV1Controller : ControllerBase
    {
        private readonly IDonorService _donorService;
        private readonly IMapper _mapper;

        public ApiDonorV1Controller(IDonorService donorService, IMapper mapper)
        {
            _donorService = donorService;
            _mapper = mapper;
        }

        // GET: api/v1/donors?bloodType=&city=
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetDonors([FromQuery] string? bloodType, [FromQuery] string? city)
        {
            try
            {
                var dtos = await _donorService.SearchDonorsAsync(bloodType, city, "Any", null, null, false);
                var donors = dtos.Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.BloodType,
                    d.City,
                    d.TotalDonations
                }).ToList();

                return Ok(donors);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ApiDonorV1Controller.GetDonors");
                throw;
            }
        }

        // GET: api/v1/donors/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDonor(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (role == "Donor" && userId != id.ToString())
                {
                    return Forbid();
                }

                var donorDto = await _donorService.GetDonorProfileAsync(id);
                if (donorDto == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Status = 404,
                        Title = "Not Found",
                        Detail = $"Donor with ID {id} not found.",
                        Instance = HttpContext.Request.Path
                    });
                }

                var donor = new
                {
                    donorDto.Id,
                    donorDto.Name,
                    donorDto.BloodType,
                    donorDto.City,
                    donorDto.TotalDonations,
                    donorDto.Age,
                    donorDto.Gender,
                    donorDto.IsVerified,
                    donorDto.LastDonationDate,
                    donorDto.Phone,
                    donorDto.Email,
                    donorDto.Address,
                    donorDto.State,
                    donorDto.PinCode
                };

                return Ok(donor);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ApiDonorV1Controller.GetDonor with ID {Id}", id);
                throw;
            }
        }
    }
}
