using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Exceptions;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Infrastructure.Persistence;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Implementation of IPermissionService.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly TenantDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;

    public PermissionService(
        TenantDbContext context,
        ICacheService cacheService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _cacheService = cacheService;
        _currentUserService = currentUserService;
    }

    public async Task<bool> HasPermissionAsync(string permissionCode)
    {
        if (!_currentUserService.UserId.HasValue)
            return false;

        return await HasPermissionAsync(_currentUserService.UserId.Value, permissionCode);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permissionCode);
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId)
    {
        var cacheKey = $"permissions:user:{userId}";

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var permissions = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Code)
                    .Distinct()
                    .ToListAsync();

                return permissions.AsEnumerable();
            },
            TimeSpan.FromMinutes(15));
    }

    public async Task<IEnumerable<string>> GetRolePermissionsAsync(Guid roleId)
    {
        var cacheKey = $"permissions:role:{roleId}";

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var permissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.Permission.Code)
                    .ToListAsync();

                return permissions.AsEnumerable();
            },
            TimeSpan.FromMinutes(30));
    }

    public async Task AuthorizeAsync(string permissionCode)
    {
        var hasPermission = await HasPermissionAsync(permissionCode);

        if (!hasPermission)
        {
            throw new ForbiddenAccessException($"User does not have permission: {permissionCode}");
        }
    }

    public async Task AuthorizeAnyAsync(params string[] permissionCodes)
    {
        foreach (var permissionCode in permissionCodes)
        {
            if (await HasPermissionAsync(permissionCode))
                return;
        }

        throw new ForbiddenAccessException($"User does not have any of the required permissions: {string.Join(", ", permissionCodes)}");
    }
}
