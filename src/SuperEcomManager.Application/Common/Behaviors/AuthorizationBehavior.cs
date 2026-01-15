using MediatR;
using SuperEcomManager.Application.Common.Exceptions;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Application.Common.Behaviors;

/// <summary>
/// Attribute to specify required permission for a request.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// Attribute to specify required feature for a request.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequireFeatureAttribute : Attribute
{
    public string FeatureCode { get; }

    public RequireFeatureAttribute(string featureCode)
    {
        FeatureCode = featureCode;
    }
}

/// <summary>
/// Pipeline behavior that checks authorization and feature flags.
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IFeatureFlagService _featureFlagService;

    public AuthorizationBehavior(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IFeatureFlagService featureFlagService)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _featureFlagService = featureFlagService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);

        // Check feature flags
        var featureAttribute = requestType
            .GetCustomAttributes(typeof(RequireFeatureAttribute), true)
            .FirstOrDefault() as RequireFeatureAttribute;

        if (featureAttribute != null)
        {
            var isEnabled = await _featureFlagService.IsEnabledAsync(featureAttribute.FeatureCode);
            if (!isEnabled)
            {
                throw new FeatureDisabledException(featureAttribute.FeatureCode);
            }
        }

        // Check permissions
        var permissionAttributes = requestType
            .GetCustomAttributes(typeof(RequirePermissionAttribute), true)
            .Cast<RequirePermissionAttribute>()
            .ToList();

        if (permissionAttributes.Any())
        {
            if (!_currentUserService.UserId.HasValue)
            {
                throw new UnauthorizedException();
            }

            foreach (var attr in permissionAttributes)
            {
                var hasPermission = await _permissionService.HasPermissionAsync(
                    _currentUserService.UserId.Value,
                    attr.Permission);

                if (!hasPermission)
                {
                    throw new ForbiddenAccessException("Resource", attr.Permission);
                }
            }
        }

        return await next();
    }
}
