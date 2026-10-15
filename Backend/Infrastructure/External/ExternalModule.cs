using System.Text;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Recommendation;
using Application.Settings;
using Infrastructure.External.AI;
using Infrastructure.External.Caching;
using Infrastructure.External.Email;
using Infrastructure.External.Media;
using Infrastructure.External.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.External;

/// <summary>Đăng ký các dịch vụ tích hợp bên ngoài: Caching, Email, Media, AI, Security và JWT Authentication.</summary>
public static class ExternalModule
{
    public static IServiceCollection AddExternalServicesModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Caching: Redis Distributed Cache với In-Memory Fallback
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

        // 2. Security, Cryptography & JWT Token Service
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<ICryptoService, AesGcmCryptoService>();
        services.AddSingleton<ISignedUrlService, SignedUrlService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // 3. JWT Authentication Configuration
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

            // Token từ query param ?access_token= cho WebSocket SignalR
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

        // 4. App Settings Bindings
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<BrevoSettings>(configuration.GetSection(BrevoSettings.SectionName));
        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));
        services.Configure<GoogleSettings>(configuration.GetSection(GoogleSettings.SectionName));
        services.Configure<GeminiSettings>(configuration.GetSection(GeminiSettings.SectionName));

        // 5. Email Sender (Brevo / Smtp / Console)
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

        // 6. Media & Storage (Cloudinary)
        services.AddScoped<IAvatarStorageService, CloudinaryAvatarService>();
        services.AddScoped<IPostMediaStorageService, CloudinaryPostMediaService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.IProductMediaStorageService, CloudinaryProductMediaService>();

        // 7. AI & Embedding Services (Gemini)
        services.AddHttpClient<IAiContentAnalyzer, GeminiService>();
        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();

        // 8. Asynchronous AI Background Worker & Channel
        services.AddSingleton<IPostAiChannel, PostAiChannel>();
        services.AddHostedService<PostAiBackgroundWorker>();

        return services;
    }
}
