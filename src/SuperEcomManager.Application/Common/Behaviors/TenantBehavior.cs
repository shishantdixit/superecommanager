using MediatR;
using SuperEcomManager.Application.Common.Exceptions;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that ensures tenant context is available.
/// </summary>
public class TenantBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentTenantService _currentTenantService;

    public TenantBehavior(ICurrentTenantService currentTenantService)
    {
        _currentTenantService = currentTenantService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only enforce tenant context for requests that require it
        if (request is ITenantRequest && !_currentTenantService.HasTenant)
        {
            throw new TenantNotFoundException();
        }

        return await next();
    }
}
