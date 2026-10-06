using System.Text;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Settings;
using Infrastructure.Email;
using Infrastructure.Identity;
using Infrastructure.Media;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Infrastructure.Services;
using Infrastructure.AI;
using Application.Interfaces.Recommendation;
using Infrastructure.Recommendation;
using Infrastructure.Recommendation.Aggregator;
using Infrastructure.Recommendation.CandidateGenerators;
using Infrastructure.Recommendation.Diversity;
using Infrastructure.Recommendation.Ranking;
using Infrastructure.Recommendation.Collaborative;
using Infrastructure.Recommendation.Semantic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Persistence & Unit of Work / Repositories
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<SocialDbContext>(options =>
            options.UseNpgsql(connectionString, b => 
                b.MigrationsAssembly(typeof(SocialDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
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

        // 2. HTTP Context & Current User
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 3. Security & Cryptography
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // 4. JWT Settings & Authentication
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);
        var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

        var key = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jwtSettings.SigningKey)
            ? "DefaultSuperSecretKeyForDevelopmentPhase1Only123456"
            : jwtSettings.SigningKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = !string.IsNullOrEmpty(jwtSettings.Issuer),
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = !string.IsNullOrEmpty(jwtSettings.Audience),
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Cấu hình bắt token từ query param ?access_token= khi kết nối WebSocket (SignalR)
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // 5. Settings (App, Email, Smtp, Brevo, Cloudinary, Google)
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<BrevoSettings>(configuration.GetSection(BrevoSettings.SectionName));
        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));
        services.Configure<GoogleSettings>(configuration.GetSection(GoogleSettings.SectionName));
        services.Configure<GeminiSettings>(configuration.GetSection(GeminiSettings.SectionName));

        // 6. Email Sender (Console / Smtp / Brevo)
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        var emailProvider = configuration["Email:Provider"]
            ?? Environment.GetEnvironmentVariable("Email__Provider")
            ?? Environment.GetEnvironmentVariable("EMAIL_PROVIDER")
            ?? emailSettings.Provider;

        var hasBrevoKey = !string.IsNullOrWhiteSpace(configuration["Brevo:ApiKey"])
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Brevo__ApiKey"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BREVO_API_KEY"));

        if (string.Equals(emailProvider, "Brevo", StringComparison.OrdinalIgnoreCase) || hasBrevoKey)
        {
            services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });
        }
        else if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }

        // 7. Media & Storage
        services.AddScoped<IAvatarStorageService, CloudinaryAvatarService>();
        services.AddScoped<IPostMediaStorageService, CloudinaryPostMediaService>();

        // 8. Domain / Infrastructure Services Implementation
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
        services.AddHttpClient<IAiContentAnalyzer, GeminiService>();
        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();

        // 9. Asynchronous AI Background Worker & Channel
        services.AddSingleton<IPostAiChannel, PostAiChannel>();
        services.AddHostedService<PostAiBackgroundWorker>();

        // 10. Hybrid Recommendation Pipeline (Generators -> Aggregator -> Ranking -> Diversity)
        services.AddScoped<ICollaborativeFilteringService, CollaborativeFilteringService>();
        services.AddScoped<ISemanticVectorSearchService, SemanticVectorSearchService>();
        services.AddScoped<ICandidateGenerator, RecentCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, InterestCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, FollowingCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, TrendingCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, ExplorationCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, CollaborativeCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, SemanticCandidateGenerator>();
        services.AddScoped<ICandidateAggregator, CandidateAggregator>();
        services.AddScoped<IRecommendationRankingService, RecommendationRankingService>();
        services.AddScoped<IDiversityService, DiversityService>();
        services.AddScoped<IRecommendationService, RecommendationService>();

        return services;
    }
}
