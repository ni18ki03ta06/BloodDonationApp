using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace BloodDonationApp.Services
{
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        // Deterministic fallback dictionary for standard cities
        private static readonly Dictionary<string, (double Lat, double Lon)> FallbackCityCoordinates = 
            new Dictionary<string, (double, double)>(StringComparer.OrdinalIgnoreCase)
            {
                { "pune", (18.5204, 73.8567) },
                { "mumbai", (19.0760, 72.8777) },
                { "goa", (15.2993, 74.1240) },
                { "panaji", (15.4989, 73.8278) },
                { "kothrud", (18.5074, 73.8077) },
                { "ganeshkhind", (18.5385, 73.8340) },
                { "shivaji nagar", (18.5312, 73.8446) },
                { "delhi", (28.6139, 77.2090) },
                { "bangalore", (12.9716, 77.5946) },
                { "bengaluru", (12.9716, 77.5946) },
                { "kolkata", (22.5726, 88.3639) },
                { "chennai", (13.0827, 80.2707) },
                { "hyderabad", (17.3850, 78.4867) }
            };

        public GoogleMapsService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            // Set User-Agent which is required by Nominatim
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BloodDonationApp/1.0 (tbans@gemini.com)");
            
            var key = configuration["GoogleMaps:ApiKey"];
            if (!string.IsNullOrEmpty(key) && 
                !string.Equals(key, "YOUR_GOOGLE_MAPS_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                _apiKey = key;
            }
        }

        public async Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;

            // 1. Google Maps Geocoding API if key is present
            if (!string.IsNullOrEmpty(_apiKey))
            {
                try
                {
                    Log.Information("Geocoding using Google Maps API for address: {Address}", address);
                    var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_apiKey}";
                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var status = doc.RootElement.GetProperty("status").GetString();
                        if (string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                        {
                            var location = doc.RootElement
                                .GetProperty("results")[0]
                                .GetProperty("geometry")
                                .GetProperty("location");
                            
                            double lat = location.GetProperty("lat").GetDouble();
                            double lng = location.GetProperty("lng").GetDouble();
                            return (lat, lng);
                        }
                        else
                        {
                            Log.Warning("Google Geocoding failed with status: {Status}", status);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while geocoding via Google Maps API for address: {Address}", address);
                }
            }

            // 2. OpenStreetMap Nominatim Fallback
            try
            {
                Log.Information("Geocoding fallback via OpenStreetMap Nominatim for address: {Address}", address);
                var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.GetArrayLength() > 0)
                    {
                        var element = doc.RootElement[0];
                        if (double.TryParse(element.GetProperty("lat").GetString(), out double lat) &&
                            double.TryParse(element.GetProperty("lon").GetString(), out double lon))
                        {
                            return (lat, lon);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while geocoding via Nominatim for address: {Address}", address);
            }

            // 3. Local Dictionary Fallback
            Log.Information("Geocoding local dictionary fallback for address: {Address}", address);
            foreach (var kvp in FallbackCityCoordinates)
            {
                if (address.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("Matched local dictionary city: {City} -> Coordinates: {Lat}, {Lon}", kvp.Key, kvp.Value.Lat, kvp.Value.Lon);
                    
                    // Add slight random offset to simulate multiple distinct addresses in the same city
                    var random = new Random(address.GetHashCode());
                    double offsetLat = (random.NextDouble() - 0.5) * 0.04; // ~4km radius
                    double offsetLon = (random.NextDouble() - 0.5) * 0.04;
                    return (kvp.Value.Lat + offsetLat, kvp.Value.Lon + offsetLon);
                }
            }

            // Return default center (Pune) if everything else fails
            Log.Warning("All geocoding fallbacks failed. Defaulting to Pune coordinates.");
            return (18.5204, 73.8567);
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusKm = 6371.0;

            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        private static double ToRadians(double val)
        {
            return (Math.PI / 180) * val;
        }
    }
}
