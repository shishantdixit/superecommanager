using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Tenants;

/// <summary>
/// Represents a tenant (organization/company) in the multi-tenant system.
/// This entity is stored in the shared/public schema.
/// </summary>
public class Tenant : AuditableEntity, ISoftDeletable
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? CompanyName { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? Website { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Address { get; private set; }
    public string? GstNumber { get; private set; }
    public string SchemaName { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private Tenant() { } // EF Core constructor

    public static Tenant Create(
        string name,
        string slug,
        string? contactEmail = null,
        int trialDays = 14)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tenant slug cannot be empty", nameof(slug));

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            SchemaName = $"tenant_{slug.Trim().ToLowerInvariant().Replace("-", "_")}",
            ContactEmail = contactEmail,
            Status = TenantStatus.Pending,
            TrialEndsAt = trialDays > 0 ? DateTime.UtcNow.AddDays(trialDays) : null,
            CreatedAt = DateTime.UtcNow
        };

        return tenant;
    }

    public void Activate()
    {
        if (Status == TenantStatus.Deactivated)
            throw new InvalidOperationException("Cannot activate a deactivated tenant");

        Status = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend(string reason)
    {
        if (Status == TenantStatus.Deactivated)
            throw new InvalidOperationException("Cannot suspend a deactivated tenant");

        Status = TenantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = TenantStatus.Deactivated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(
        string? companyName,
        string? logoUrl,
        string? website,
        string? contactEmail,
        string? contactPhone,
        string? address,
        string? gstNumber)
    {
        CompanyName = companyName;
        LogoUrl = logoUrl;
        Website = website;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        Address = address;
        GstNumber = gstNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsTrialActive()
    {
        return TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow;
    }

    public bool IsActive()
    {
        return Status == TenantStatus.Active;
    }
}
