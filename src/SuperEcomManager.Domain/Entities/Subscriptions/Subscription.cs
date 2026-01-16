using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Subscriptions;

/// <summary>
/// Represents a tenant's subscription to a plan.
/// Stored in shared schema.
/// </summary>
public class Subscription : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public bool IsYearly { get; private set; }
    public decimal PriceAtSubscription { get; private set; }
    public string Currency { get; private set; } = "INR";

    public Plan? Plan { get; private set; }

    private Subscription() { }

    public static Subscription CreateTrial(Guid tenantId, Guid planId, int trialDays = 14)
    {
        return new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow,
            TrialEndsAt = DateTime.UtcNow.AddDays(trialDays),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate(decimal price, bool isYearly)
    {
        Status = SubscriptionStatus.Active;
        PriceAtSubscription = price;
        IsYearly = isYearly;
        EndDate = isYearly ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);
        TrialEndsAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Renew(decimal price, bool isYearly)
    {
        Status = SubscriptionStatus.Active;
        PriceAtSubscription = price;
        IsYearly = isYearly;
        StartDate = EndDate ?? DateTime.UtcNow;
        EndDate = isYearly ? StartDate.AddYears(1) : StartDate.AddMonths(1);
        CancelledAt = null;
        CancellationReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePlan(Guid newPlanId, decimal price, bool isYearly)
    {
        PlanId = newPlanId;
        PriceAtSubscription = price;
        IsYearly = isYearly;
        // Keep current end date for upgrades/downgrades
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pause()
    {
        Status = SubscriptionStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resume()
    {
        if (Status == SubscriptionStatus.Paused)
        {
            Status = SubscriptionStatus.Active;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkPastDue()
    {
        Status = SubscriptionStatus.PastDue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired => Status == SubscriptionStatus.Expired ||
                             (EndDate.HasValue && EndDate.Value < DateTime.UtcNow);

    public bool IsInTrial => Status == SubscriptionStatus.Trial &&
                             TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow;
}
