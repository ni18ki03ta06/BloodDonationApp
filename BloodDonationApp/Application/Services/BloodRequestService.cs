using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Application.Interfaces;
using BloodDonationApp.Core.Interfaces;
using BloodDonationApp.Models;
using BloodDonationApp.Services;
using Microsoft.AspNetCore.SignalR;
using BloodDonationApp.Hubs;

namespace BloodDonationApp.Application.Services
{
    public class BloodRequestService : IBloodRequestService
    {
        private readonly IBloodRequestRepository _bloodRequestRepository;
        private readonly IDonorRepository _donorRepository;
        private readonly IGoogleMapsService _googleMapsService;
        private readonly IDonorRecommendationService _recommendationService;
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IMapper _mapper;

        public BloodRequestService(
            IBloodRequestRepository bloodRequestRepository,
            IDonorRepository donorRepository,
            IGoogleMapsService googleMapsService,
            IDonorRecommendationService recommendationService,
            IRepository<AuditLog> auditLogRepository,
            IHubContext<NotificationHub> hubContext,
            IMapper mapper)
        {
            _bloodRequestRepository = bloodRequestRepository;
            _donorRepository = donorRepository;
            _googleMapsService = googleMapsService;
            _recommendationService = recommendationService;
            _auditLogRepository = auditLogRepository;
            _hubContext = hubContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<BloodRequestDto>> GetAllRequestsAsync()
        {
            var requests = await _bloodRequestRepository.GetBloodRequestsWithFulfilledDonorAsync();
            return _mapper.Map<IEnumerable<BloodRequestDto>>(requests);
        }

        public async Task<IEnumerable<BloodRequestDto>> GetRequestsFilteredAsync(string? status, string? urgency)
        {
            var requests = await _bloodRequestRepository.GetBloodRequestsWithFulfilledDonorAsync();
            
            if (!string.IsNullOrEmpty(status))
            {
                requests = requests.Where(r => r.Status == status);
            }

            if (!string.IsNullOrEmpty(urgency) && Enum.TryParse<UrgencyLevel>(urgency, true, out var urgencyEnum))
            {
                requests = requests.Where(r => r.UrgencyLevel == urgencyEnum);
            }

            return _mapper.Map<IEnumerable<BloodRequestDto>>(requests);
        }

        public async Task<BloodRequestDto?> GetRequestByIdAsync(int id)
        {
            var request = await _bloodRequestRepository.GetByIdAsync(id);
            if (request == null) return null;
            return _mapper.Map<BloodRequestDto>(request);
        }

        public async Task<BloodRequestDto> CreateRequestAsync(BloodRequestDto requestDto)
        {
            var bloodRequest = _mapper.Map<BloodRequest>(requestDto);

            var requestAddr = $"{bloodRequest.Hospital}, {bloodRequest.City}";
            var coords = await _googleMapsService.GeocodeAddressAsync(requestAddr);
            if (coords.HasValue)
            {
                bloodRequest.Latitude = coords.Value.Latitude;
                bloodRequest.Longitude = coords.Value.Longitude;
            }

            bloodRequest.Status = "Pending";
            bloodRequest.CreatedAt = DateTime.UtcNow;

            await _bloodRequestRepository.AddAsync(bloodRequest);
            await _bloodRequestRepository.SaveChangesAsync();

            await _recommendationService.NotifyTopDonorsAsync(bloodRequest);

            // Broadcast real-time match alerts to connected matching donors
            string bloodGroup = $"BloodType_{bloodRequest.BloodType.Trim().ToUpper().Replace(" ", "+")}";
            await _hubContext.Clients.Group(bloodGroup).SendAsync("ReceiveMatchAlert", new
            {
                id = bloodRequest.Id,
                patientName = bloodRequest.PatientName,
                bloodType = bloodRequest.BloodType,
                units = bloodRequest.Units,
                hospital = bloodRequest.Hospital,
                city = bloodRequest.City,
                urgencyLevel = bloodRequest.UrgencyLevel.ToString(),
                status = bloodRequest.Status,
                createdAt = bloodRequest.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return _mapper.Map<BloodRequestDto>(bloodRequest);
        }

        public async Task<bool> ApproveRequestAsync(int id)
        {
            var request = await _bloodRequestRepository.GetByIdAsync(id);
            if (request == null) return false;

            request.Status = "Approved";
            await _bloodRequestRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectRequestAsync(int id)
        {
            var request = await _bloodRequestRepository.GetByIdAsync(id);
            if (request == null) return false;

            request.Status = "Rejected";
            await _bloodRequestRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MakeEmergencyAsync(int id)
        {
            var request = await _bloodRequestRepository.GetByIdAsync(id);
            if (request == null) return false;

            request.Status = "Emergency";
            request.UrgencyLevel = UrgencyLevel.Critical;
            await _bloodRequestRepository.SaveChangesAsync();

            await _recommendationService.NotifyTopDonorsAsync(request);
            return true;
        }

        public async Task<bool> AssignDonorAsync(int requestId, int donorId)
        {
            var request = await _bloodRequestRepository.GetByIdAsync(requestId);
            var donor = await _donorRepository.GetByIdAsync(donorId);

            if (request == null || donor == null) return false;

            request.FulfilledBy = donor.Id;
            request.FulfilledAt = DateTime.UtcNow;
            request.Status = "Fulfilled";

            donor.LastDonationDate = DateTime.Today;

            _bloodRequestRepository.Update(request);
            _donorRepository.Update(donor);
            await _bloodRequestRepository.SaveChangesAsync();
            await _donorRepository.SaveChangesAsync();
            return true;
        }
    }
}
