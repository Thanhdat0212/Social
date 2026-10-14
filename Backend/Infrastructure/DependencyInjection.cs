using System.Text;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Settings;
using Infrastructure.Caching;
using Infrastructure.Common;
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
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Shops.IShopRepository, Infrastructure.Persistence.Repositories.Marketplace.Shops.ShopRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Shops.IShopReviewRepository, Infrastructure.Persistence.Repositories.Marketplace.Shops.ShopReviewRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Shops.IShopSanctionRepository, Infrastructure.Persistence.Repositories.Marketplace.Shops.ShopSanctionRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRepository, Infrastructure.Persistence.Repositories.Marketplace.Categories.CategoryRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Categories.IAttributeRepository, Infrastructure.Persistence.Repositories.Marketplace.Categories.AttributeRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRequestRepository, Infrastructure.Persistence.Repositories.Marketplace.Categories.CategoryRequestRepository>();

        // 2. HTTP Context & Current User & Clock
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IClock, SystemClock>();

        // 3. Caching: Redis Distributed Cache (với In-Memory Fallback dự phòng)
        services.AddMemoryCache();
        var redisConn = configuration["Redis:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
            {
                var options = StackExchange.Redis.ConfigurationOptions.Parse(redisConn);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 5000;
                return StackExchange.Redis.ConnectionMultiplexer.Connect(options);
            });
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        // 4. Security & Cryptography
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<Application.Common.ICryptoService, Infrastructure.Security.AesGcmCryptoService>();
        services.AddSingleton<Application.Common.ISignedUrlService, Infrastructure.Security.SignedUrlService>();
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
        services.AddScoped<Application.Interfaces.Marketplace.Products.IProductMediaStorageService, CloudinaryProductMediaService>();

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
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<Application.Interfaces.Marketplace.Shops.IShopService, Infrastructure.Services.Marketplace.Shops.ShopService>();
        services.AddScoped<Application.Interfaces.Marketplace.Shops.IAdminShopService, Infrastructure.Services.Marketplace.Shops.AdminShopService>();
        services.AddScoped<Application.Interfaces.Marketplace.Categories.ICategoryService, Infrastructure.Services.Marketplace.Categories.CategoryService>();
        services.AddScoped<Application.Interfaces.Marketplace.Categories.IAdminCategoryService, Infrastructure.Services.Marketplace.Categories.AdminCategoryService>();
        services.AddScoped<Application.Interfaces.Marketplace.Categories.ISellerCategoryService, Infrastructure.Services.Marketplace.Categories.SellerCategoryService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.IProductService, Infrastructure.Services.Marketplace.Products.ProductService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.ICatalogService, Infrastructure.Services.Marketplace.Products.CatalogService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.IAdminProductService, Infrastructure.Services.Marketplace.Products.AdminProductService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.IProductReviewService, Infrastructure.Services.Marketplace.Products.ProductReviewService>();
        services.AddScoped<Application.Interfaces.Marketplace.Inventory.IInventoryService, Infrastructure.Services.Marketplace.Inventory.InventoryService>();
        services.AddScoped<Application.Interfaces.Marketplace.Orders.ICartService, Infrastructure.Services.Marketplace.Orders.CartService>();
        services.AddScoped<Application.Interfaces.Marketplace.Orders.ICheckoutService, Infrastructure.Services.Marketplace.Orders.CheckoutService>();
        services.AddScoped<Application.Interfaces.Marketplace.Orders.IOrderService, Infrastructure.Services.Marketplace.Orders.OrderService>();
        services.AddHostedService<Infrastructure.Services.Marketplace.Orders.CheckoutExpirationWorker>();
        services.AddHostedService<Infrastructure.Services.Marketplace.Orders.FundReleaseWorker>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Payments.IPaymentRepository, Infrastructure.Persistence.Repositories.Marketplace.Payments.PaymentRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Ledger.ILedgerRepository, Infrastructure.Persistence.Repositories.Marketplace.Ledger.LedgerRepository>();
        services.AddScoped<Application.Interfaces.Marketplace.Payments.IPaymentGateway, Infrastructure.Services.Marketplace.Payments.MockPaymentGateway>();
        services.AddHttpClient<Infrastructure.Services.Marketplace.Payments.PayOsPaymentGateway>();
        services.AddScoped<Application.Interfaces.Marketplace.Payments.IPaymentGateway>(sp => sp.GetRequiredService<Infrastructure.Services.Marketplace.Payments.PayOsPaymentGateway>());
        services.AddScoped<Application.Interfaces.Marketplace.Payments.IPaymentService, Infrastructure.Services.Marketplace.Payments.PaymentService>();
        services.AddScoped<Application.Interfaces.Marketplace.Ledger.ILedgerService, Infrastructure.Services.Marketplace.Ledger.LedgerService>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Complaints.IComplaintRepository, Infrastructure.Persistence.Repositories.Marketplace.Complaints.ComplaintRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Complaints.IRefundRepository, Infrastructure.Persistence.Repositories.Marketplace.Complaints.RefundRepository>();
        services.AddScoped<Application.Interfaces.Marketplace.Complaints.IComplaintService, Infrastructure.Services.Marketplace.Complaints.ComplaintService>();
        services.AddHostedService<Infrastructure.Services.Marketplace.Complaints.ComplaintEscalationWorker>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Payouts.IShopBankAccountRepository, Infrastructure.Persistence.Repositories.Marketplace.Payouts.ShopBankAccountRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Payouts.IPayoutRepository, Infrastructure.Persistence.Repositories.Marketplace.Payouts.PayoutRepository>();
        services.AddScoped<Application.Interfaces.Marketplace.Payouts.IPayoutService, Infrastructure.Services.Marketplace.Payouts.PayoutService>();
        services.AddScoped<Application.Interfaces.Marketplace.Audit.IReconciliationService, Infrastructure.Services.Marketplace.Audit.ReconciliationService>();
        services.AddHostedService<Infrastructure.Services.Marketplace.Audit.DailyReconciliationWorker>();
        services.AddScoped<Application.Interfaces.Marketplace.Reports.IAbuseProtectionService, Infrastructure.Services.Marketplace.Reports.AbuseProtectionService>();
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
