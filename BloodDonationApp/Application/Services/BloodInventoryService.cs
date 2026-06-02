using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Models;

namespace BloodDonationApp.Application.Services
{
    public class BloodInventoryService : IBloodInventoryService
    {
        private readonly IBloodInventoryRepository _bloodInventoryRepository;
        private readonly IMapper _mapper;

        public BloodInventoryService(IBloodInventoryRepository bloodInventoryRepository, IMapper mapper)
        {
            _bloodInventoryRepository = bloodInventoryRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<BloodInventoryDto>> GetAllInventoryAsync()
        {
            var inventory = await _bloodInventoryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<BloodInventoryDto>>(inventory);
        }

        public async Task<BloodInventoryDto?> GetInventoryByIdAsync(int id)
        {
            var item = await _bloodInventoryRepository.GetByIdAsync(id);
            if (item == null) return null;
            return _mapper.Map<BloodInventoryDto>(item);
        }

        public async Task<(bool Success, int OldUnits, string BloodType, DateTime LastUpdated, int UnitsReserved)> UpdateInventoryStockAsync(int id, int units)
        {
            if (units < 0) return (false, 0, string.Empty, DateTime.Now, 0);

            var inventory = await _bloodInventoryRepository.GetByIdAsync(id);
            if (inventory == null) return (false, 0, string.Empty, DateTime.Now, 0);

            int oldUnits = inventory.UnitsAvailable;
            string bloodType = inventory.BloodType;
            int unitsReserved = inventory.UnitsReserved;

            inventory.UnitsAvailable = units;
            inventory.LastUpdated = DateTime.Now;
            DateTime lastUpdated = inventory.LastUpdated;

            _bloodInventoryRepository.Update(inventory);
            await _bloodInventoryRepository.SaveChangesAsync();
            return (true, oldUnits, bloodType, lastUpdated, unitsReserved);
        }
    }
}
