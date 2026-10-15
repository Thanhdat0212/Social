using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Infrastructure.Common;
using Infrastructure.External;
using Infrastructure.Identity;
using Infrastructure.Marketplace;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Recommendation;
using Infrastructure.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Persistence & Unit of Work
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<SocialDbContext>(options =>
            options.UseNpgsql(connectionString, b => 
                b.MigrationsAssembly(typeof(SocialDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 2. HTTP Context, Current User & System Clock
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IClock, SystemClock>();

        // 3. Feature Modules & External Integrations
        services.AddSocialModule();
        services.AddMarketplaceModule();
        services.AddRecommendationModule();
        services.AddExternalServicesModule(configuration);

        return services;
    }
}
