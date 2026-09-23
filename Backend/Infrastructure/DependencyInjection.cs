using System.Text;
using Application.Common;
using Application.Interfaces;
using Application.Options;
using Infrastructure.Email;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Services;
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
        // 1. Persistence
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<SocialDbContext>(options =>
            options.UseNpgsql(connectionString, b => 
                b.MigrationsAssembly(typeof(SocialDbContext).Assembly.FullName)));

        // 2. HTTP Context & Current User
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 3. Security & Cryptography
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // 4. JWT Options & Authentication
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        var key = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jwtOptions.SigningKey) 
            ? "DefaultSuperSecretKeyForDevelopmentPhase1Only123456" 
            : jwtOptions.SigningKey);

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
                ValidateIssuer = !string.IsNullOrEmpty(jwtOptions.Issuer),
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = !string.IsNullOrEmpty(jwtOptions.Audience),
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // 5. Options (App, Email, Smtp, Brevo)
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<BrevoOptions>(configuration.GetSection(BrevoOptions.SectionName));

        // 6. Email Sender (Console / Smtp / Brevo)
        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        if (string.Equals(emailOptions.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else if (string.Equals(emailOptions.Provider, "Brevo", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IEmailSender, BrevoEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }

        // 7. Domain / Infrastructure Services Implementation
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
