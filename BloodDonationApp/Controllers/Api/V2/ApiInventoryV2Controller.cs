using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Models;
using AutoMapper;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using System;
using Microsoft.AspNetCore.Authorization;

namespace BloodDonationApp.Controllers.Api.V2
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/inventory")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ApiInventoryV2Controller : ControllerBase
    {
        private readonly IBloodInventoryService _bloodInventoryService;
        private readonly IMapper _mapper;

        public ApiInventoryV2Controller(IBloodInventoryService bloodInventoryService, IMapper mapper)
        {
            _bloodInventoryService = bloodInventoryService;
            _mapper = mapper;
        }

        // GET: api/v2/inventory
        [HttpGet]
        public async Task<IActionResult> GetInventoryEnhanced()
        {
            try
            {
                var dtos = await _bloodInventoryService.GetAllInventoryAsync();
                var inventory = _mapper.Map<List<BloodInventory>>(dtos);

                var totalUnitsAvailable = inventory.Sum(i => i.UnitsAvailable);
                var totalUnitsReserved = inventory.Sum(i => i.UnitsReserved);
                
                var items = inventory.Select(i => new
                {
                    i.Id,
                    i.BloodType,
                    i.UnitsAvailable,
                    i.UnitsReserved,
                    Status = i.UnitsAvailable < 5 ? "Critical" : (i.UnitsAvailable < 10 ? "Warning" : "Healthy"),
                    i.LastUpdated
                }).ToList();

                var response = new
                {
                    TotalUnitsAvailable = totalUnitsAvailable,
                    TotalUnitsReserved = totalUnitsReserved,
                    ReportTimestamp = DateTime.UtcNow,
                    StockStatus = totalUnitsAvailable < 50 ? "Low" : "Optimal",
                    Inventory = items
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ApiInventoryV2Controller.GetInventoryEnhanced");
                throw;
            }
        }
    }
}
