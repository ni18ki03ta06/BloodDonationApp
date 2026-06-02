using System.Threading.Tasks;

namespace BloodDonationApp.Services
{
    public interface IGoogleMapsService
    {
        Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(string address);
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
    }
}
