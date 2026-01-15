namespace SuperEcomManager.Application.Common.Attributes;

/// <summary>
/// Attribute to specify required feature flag for a request.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute
{
    public string Feature { get; }

    public RequireFeatureAttribute(string feature)
    {
        Feature = feature;
    }
}
