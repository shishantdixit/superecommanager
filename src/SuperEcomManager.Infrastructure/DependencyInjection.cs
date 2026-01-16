using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.Authentication;
using SuperEcomManager.Infrastructure.BackgroundJobs;
using SuperEcomManager.Infrastructure.Persistence;
using SuperEcomManager.Infrastructure.Persistence.Interceptors;
using SuperEcomManager.Infrastructure.Persistence.Migrations;
using SuperEcomManager.Infrastructure.Persistence.Seeding;
using SuperEcomManager.Infrastructure.RateLimiting;
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
            var schemaName = currentTenantService.HasTenant ? currentTenantService.SchemaName : "public";

            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    // Configure migrations history table to be in the tenant's schema
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schemaName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });

            // Use custom model cache key factory to support schema-per-tenant
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();

            // Suppress PendingModelChangesWarning to allow migrations to run
            // This is needed for multi-tenant scenarios where we apply migrations dynamically
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
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
        services.AddScoped<IWebhookDispatcher, WebhookDispatcherService>();

        // Register HttpClient for webhook delivery
        services.AddHttpClient("Webhook", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "SuperEcomManager-Webhook/1.0");
        });

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

        // Configure authorization policies
        services.AddAuthorization(options =>
        {
            // Policy for platform admins
            options.AddPolicy("PlatformAdmin", policy =>
                policy.RequireRole("PlatformAdmin", "SuperAdmin"));

            // Policy for super admins only
            options.AddPolicy("SuperAdmin", policy =>
                policy.RequireRole("SuperAdmin"));

            // Policy for tenant users (requires tenant context)
            options.AddPolicy("TenantUser", policy =>
                policy.RequireClaim("tenant_id"));
        });

        // Register authentication services
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Register background job services
        services.Configure<OrderSyncSettings>(configuration.GetSection(OrderSyncSettings.SectionName));
        services.AddScoped<OrderSyncJob>();
        services.AddHostedService<OrderSyncHostedService>();

        // Webhook retry job
        services.Configure<WebhookRetrySettings>(configuration.GetSection(WebhookRetrySettings.SectionName));
        services.AddScoped<WebhookRetryJob>();
        services.AddHostedService<WebhookRetryHostedService>();

        // Notification sender job
        services.Configure<NotificationSenderSettings>(configuration.GetSection(NotificationSenderSettings.SectionName));
        services.AddScoped<NotificationSenderJob>();
        services.AddHostedService<NotificationSenderHostedService>();

        // Data cleanup job
        services.Configure<DataCleanupSettings>(configuration.GetSection(DataCleanupSettings.SectionName));
        services.AddScoped<DataCleanupJob>();
        services.AddHostedService<DataCleanupHostedService>();

        // Inventory sync job
        services.Configure<InventorySyncSettings>(configuration.GetSection(InventorySyncSettings.SectionName));
        services.AddScoped<InventorySyncJob>();
        services.AddHostedService<InventorySyncHostedService>();

        // NDR follow-up job
        services.Configure<NdrFollowUpSettings>(configuration.GetSection(NdrFollowUpSettings.SectionName));
        services.AddScoped<NdrFollowUpJob>();
        services.AddHostedService<NdrFollowUpHostedService>();

        // Stock alert job
        services.Configure<StockAlertSettings>(configuration.GetSection(StockAlertSettings.SectionName));
        services.AddScoped<StockAlertJob>();
        services.AddHostedService<StockAlertHostedService>();

        // Shipment tracking update job
        services.Configure<ShipmentTrackingSettings>(configuration.GetSection(ShipmentTrackingSettings.SectionName));
        services.AddScoped<ShipmentTrackingUpdateJob>();
        services.AddHostedService<ShipmentTrackingHostedService>();

        // Rate limiting and API usage tracking
        services.AddRateLimitingServices(configuration);
        services.AddScoped<IApiUsageTracker, ApiUsageTracker>();

        // Database seeders and migration service
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<DevelopmentSeeder>();
        services.AddScoped<TenantSeeder>();
        services.AddScoped<ITenantSeeder>(sp => sp.GetRequiredService<TenantSeeder>());
        services.AddScoped<IMigrationService, MigrationService>();

        return services;
    }
}
