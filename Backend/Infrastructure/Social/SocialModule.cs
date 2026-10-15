using Infrastructure.Social.Auth;
using Infrastructure.Social.Profile;
using Infrastructure.Social.Posts;
using Infrastructure.Social.Comments;
using Infrastructure.Social.Follows;
using Infrastructure.Social.Interests;
using Infrastructure.Social.Interactions;
using Infrastructure.Social.Notifications;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Social;

/// <summary>Đăng ký repository và service của module Social (user, post, comment, follow, notification...).</summary>
public static class SocialModule
{
    public static IServiceCollection AddSocialModule(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
        services.AddScoped<IInterestRepository, InterestRepository>();
        services.AddScoped<IUserInterestRepository, UserInterestRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IPostInterestRepository, PostInterestRepository>();
        services.AddScoped<IPostLikeRepository, PostLikeRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IUserFollowRepository, UserFollowRepository>();
        services.AddScoped<IUserInteractionRepository, UserInteractionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IInterestService, InterestService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<ILikeService, LikeService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IFollowService, FollowService>();
        services.AddScoped<IInteractionService, InteractionService>();
        services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
