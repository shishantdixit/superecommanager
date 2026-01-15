namespace SuperEcomManager.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a feature is disabled.
/// </summary>
public class FeatureDisabledException : Exception
{
    public string FeatureCode { get; }

    public FeatureDisabledException(string featureCode)
        : base($"Feature '{featureCode}' is not enabled for this tenant.")
    {
        FeatureCode = featureCode;
    }

    public FeatureDisabledException(string featureCode, string message)
        : base(message)
    {
        FeatureCode = featureCode;
    }
}
