using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers;
using SuperEcomManager.Integrations.Couriers.Shiprocket;
using SuperEcomManager.Integrations.Couriers.Delhivery;
using SuperEcomManager.Integrations.Couriers.BlueDart;
using SuperEcomManager.Integrations.Couriers.DTDC;
using SuperEcomManager.Integrations.Amazon;
using SuperEcomManager.Integrations.Common;
using SuperEcomManager.Integrations.Flipkart;
using SuperEcomManager.Integrations.Meesho;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Integrations.Shopify;
using SuperEcomManager.Integrations.Shopify.Services;
using SuperEcomManager.Integrations.Shopify.Webhooks;

namespace SuperEcomManager.Integrations;

/// <summary>
/// Dependency injection configuration for the Integrations layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Shopify settings
        services.Configure<ShopifySettings>(configuration.GetSection(ShopifySettings.SectionName));

        // Register Shopify HTTP client with retry policy
        // Increased timeout to 120 seconds to handle large product/inventory syncs
        services.AddHttpClient<IShopifyClient, ShopifyClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register Shopify services
        services.AddScoped<ShopifyOrderMapper>();
        services.AddScoped<IShopifyOrderSyncService, ShopifyOrderSyncService>();
        services.AddScoped<IShopifyWebhookHandler, ShopifyWebhookHandler>();

        // Register channel sync services
        services.AddScoped<IChannelSyncService, ShopifyChannelSyncService>();
        services.AddScoped<IChannelSyncServiceFactory, ChannelSyncServiceFactory>();

        // Register order creation services for external platforms
        services.AddScoped<IOrderCreationService, ShopifyOrderCreationService>();
        services.AddScoped<IOrderCreationServiceFactory, Services.OrderCreationServiceFactory>();

        // Register order update services for syncing order changes to external platforms
        services.AddScoped<IOrderUpdateService, ShopifyOrderUpdateService>();
        services.AddScoped<IOrderUpdateServiceFactory, Services.OrderUpdateServiceFactory>();

        // Register inventory sync services for pushing inventory to external platforms
        services.AddScoped<IInventorySyncService, ShopifyInventorySyncService>();
        services.AddScoped<IInventorySyncServiceFactory, Services.InventorySyncServiceFactory>();

        // Register Shiprocket settings
        services.Configure<ShiprocketSettings>(configuration.GetSection(ShiprocketSettings.SectionName));

        // Register Shiprocket HTTP client
        services.AddHttpClient<IShiprocketClient, ShiprocketClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register Shiprocket services
        services.AddScoped<ShiprocketAdapter>();
        services.AddScoped<IShiprocketWebhookHandler, ShiprocketWebhookHandler>();

        // Register Delhivery settings
        services.Configure<DelhiverySettings>(configuration.GetSection(DelhiverySettings.SectionName));

        // Register Delhivery HTTP client
        services.AddHttpClient<IDelhiveryClient, DelhiveryClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register Delhivery services
        services.AddScoped<DelhiveryAdapter>();
        services.AddScoped<IDelhiveryWebhookHandler, DelhiveryWebhookHandler>();

        // Register BlueDart settings
        services.Configure<BlueDartSettings>(configuration.GetSection(BlueDartSettings.SectionName));

        // Register BlueDart HTTP client
        services.AddHttpClient<IBlueDartClient, BlueDartClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register BlueDart services
        services.AddScoped<BlueDartAdapter>();
        services.AddScoped<IBlueDartWebhookHandler, BlueDartWebhookHandler>();

        // Register DTDC settings
        services.Configure<DTDCSettings>(configuration.GetSection(DTDCSettings.SectionName));

        // Register DTDC HTTP client
        services.AddHttpClient<IDTDCClient, DTDCClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register DTDC services
        services.AddScoped<DTDCAdapter>();
        services.AddScoped<IDTDCWebhookHandler, DTDCWebhookHandler>();

        // Register courier adapter factory with all adapters
        services.AddCourierAdapters()
            .AddAdapter<ShiprocketAdapter>(CourierType.Shiprocket)
            .AddAdapter<DelhiveryAdapter>(CourierType.Delhivery)
            .AddAdapter<BlueDartAdapter>(CourierType.BlueDart)
            .AddAdapter<DTDCAdapter>(CourierType.DTDC)
            .Build();

        // Register Amazon settings and adapter
        services.Configure<AmazonSettings>(configuration.GetSection(AmazonSettings.SectionName));
        services.AddHttpClient("Amazon", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register Flipkart settings and adapter
        services.Configure<FlipkartSettings>(configuration.GetSection(FlipkartSettings.SectionName));
        services.AddHttpClient("Flipkart", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register Meesho settings and adapter
        services.Configure<MeeshoSettings>(configuration.GetSection(MeeshoSettings.SectionName));
        services.AddHttpClient("Meesho", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register channel adapter factory with all adapters
        services.AddChannelAdapters()
            .AddAdapter<AmazonChannelAdapter>(ChannelType.Amazon)
            .AddAdapter<FlipkartChannelAdapter>(ChannelType.Flipkart)
            .AddAdapter<MeeshoChannelAdapter>(ChannelType.Meesho)
            .BuildChannelAdapters();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
