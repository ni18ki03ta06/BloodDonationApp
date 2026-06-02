using System.Collections.Generic;
using System.Threading.Tasks;
using BloodDonationApp.Application.DTOs;

namespace BloodDonationApp.Application.Interfaces
{
    public interface IBloodRequestService
    {
        Task<IEnumerable<BloodRequestDto>> GetAllRequestsAsync();
        Task<IEnumerable<BloodRequestDto>> GetRequestsFilteredAsync(string? status, string? urgency);
        Task<BloodRequestDto?> GetRequestByIdAsync(int id);
        Task<BloodRequestDto> CreateRequestAsync(BloodRequestDto requestDto);
        Task<bool> ApproveRequestAsync(int id);
        Task<bool> RejectRequestAsync(int id);
        Task<bool> MakeEmergencyAsync(int id);
        Task<bool> AssignDonorAsync(int requestId, int donorId);
    }
}
