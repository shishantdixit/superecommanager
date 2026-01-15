# Feature Flags & RBAC

## Table of Contents
1. [Overview](#overview)
2. [Feature Flag System](#feature-flag-system)
3. [RBAC System](#rbac-system)
4. [Implementation Details](#implementation-details)
5. [API Enforcement](#api-enforcement)
6. [Frontend Integration](#frontend-integration)

---

## Overview

SuperEcomManager implements a dual-layer access control system:

1. **Feature Flags** - Subscription-based feature access (what the tenant can do)
2. **RBAC** - Role-based permissions (what a user within tenant can do)

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                         ACCESS CONTROL FLOW                                          │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   REQUEST                                                                            │
│      │                                                                               │
│      ▼                                                                               │
│   ┌─────────────────┐                                                               │
│   │ Authentication  │──── Is user authenticated?                                    │
│   └────────┬────────┘                                                               │
│            │ YES                                                                     │
│            ▼                                                                         │
│   ┌─────────────────┐                                                               │
│   │ Tenant Context  │──── Does user belong to this tenant?                          │
│   └────────┬────────┘                                                               │
│            │ YES                                                                     │
│            ▼                                                                         │
│   ┌─────────────────┐                                                               │
│   │  Feature Flag   │──── Is feature enabled for tenant subscription?               │
│   │    (Layer 1)    │     + Any tenant-level overrides?                             │
│   └────────┬────────┘     + Any user-level overrides?                               │
│            │ YES                                                                     │
│            ▼                                                                         │
│   ┌─────────────────┐                                                               │
│   │   Permission    │──── Does user have required permission?                       │
│   │    (Layer 2)    │     (via assigned roles)                                      │
│   └────────┬────────┘                                                               │
│            │ YES                                                                     │
│            ▼                                                                         │
│   ┌─────────────────┐                                                               │
│   │  EXECUTE API    │                                                               │
│   └─────────────────┘                                                               │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Feature Flag System

### Feature Categories

| Category | Features |
|----------|----------|
| **Orders** | `orders_management`, `bulk_operations`, `manual_orders` |
| **Channels** | `multi_channel`, `shopify`, `amazon`, `flipkart`, `meesho`, `woocommerce` |
| **Shipments** | `shipment_management`, `custom_couriers`, `bulk_shipments` |
| **NDR** | `ndr_management`, `ndr_automation`, `ndr_analytics` |
| **Inventory** | `inventory_management`, `inventory_sync`, `low_stock_alerts` |
| **Finance** | `finance_reports`, `expense_tracking`, `pl_analytics` |
| **Analytics** | `basic_analytics`, `advanced_analytics`, `custom_reports` |
| **Integrations** | `api_access`, `webhooks`, `custom_integrations` |
| **Security** | `audit_logs`, `data_export`, `api_keys` |
| **Branding** | `white_label`, `custom_domain` |

### Feature Resolution Priority

```
1. User-level override      (highest priority)
   ↓
2. Tenant-level override
   ↓
3. Subscription plan feature
   ↓
4. Default: DISABLED        (lowest priority)
```

### Sample Plans & Features Matrix

| Feature | Starter | Professional | Business | Enterprise |
|---------|:-------:|:------------:|:--------:|:----------:|
| orders_management | ✅ | ✅ | ✅ | ✅ |
| multi_channel | ❌ | ✅ | ✅ | ✅ |
| shipment_management | ✅ | ✅ | ✅ | ✅ |
| ndr_management | ❌ | ✅ | ✅ | ✅ |
| ndr_automation | ❌ | ❌ | ✅ | ✅ |
| inventory_sync | ❌ | ✅ | ✅ | ✅ |
| finance_reports | ❌ | ✅ | ✅ | ✅ |
| advanced_analytics | ❌ | ❌ | ✅ | ✅ |
| api_access | ❌ | ❌ | ✅ | ✅ |
| custom_couriers | ❌ | ❌ | ✅ | ✅ |
| white_label | ❌ | ❌ | ❌ | ✅ |
| **Max Users** | 2 | 5 | 15 | Unlimited |
| **Max Orders/Month** | 500 | 2,000 | 10,000 | Unlimited |
| **Max Channels** | 1 | 3 | 5 | Unlimited |

### Feature with Configuration

Some features have configurable limits:

```json
{
  "feature_code": "ndr_management",
  "is_enabled": true,
  "config": {
    "max_ndr_actions_per_day": 100,
    "auto_assignment_enabled": true,
    "sms_notifications_enabled": false,
    "whatsapp_notifications_enabled": true
  }
}
```

---

## RBAC System

### System Roles (Built-in)

| Role | Description | Typical Permissions |
|------|-------------|---------------------|
| **Owner** | Tenant owner | All permissions (`*`) |
| **Admin** | Administrator | All except billing/subscription |
| **Manager** | Team manager | Team management + all operations |
| **Operator** | Operations staff | Day-to-day operations |
| **NDR Agent** | NDR handling | NDR view, action, remark |
| **Viewer** | Read-only access | View-only permissions |

### Permission Modules

```
permissions
├── orders
│   ├── orders.view
│   ├── orders.create
│   ├── orders.edit
│   ├── orders.cancel
│   └── orders.export
│
├── shipments
│   ├── shipments.view
│   ├── shipments.create
│   ├── shipments.cancel
│   └── shipments.track
│
├── ndr
│   ├── ndr.view
│   ├── ndr.assign
│   ├── ndr.action
│   ├── ndr.reattempt
│   └── ndr.close
│
├── inventory
│   ├── inventory.view
│   ├── inventory.adjust
│   └── inventory.sync
│
├── channels
│   ├── channels.view
│   ├── channels.connect
│   ├── channels.disconnect
│   └── channels.configure
│
├── team
│   ├── team.view
│   ├── team.invite
│   ├── team.edit
│   ├── team.delete
│   └── team.roles
│
├── finance
│   ├── finance.view
│   ├── finance.create
│   └── finance.export
│
├── settings
│   ├── settings.view
│   └── settings.edit
│
├── analytics
│   ├── analytics.view
│   └── analytics.export
│
├── security                          # NEW: Security & Data Protection Module
│   ├── security.view                 # View security settings & audit logs
│   ├── security.configure            # Configure security policies
│   ├── security.audit_logs           # Access detailed audit logs
│   ├── security.export_approve       # Approve large export requests
│   ├── security.sessions             # View/manage user sessions
│   └── security.force_logout         # Force logout users
│
├── data_access                       # NEW: Data Access Controls
│   ├── data.view_masked              # View masked sensitive data (default)
│   ├── data.view_full                # View full unmasked data
│   ├── data.copy                     # Can copy data from UI
│   └── data.print                    # Can print pages
│
└── export                            # NEW: Export Permissions (Granular)
    ├── export.orders_csv             # Export orders as CSV
    ├── export.orders_excel           # Export orders as Excel
    ├── export.customers              # Export customer data
    ├── export.financial              # Export financial data
    ├── export.ndr                    # Export NDR records
    ├── export.inventory              # Export inventory data
    ├── export.analytics              # Export analytics reports
    └── export.bulk_api               # Access bulk export API
```

### Role-Permission Matrix Example

| Permission | Owner | Admin | Manager | Operator | NDR Agent | Viewer |
|------------|:-----:|:-----:|:-------:|:--------:|:---------:|:------:|
| orders.view | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| orders.create | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| orders.cancel | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| ndr.view | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ndr.action | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| ndr.assign | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| team.invite | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| team.roles | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| settings.edit | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |

### Security & Export Permission Matrix

> **Note**: These permissions control data protection features and are configurable by Tenant Admin.

| Permission | Owner | Admin | Manager | Operator | NDR Agent | Viewer |
|------------|:-----:|:-----:|:-------:|:--------:|:---------:|:------:|
| **Security Module** |
| security.view | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| security.configure | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| security.audit_logs | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| security.export_approve | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| security.sessions | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| security.force_logout | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Data Access** |
| data.view_masked | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| data.view_full | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| data.copy | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| data.print | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Export Permissions** |
| export.orders_csv | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| export.orders_excel | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| export.customers | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| export.financial | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| export.ndr | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| export.inventory | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| export.analytics | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| export.bulk_api | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

---

## Implementation Details

### Backend: Feature Flag Service

```csharp
// Application/Common/Interfaces/IFeatureFlagService.cs
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureCode, CancellationToken ct = default);
    Task<bool> IsEnabledForUserAsync(string featureCode, Guid userId, CancellationToken ct = default);
    Task<FeatureFlag?> GetFeatureAsync(string featureCode, CancellationToken ct = default);
    Task<IReadOnlyList<FeatureFlag>> GetAllFeaturesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FeatureFlag>> GetUserFeaturesAsync(Guid userId, CancellationToken ct = default);
}

// Infrastructure/FeatureManagement/FeatureFlagService.cs
public class FeatureFlagService : IFeatureFlagService
{
    private readonly ICurrentTenantService _tenantService;
    private readonly IDistributedCache _cache;
    private readonly ApplicationDbContext _appContext;
    private readonly ITenantDbContext _tenantContext;
    private readonly ILogger<FeatureFlagService> _logger;

    public async Task<bool> IsEnabledAsync(string featureCode, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId;
        var cacheKey = $"feature:{tenantId}:{featureCode}";

        // Try cache first
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
            return bool.Parse(cached);

        // Check tenant override first (highest priority for tenant level)
        var tenantOverride = await _appContext.TenantFeatures
            .Where(tf => tf.TenantId == tenantId)
            .Where(tf => tf.Feature.Code == featureCode)
            .Select(tf => (bool?)tf.IsEnabled)
            .FirstOrDefaultAsync(ct);

        if (tenantOverride.HasValue)
        {
            await CacheResultAsync(cacheKey, tenantOverride.Value, ct);
            return tenantOverride.Value;
        }

        // Check subscription plan features
        var subscription = await _appContext.Subscriptions
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
            .Where(s => s.TenantId == tenantId)
            .Where(s => s.Status == "active" || s.Status == "trial")
            .FirstOrDefaultAsync(ct);

        if (subscription == null)
        {
            _logger.LogWarning("No active subscription for tenant {TenantId}", tenantId);
            return false;
        }

        var planFeature = subscription.Plan.PlanFeatures
            .FirstOrDefault(pf => pf.Feature.Code == featureCode);

        var result = planFeature?.IsEnabled ?? false;
        await CacheResultAsync(cacheKey, result, ct);
        return result;
    }

    public async Task<bool> IsEnabledForUserAsync(string featureCode, Guid userId, CancellationToken ct = default)
    {
        // First check tenant-level
        var tenantEnabled = await IsEnabledAsync(featureCode, ct);
        if (!tenantEnabled)
            return false;

        // Then check user-level override
        var userOverride = await _tenantContext.UserFeatures
            .Where(uf => uf.UserId == userId && uf.FeatureCode == featureCode)
            .Select(uf => (bool?)uf.IsEnabled)
            .FirstOrDefaultAsync(ct);

        return userOverride ?? tenantEnabled;
    }

    public async Task<FeatureFlag?> GetFeatureAsync(string featureCode, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId;

        // Get plan feature with config
        var subscription = await _appContext.Subscriptions
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
            .Where(s => s.TenantId == tenantId && s.Status == "active")
            .FirstOrDefaultAsync(ct);

        var planFeature = subscription?.Plan.PlanFeatures
            .FirstOrDefault(pf => pf.Feature.Code == featureCode);

        if (planFeature == null)
            return null;

        // Check for tenant override
        var tenantOverride = await _appContext.TenantFeatures
            .Where(tf => tf.TenantId == tenantId && tf.Feature.Code == featureCode)
            .FirstOrDefaultAsync(ct);

        return new FeatureFlag
        {
            Code = featureCode,
            IsEnabled = tenantOverride?.IsEnabled ?? planFeature.IsEnabled,
            Config = tenantOverride?.Config ?? planFeature.Config
        };
    }

    private async Task CacheResultAsync(string key, bool value, CancellationToken ct)
    {
        await _cache.SetStringAsync(key, value.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, ct);
    }
}
```

### Backend: Permission Service

```csharp
// Application/Common/Interfaces/IPermissionService.cs
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);
}

// Infrastructure/Identity/PermissionService.cs
public class PermissionService : IPermissionService
{
    private readonly ITenantDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PermissionService> _logger;

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken ct = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, ct);

        // Check for wildcard (owner has all permissions)
        if (permissions.Contains("*"))
            return true;

        // Check for module wildcard (e.g., "orders.*")
        var module = permissionCode.Split('.')[0];
        if (permissions.Contains($"{module}.*"))
            return true;

        return permissions.Contains(permissionCode);
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = $"user_permissions:{userId}";

        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<List<string>>(cached)!;

        // Get user with roles
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            return Array.Empty<string>();

        var permissions = new List<string>();

        // Owner has all permissions
        if (user.IsOwner)
        {
            permissions.Add("*");
        }
        else
        {
            // Collect permissions from all roles
            permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToList();
        }

        // Cache for 10 minutes
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(permissions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            },
            ct);

        return permissions;
    }

    public async Task InvalidateUserPermissionsCache(Guid userId)
    {
        var cacheKey = $"user_permissions:{userId}";
        await _cache.RemoveAsync(cacheKey);
    }
}
```

---

## API Enforcement

### Feature Attribute

```csharp
// API/Filters/RequireFeatureAttribute.cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireFeatureAttribute : TypeFilterAttribute
{
    public RequireFeatureAttribute(string featureCode) : base(typeof(FeatureAuthorizationFilter))
    {
        Arguments = new object[] { featureCode };
    }
}

public class FeatureAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly string _featureCode;
    private readonly IFeatureFlagService _featureService;
    private readonly ICurrentUserService _userService;

    public FeatureAuthorizationFilter(
        string featureCode,
        IFeatureFlagService featureService,
        ICurrentUserService userService)
    {
        _featureCode = featureCode;
        _featureService = featureService;
        _userService = userService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var userId = _userService.UserId;
        var isEnabled = await _featureService.IsEnabledForUserAsync(_featureCode, userId);

        if (!isEnabled)
        {
            context.Result = new ObjectResult(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "FEATURE_NOT_ENABLED",
                    Message = $"The feature '{_featureCode}' is not available in your current plan."
                }
            })
            {
                StatusCode = 403
            };
        }
    }
}
```

### Permission Attribute

```csharp
// API/Filters/RequirePermissionAttribute.cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(params string[] permissions) : base(typeof(PermissionAuthorizationFilter))
    {
        Arguments = new object[] { permissions };
    }
}

public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly string[] _requiredPermissions;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _userService;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var userId = _userService.UserId;

        foreach (var permission in _requiredPermissions)
        {
            var hasPermission = await _permissionService.HasPermissionAsync(userId, permission);

            if (!hasPermission)
            {
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "ACCESS_DENIED",
                        Message = $"You don't have permission to perform this action. Required: {permission}"
                    }
                })
                {
                    StatusCode = 403
                };
                return;
            }
        }
    }
}
```

### Controller Usage

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NdrController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet]
    [RequireFeature("ndr_management")]
    [RequirePermission("ndr.view")]
    public async Task<IActionResult> GetNdrInbox([FromQuery] GetNdrInboxQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(ApiResponse.Success(result));
    }

    [HttpPost("{id}/assign")]
    [RequireFeature("ndr_management")]
    [RequirePermission("ndr.assign")]
    [AuditLog("NDR assigned")]
    public async Task<IActionResult> AssignNdr(Guid id, [FromBody] AssignNdrCommand command)
    {
        command.NdrId = id;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse.Success(result));
    }

    [HttpPost("{id}/action")]
    [RequireFeature("ndr_management")]
    [RequirePermission("ndr.action")]
    [AuditLog("NDR action performed")]
    public async Task<IActionResult> AddNdrAction(Guid id, [FromBody] AddNdrActionCommand command)
    {
        command.NdrId = id;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse.Success(result));
    }

    [HttpPost("{id}/reattempt")]
    [RequireFeature("ndr_management")]
    [RequirePermission("ndr.reattempt")]
    public async Task<IActionResult> ScheduleReattempt(Guid id, [FromBody] ScheduleReattemptCommand command)
    {
        command.NdrId = id;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse.Success(result));
    }

    [HttpGet("analytics")]
    [RequireFeature("ndr_analytics")]
    [RequirePermission("analytics.view")]
    public async Task<IActionResult> GetNdrAnalytics([FromQuery] GetNdrAnalyticsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(ApiResponse.Success(result));
    }
}
```

---

## Frontend Integration

### Feature Hook

```tsx
// hooks/use-features.ts
'use client';

import { useQuery } from '@tanstack/react-query';
import { featuresService } from '@/services/features.service';

export function useFeatures() {
  const { data: features = [], isLoading } = useQuery({
    queryKey: ['features'],
    queryFn: () => featuresService.getMyFeatures(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  const isEnabled = (featureCode: string): boolean => {
    const feature = features.find((f) => f.code === featureCode);
    return feature?.isEnabled ?? false;
  };

  const getConfig = <T = Record<string, unknown>>(featureCode: string): T | null => {
    const feature = features.find((f) => f.code === featureCode);
    return (feature?.config as T) ?? null;
  };

  return {
    features,
    isEnabled,
    getConfig,
    isLoading,
  };
}
```

### Permission Hook

```tsx
// hooks/use-permissions.ts
'use client';

import { useQuery } from '@tanstack/react-query';
import { authService } from '@/services/auth.service';

export function usePermissions() {
  const { data: permissions = [], isLoading } = useQuery({
    queryKey: ['permissions'],
    queryFn: () => authService.getMyPermissions(),
    staleTime: 10 * 60 * 1000, // 10 minutes
  });

  const hasPermission = (permissionCode: string): boolean => {
    // Wildcard check
    if (permissions.includes('*')) return true;

    // Module wildcard check
    const module = permissionCode.split('.')[0];
    if (permissions.includes(`${module}.*`)) return true;

    return permissions.includes(permissionCode);
  };

  const hasAnyPermission = (...codes: string[]): boolean => {
    return codes.some((code) => hasPermission(code));
  };

  const hasAllPermissions = (...codes: string[]): boolean => {
    return codes.every((code) => hasPermission(code));
  };

  return {
    permissions,
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    isLoading,
  };
}
```

### Feature Gate Component

```tsx
// components/shared/feature-gate.tsx
'use client';

import { ReactNode } from 'react';
import { useFeatures } from '@/hooks/use-features';
import { UpgradePlanCard } from './upgrade-plan-card';

interface FeatureGateProps {
  feature: string;
  children: ReactNode;
  fallback?: ReactNode;
  showUpgrade?: boolean;
}

export function FeatureGate({
  feature,
  children,
  fallback,
  showUpgrade = true,
}: FeatureGateProps) {
  const { isEnabled, isLoading } = useFeatures();

  if (isLoading) {
    return <div className="animate-pulse h-32 bg-gray-100 rounded-lg" />;
  }

  if (!isEnabled(feature)) {
    if (fallback) return <>{fallback}</>;
    if (showUpgrade) return <UpgradePlanCard feature={feature} />;
    return null;
  }

  return <>{children}</>;
}
```

### Permission Gate Component

```tsx
// components/shared/permission-gate.tsx
'use client';

import { ReactNode } from 'react';
import { usePermissions } from '@/hooks/use-permissions';

interface PermissionGateProps {
  permission: string | string[];
  requireAll?: boolean;
  children: ReactNode;
  fallback?: ReactNode;
}

export function PermissionGate({
  permission,
  requireAll = false,
  children,
  fallback = null,
}: PermissionGateProps) {
  const { hasPermission, hasAnyPermission, hasAllPermissions, isLoading } = usePermissions();

  if (isLoading) return null;

  const permissions = Array.isArray(permission) ? permission : [permission];

  const hasAccess = requireAll
    ? hasAllPermissions(...permissions)
    : hasAnyPermission(...permissions);

  if (!hasAccess) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}
```

### Combined Usage Example

```tsx
// Example: NDR page with feature and permission gates
export default function NdrPage() {
  return (
    <FeatureGate feature="ndr_management">
      <div className="space-y-6">
        <PageHeader
          title="NDR Management"
          actions={
            <PermissionGate permission="ndr.assign">
              <BulkAssignButton />
            </PermissionGate>
          }
        />

        <NdrFilters />

        <NdrInbox />

        <PermissionGate permission="analytics.view">
          <FeatureGate feature="ndr_analytics">
            <NdrAnalyticsSummary />
          </FeatureGate>
        </PermissionGate>
      </div>
    </FeatureGate>
  );
}
```

### Sidebar Navigation with Gates

```tsx
// components/layout/sidebar/sidebar-nav.tsx
const navItems = [
  { href: '/', label: 'Dashboard', icon: Home },
  { href: '/orders', label: 'Orders', icon: ShoppingCart, permission: 'orders.view' },
  { href: '/shipments', label: 'Shipments', icon: Truck, permission: 'shipments.view' },
  { href: '/ndr', label: 'NDR', icon: AlertCircle, feature: 'ndr_management', permission: 'ndr.view' },
  { href: '/inventory', label: 'Inventory', icon: Package, feature: 'inventory_management', permission: 'inventory.view' },
  { href: '/channels', label: 'Channels', icon: Store, permission: 'channels.view' },
  { href: '/finance', label: 'Finance', icon: DollarSign, feature: 'finance_reports', permission: 'finance.view' },
  { href: '/analytics', label: 'Analytics', icon: BarChart, permission: 'analytics.view' },
  { href: '/team', label: 'Team', icon: Users, permission: 'team.view' },
  { href: '/settings', label: 'Settings', icon: Settings, permission: 'settings.view' },
];

export function SidebarNav() {
  const { isEnabled } = useFeatures();
  const { hasPermission } = usePermissions();

  const visibleItems = navItems.filter((item) => {
    if (item.feature && !isEnabled(item.feature)) return false;
    if (item.permission && !hasPermission(item.permission)) return false;
    return true;
  });

  return (
    <nav className="space-y-1">
      {visibleItems.map((item) => (
        <SidebarItem key={item.href} item={item} />
      ))}
    </nav>
  );
}
```

---

## Next Steps

See the following documents for more details:
- [Multi-Platform Integration](06-multi-platform-integration.md)
- [NDR Workflow](07-ndr-workflow.md)
- [API Design](08-api-design.md)
