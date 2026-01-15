using MediatR;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using System.Diagnostics;

namespace SuperEcomManager.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that logs slow-running requests.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;

    private const int SlowRequestThresholdMs = 500;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService)
    {
        _timer = new Stopwatch();
        _logger = logger;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > SlowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId;
            var tenantId = _currentTenantService.TenantId;

            _logger.LogWarning(
                "Long Running Request: {RequestName} ({ElapsedMilliseconds}ms) " +
                "Tenant: {TenantId} User: {UserId} Request: {@Request}",
                requestName, elapsedMilliseconds, tenantId, userId, request);
        }

        return response;
    }
}
