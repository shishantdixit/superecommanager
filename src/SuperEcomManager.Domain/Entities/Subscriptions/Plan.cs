using SuperEcomManager.Domain.Common;

namespace SuperEcomManager.Domain.Entities.Subscriptions;

/// <summary>
/// Represents a subscription plan available in the platform.
/// Stored in shared schema.
/// </summary>
public class Plan : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public decimal YearlyPrice { get; private set; }
    public string Currency { get; private set; } = "INR";
    public int MaxUsers { get; private set; }
    public int MaxOrders { get; private set; }
    public int MaxChannels { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    private readonly List<PlanFeature> _planFeatures = new();
    public IReadOnlyCollection<PlanFeature> PlanFeatures => _planFeatures.AsReadOnly();

    private Plan() { }

    public static Plan Create(
        string name,
        string code,
        decimal monthlyPrice,
        decimal yearlyPrice,
        int maxUsers,
        int maxOrders,
        int maxChannels)
    {
        return new Plan
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code.ToLowerInvariant(),
            MonthlyPrice = monthlyPrice,
            YearlyPrice = yearlyPrice,
            MaxUsers = maxUsers,
            MaxOrders = maxOrders,
            MaxChannels = maxChannels,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
