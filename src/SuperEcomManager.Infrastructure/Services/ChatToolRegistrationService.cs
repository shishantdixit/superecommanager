using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Hosted service that registers chat tool providers at application startup.
/// </summary>
public class ChatToolRegistrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IChatToolRegistry _toolRegistry;
    private readonly ILogger<ChatToolRegistrationService> _logger;

    public ChatToolRegistrationService(
        IServiceProvider serviceProvider,
        IChatToolRegistry toolRegistry,
        ILogger<ChatToolRegistrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering chat tool providers...");

        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var providers = scope.ServiceProvider.GetServices<IChatToolProvider>();

        foreach (var provider in providers)
        {
            _toolRegistry.RegisterProvider(provider);
        }

        var totalTools = _toolRegistry.GetAllTools().Count;
        _logger.LogInformation("Chat tool registration complete. Total tools: {TotalTools}", totalTools);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
