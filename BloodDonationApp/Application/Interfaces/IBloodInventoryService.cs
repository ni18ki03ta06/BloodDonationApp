using System.Collections.Generic;
using System.Threading.Tasks;
using BloodDonationApp.Application.DTOs;

namespace BloodDonationApp.Application.Interfaces
{
    public interface IBloodInventoryService
    {
        Task<IEnumerable<BloodInventoryDto>> GetAllInventoryAsync();
        Task<BloodInventoryDto?> GetInventoryByIdAsync(int id);
        Task<(bool Success, int OldUnits, string BloodType, DateTime LastUpdated, int UnitsReserved)> UpdateInventoryStockAsync(int id, int units);
    }
}
