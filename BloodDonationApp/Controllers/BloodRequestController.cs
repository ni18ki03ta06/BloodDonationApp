using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Models;
using BloodDonationApp.Filters;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Application.DTOs;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BloodDonationApp.Controllers
{
    [AuthFilter]
    public class BloodRequestController : Controller
    {
        private readonly IBloodRequestService _bloodRequestService;
        private readonly IMapper _mapper;

        public BloodRequestController(IBloodRequestService bloodRequestService, IMapper mapper)
        {
            _bloodRequestService = bloodRequestService;
            _mapper = mapper;
        }

        // GET: BloodRequest/Create
        public IActionResult Create(string bloodType)
        {
            var model = new BloodRequest();
            if (!string.IsNullOrEmpty(bloodType))
            {
                model.BloodType = bloodType.Replace(" ", "+").Trim();
            }
            return View(model);
        }

        // POST: BloodRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientName,BloodType,Units,Hospital,City,ContactNumber,RequiredDate,UrgencyLevel,RequesterName,RequesterEmail,Diagnosis,IsAnonymous")] BloodRequest bloodRequest)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var requestDto = _mapper.Map<BloodRequestDto>(bloodRequest);
                    await _bloodRequestService.CreateRequestAsync(requestDto);

                    TempData["SuccessMessage"] = "Your blood request has been successfully submitted and is awaiting administrator review.";
                    return RedirectToAction("Dashboard", "Donor");
                }
                return View(bloodRequest);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error creating blood request for patient {PatientName}", bloodRequest?.PatientName);
                TempData["ErrorMessage"] = "A database error occurred while submitting your blood request.";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: BloodRequest/List
        public async Task<IActionResult> List()
        {
            try
            {
                var dtos = await _bloodRequestService.GetAllRequestsAsync();
                var requests = _mapper.Map<List<BloodRequest>>(dtos);
                return View(requests);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Database error loading blood requests list.");
                TempData["ErrorMessage"] = "A database error occurred while fetching the blood requests list.";
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
