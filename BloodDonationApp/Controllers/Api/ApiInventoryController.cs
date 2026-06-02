using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Models;
using AutoMapper;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using System;

namespace BloodDonationApp.Controllers.Api
{
    [ApiController]
    [Route("api/inventory")]
    public class ApiInventoryController : ControllerBase
    {
        private readonly IBloodInventoryService _bloodInventoryService;
        private readonly IMapper _mapper;

        public ApiInventoryController(IBloodInventoryService bloodInventoryService, IMapper mapper)
        {
            _bloodInventoryService = bloodInventoryService;
            _mapper = mapper;
        }

        // GET: api/inventory
        [HttpGet]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var dtos = await _bloodInventoryService.GetAllInventoryAsync();
                var inventory = _mapper.Map<List<BloodInventory>>(dtos);
                return Ok(inventory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ApiInventoryController.GetInventory");
                return StatusCode(500, new { error = "An error occurred while fetching blood inventory." });
            }
        }
    }
}
