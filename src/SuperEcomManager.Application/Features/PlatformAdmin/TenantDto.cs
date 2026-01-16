using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

/// <summary>
/// DTO for tenant information in admin context.
/// </summary>
public class TenantAdminDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public string StatusDisplay => Status.ToString();
    public DateTime? TrialEndsAt { get; set; }
    public bool IsTrialActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Subscription info
    public string? CurrentPlan { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }

    // Stats
    public int UserCount { get; set; }
    public int OrderCount { get; set; }
    public int ChannelCount { get; set; }
}

/// <summary>
/// Summary DTO for tenant listing.
/// </summary>
public class TenantSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public TenantStatus Status { get; set; }
    public string? CurrentPlan { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
}

/// <summary>
/// DTO for platform admin.
/// </summary>
public class PlatformAdminDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSuperAdmin { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for tenant activity log.
/// </summary>
public class TenantActivityLogDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid PerformedBy { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime PerformedAt { get; set; }
}

/// <summary>
/// Statistics for platform dashboard.
/// </summary>
public class PlatformStatsDto
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TrialTenants { get; set; }
    public int SuspendedTenants { get; set; }
    public int TenantsThisMonth { get; set; }
    public int TotalUsers { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, int> TenantsByPlan { get; set; } = new();
    public List<TenantGrowthDto> GrowthData { get; set; } = new();
}

/// <summary>
/// Tenant growth data point.
/// </summary>
public class TenantGrowthDto
{
    public DateTime Date { get; set; }
    public int NewTenants { get; set; }
    public int ChurnedTenants { get; set; }
    public int TotalActive { get; set; }
}
