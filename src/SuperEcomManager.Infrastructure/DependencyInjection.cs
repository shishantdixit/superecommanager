using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.Authentication;
using SuperEcomManager.Infrastructure.BackgroundJobs;
using SuperEcomManager.Infrastructure.Persistence;
using SuperEcomManager.Infrastructure.Persistence.Interceptors;
using SuperEcomManager.Infrastructure.Services;

namespace SuperEcomManager.Infrastructure;

/// <summary>
/// Dependency injection configuration for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // Register ApplicationDbContext (shared schema)
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "shared");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });
        });

        // Register TenantDbContext (tenant-specific schema)
        services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
        {
            var currentTenantService = serviceProvider.GetRequiredService<ICurrentTenantService>();

            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });
        });

        // Register interfaces
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ITenantDbContext>(sp => sp.GetRequiredService<TenantDbContext>());

        // Register services
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IEventBus, EventBus>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();

        // Register distributed cache (Redis)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "SuperEcomManager:";
            });
        }
        else
        {
            // Fallback to in-memory cache for development
            services.AddDistributedMemoryCache();
        }

        // Register JWT authentication
        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        // Register authentication services
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Register background job services
        services.Configure<OrderSyncSettings>(configuration.GetSection(OrderSyncSettings.SectionName));
        services.AddScoped<OrderSyncJob>();
        services.AddHostedService<OrderSyncHostedService>();

        return services;
    }
}
