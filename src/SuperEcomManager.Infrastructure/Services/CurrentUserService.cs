using Microsoft.AspNetCore.Http;
using SuperEcomManager.Application.Common.Interfaces;
using System.Security.Claims;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService.
/// Extracts user information from HttpContext.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User?
        .FindAll(ClaimTypes.Role)
        .Select(c => c.Value) ?? Enumerable.Empty<string>();

    public IEnumerable<string> Permissions => _httpContextAccessor.HttpContext?.User?
        .FindAll("permission")
        .Select(c => c.Value) ?? Enumerable.Empty<string>();

    public bool HasPermission(string permissionCode)
    {
        return Permissions.Contains(permissionCode);
    }

    public bool HasAnyPermission(params string[] permissionCodes)
    {
        return permissionCodes.Any(p => Permissions.Contains(p));
    }

    public bool HasAllPermissions(params string[] permissionCodes)
    {
        return permissionCodes.All(p => Permissions.Contains(p));
    }
}
