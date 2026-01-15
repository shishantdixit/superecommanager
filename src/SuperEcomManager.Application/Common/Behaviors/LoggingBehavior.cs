using MediatR;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using System.Diagnostics;

namespace SuperEcomManager.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that logs request execution.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId;
        var tenantId = _currentTenantService.TenantId;

        _logger.LogInformation(
            "Handling {RequestName} for Tenant {TenantId} by User {UserId}",
            requestName, tenantId, userId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Error handling {RequestName} after {ElapsedMilliseconds}ms: {ErrorMessage}",
                requestName, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }
}
