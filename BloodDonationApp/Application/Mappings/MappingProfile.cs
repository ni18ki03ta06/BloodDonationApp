using AutoMapper;
using BloodDonationApp.Application.DTOs;
using BloodDonationApp.Models;

namespace BloodDonationApp.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Donor, DonorDto>().ReverseMap();
            CreateMap<Appointment, AppointmentDto>().ReverseMap();
            CreateMap<BloodRequest, BloodRequestDto>().ReverseMap();
            CreateMap<BloodInventory, BloodInventoryDto>().ReverseMap();
            CreateMap<Badge, BadgeDto>().ReverseMap();
            CreateMap<Reward, RewardDto>().ReverseMap();
            CreateMap<RedeemedReward, RedeemedRewardDto>().ReverseMap();
        }
    }
}
