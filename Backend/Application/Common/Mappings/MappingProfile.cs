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

        // Interest & Preference Mappings
        CreateMap<Interest, Application.DTOs.Interests.InterestDto>()
            .ForMember(dest => dest.IsSelected, opt => opt.Ignore());

        CreateMap<UserPreference, Application.DTOs.Interests.UserPreferenceDto>()
            .ForMember(dest => dest.InterestName, opt => opt.MapFrom(src => src.Interest.Name))
            .ForMember(dest => dest.InterestSlug, opt => opt.MapFrom(src => src.Interest.Slug))
            .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Interest.Icon));

        // Post Mappings
        CreateMap<User, Application.DTOs.Posts.PostAuthorDto>();

        CreateMap<PostInterest, Application.DTOs.Posts.PostTopicDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Interest.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Interest.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Interest.Slug))
            .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Interest.Icon))
            .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Confidence));

        CreateMap<Domain.Entities.Marketplace.PostProduct, Application.DTOs.Posts.PostTaggedProductDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Product.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Product.Slug))
            .ForMember(dest => dest.MinPrice, opt => opt.MapFrom(src => src.Product.MinPriceCache))
            .ForMember(dest => dest.MaxPrice, opt => opt.MapFrom(src => src.Product.MaxPriceCache))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.Product.Images.OrderBy(i => i.Position).Select(i => i.Url).FirstOrDefault()))
            .ForMember(dest => dest.ShopId, opt => opt.MapFrom(src => src.Product.ShopId))
            .ForMember(dest => dest.ShopName, opt => opt.MapFrom(src => src.Product.Shop != null ? src.Product.Shop.Name : string.Empty))
            .ForMember(dest => dest.ShopSlug, opt => opt.MapFrom(src => src.Product.Shop != null ? src.Product.Shop.Slug : string.Empty));

        CreateMap<Post, Application.DTOs.Posts.PostDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Topics, opt => opt.MapFrom(src => src.PostInterests))
            .ForMember(dest => dest.TaggedProducts, opt => opt.MapFrom(src => src.PostProducts));
    }
}
