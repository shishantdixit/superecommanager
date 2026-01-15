namespace SuperEcomManager.Domain.Entities.Subscriptions;

/// <summary>
/// Join table between Plan and Feature.
/// </summary>
public class PlanFeature
{
    public Guid PlanId { get; private set; }
    public Guid FeatureId { get; private set; }

    public Plan? Plan { get; private set; }
    public Feature? Feature { get; private set; }

    private PlanFeature() { }

    public PlanFeature(Guid planId, Guid featureId)
    {
        PlanId = planId;
        FeatureId = featureId;
    }
}
