using Application.DTOs.Auth.Requests;
using Application.DTOs.Auth.Responses;
using Application.DTOs.Profile.Responses;
using AutoMapper;
using Domain.Entities;

namespace Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Auth Mappings
        CreateMap<RegisterRequestDto, User>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim()))
            .ForMember(dest => dest.NormalizedEmail, opt => opt.MapFrom(src => src.Email.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName.Trim()))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
            .ForMember(dest => dest.VerificationTokens, opt => opt.Ignore());

        CreateMap<User, UserDto>();

        // Profile Mappings
        CreateMap<User, ProfileDto>();
    }
}
