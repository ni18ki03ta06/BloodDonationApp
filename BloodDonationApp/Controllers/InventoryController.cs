using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BloodDonationApp.Controllers
{
    public class InventoryController : Controller
    {
        private readonly IBloodInventoryService _bloodInventoryService;
        private readonly IMapper _mapper;

        public InventoryController(IBloodInventoryService bloodInventoryService, IMapper mapper)
        {
            _bloodInventoryService = bloodInventoryService;
            _mapper = mapper;
        }

        // GET: Inventory
        // GET: Inventory/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var dtos = await _bloodInventoryService.GetAllInventoryAsync();
                var inventory = _mapper.Map<List<BloodInventory>>(dtos);
                return View(inventory);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading blood inventory.");
                TempData["ErrorMessage"] = "A database error occurred while fetching blood inventory.";
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
