using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Models;
using AutoMapper;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using System;
using Microsoft.AspNetCore.Authorization;

namespace BloodDonationApp.Controllers.Api.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/inventory")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ApiInventoryV1Controller : ControllerBase
    {
        private readonly IBloodInventoryService _bloodInventoryService;
        private readonly IMapper _mapper;

        public ApiInventoryV1Controller(IBloodInventoryService bloodInventoryService, IMapper mapper)
        {
            _bloodInventoryService = bloodInventoryService;
            _mapper = mapper;
        }

        // GET: api/v1/inventory
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
                Log.Error(ex, "Error occurred in ApiInventoryV1Controller.GetInventory");
                throw;
            }
        }
    }
}
