using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Models;
using AutoMapper;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using System;

namespace BloodDonationApp.Controllers.Api
{
    [ApiController]
    [Route("api/donors")]
    public class ApiDonorController : ControllerBase
    {
        private readonly IDonorService _donorService;
        private readonly IMapper _mapper;

        public ApiDonorController(IDonorService donorService, IMapper mapper)
        {
            _donorService = donorService;
            _mapper = mapper;
        }

        // GET: api/donors?bloodType=&city=
        [HttpGet]
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
                Log.Error(ex, "Error occurred in ApiDonorController.GetDonors");
                return StatusCode(500, new { error = "An error occurred while fetching donors." });
            }
        }

        // GET: api/donors/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDonor(int id)
        {
            try
            {
                var donorDto = await _donorService.GetDonorProfileAsync(id);
                if (donorDto == null)
                {
                    return NotFound(new { error = $"Donor with ID {id} not found." });
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
                    donorDto.LastDonationDate
                };

                return Ok(donor);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ApiDonorController.GetDonor with ID {Id}", id);
                return StatusCode(500, new { error = "An error occurred while fetching the donor profile." });
            }
        }
    }
}
