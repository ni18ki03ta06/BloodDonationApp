using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Models;
using BloodDonationApp.Services;
using BloodDonationApp.Helpers;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace BloodDonationApp.Controllers
{
    public class MapController : Controller
    {
        private readonly IDonorRepository _donorRepository;
        private readonly IRepository<BloodCamp> _campRepository;
        private readonly IGoogleMapsService _googleMapsService;
        private readonly IConfiguration _configuration;

        public MapController(
            IDonorRepository donorRepository, 
            IRepository<BloodCamp> campRepository,
            IGoogleMapsService googleMapsService, 
            IConfiguration configuration)
        {
            _donorRepository = donorRepository;
            _campRepository = campRepository;
            _googleMapsService = googleMapsService;
            _configuration = configuration;
        }

        // GET: Map/Finder
        public async Task<IActionResult> Finder(string? searchTerm, string? bloodType)
        {
            try
            {
                var googleMapsApiKey = _configuration["GoogleMaps:ApiKey"];
                ViewBag.GoogleMapsApiKey = googleMapsApiKey;

                // Determine current user context
                var userIdStr = HttpContext.Session.GetString("UserId");
                var adminIdStr = HttpContext.Session.GetString("AdminId");
                bool isAdmin = !string.IsNullOrEmpty(adminIdStr) || HttpContext.Session.GetString("UserRole") == "Admin";

                ViewBag.IsAdmin = isAdmin;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.BloodTypeFilter = bloodType;

                double defaultLat = 18.5204; // Pune default center
                double defaultLng = 73.8567;
                string centerLabel = "Default: Pune";

                // Resolve search center coordinates
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchCoords = await _googleMapsService.GeocodeAddressAsync(searchTerm);
                    if (searchCoords.HasValue)
                    {
                        defaultLat = searchCoords.Value.Latitude;
                        defaultLng = searchCoords.Value.Longitude;
                        centerLabel = $"Search: {searchTerm}";
                    }
                }
                else if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    // If no search, center on logged-in donor's city
                    var donor = await _donorRepository.GetByIdAsync(userId);
                    if (donor != null && (!string.IsNullOrEmpty(donor.City)))
                    {
                        var donorCoords = await _googleMapsService.GeocodeAddressAsync($"{donor.City}, {donor.State}");
                        if (donorCoords.HasValue)
                        {
                            defaultLat = donorCoords.Value.Latitude;
                            defaultLng = donorCoords.Value.Longitude;
                            centerLabel = $"My City: {donor.City}";
                        }
                    }
                }

                ViewBag.CenterLat = defaultLat;
                ViewBag.CenterLng = defaultLng;
                ViewBag.CenterLabel = centerLabel;

                // 1. Find Donors
                var compatibleTypes = new List<string>();
                if (!string.IsNullOrEmpty(bloodType))
                {
                    compatibleTypes = BloodCompatibilityHelper.GetCompatibleBloodTypes(bloodType).ToList();
                }

                var donors = (await _donorRepository.FindAsync(d => d.IsVerified && d.IsAvailable)).ToList();

                if (!string.IsNullOrEmpty(bloodType))
                {
                    // Filter donors by compatibility
                    donors = donors.Where(d => compatibleTypes.Contains(d.BloodType)).ToList();
                }

                // Calculate distance for all donors
                var nearbyDonors = new List<NearbyDonorViewModel>();
                foreach (var d in donors)
                {
                    if (!d.Latitude.HasValue || !d.Longitude.HasValue)
                    {
                        // Geocode missing coordinates on the fly
                        var addr = $"{d.Address}, {d.City}, {d.State} {d.PinCode}";
                        var coords = await _googleMapsService.GeocodeAddressAsync(addr);
                        if (coords.HasValue)
                        {
                            d.Latitude = coords.Value.Latitude;
                            d.Longitude = coords.Value.Longitude;
                            _donorRepository.Update(d);
                        }
                    }

                    if (d.Latitude.HasValue && d.Longitude.HasValue)
                    {
                        double dist = _googleMapsService.CalculateDistance(defaultLat, defaultLng, d.Latitude.Value, d.Longitude.Value);
                        nearbyDonors.Add(new NearbyDonorViewModel
                        {
                            Donor = d,
                            DistanceKm = dist
                        });
                    }
                }
                
                await _donorRepository.SaveChangesAsync();

                // Sort nearby donors by distance
                ViewBag.NearbyDonors = nearbyDonors.OrderBy(nd => nd.DistanceKm).ToList();

                // 2. Find Camps
                var camps = (await _campRepository.FindAsync(c => c.IsActive && c.ScheduledDate >= DateTime.Today)).ToList();

                var nearbyCamps = new List<NearbyCampViewModel>();
                foreach (var c in camps)
                {
                    if (!c.Latitude.HasValue || !c.Longitude.HasValue)
                    {
                        var addr = $"{c.Location}, {c.City}, {c.State}";
                        var coords = await _googleMapsService.GeocodeAddressAsync(addr);
                        if (coords.HasValue)
                        {
                            c.Latitude = coords.Value.Latitude;
                            c.Longitude = coords.Value.Longitude;
                            _campRepository.Update(c);
                        }
                    }

                    if (c.Latitude.HasValue && c.Longitude.HasValue)
                    {
                        double dist = _googleMapsService.CalculateDistance(defaultLat, defaultLng, c.Latitude.Value, c.Longitude.Value);
                        nearbyCamps.Add(new NearbyCampViewModel
                        {
                            Camp = c,
                            DistanceKm = dist
                        });
                    }
                }

                await _campRepository.SaveChangesAsync();

                // Sort nearby camps by distance
                ViewBag.NearbyCamps = nearbyCamps.OrderBy(nc => nc.DistanceKm).ToList();

                return View();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in Map/Finder action.");
                TempData["ErrorMessage"] = "A database or maps service error occurred.";
                return RedirectToAction("Error", "Home");
            }
        }
    }

    public class NearbyDonorViewModel
    {
        public Donor Donor { get; set; } = null!;
        public double DistanceKm { get; set; }
    }

    public class NearbyCampViewModel
    {
        public BloodCamp Camp { get; set; } = null!;
        public double DistanceKm { get; set; }
    }
}
