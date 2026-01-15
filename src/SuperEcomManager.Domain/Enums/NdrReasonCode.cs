namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// NDR reason codes provided by courier.
/// </summary>
public enum NdrReasonCode
{
    /// <summary>Customer not available at address</summary>
    CustomerNotAvailable = 1,

    /// <summary>Customer refused delivery</summary>
    CustomerRefused = 2,

    /// <summary>Address incomplete or incorrect</summary>
    IncorrectAddress = 3,

    /// <summary>Customer requested future delivery</summary>
    FutureDeliveryRequested = 4,

    /// <summary>Customer not contactable (phone unreachable)</summary>
    CustomerUnreachable = 5,

    /// <summary>Shop/office closed</summary>
    PremisesClosed = 6,

    /// <summary>Customer out of station</summary>
    CustomerOutOfStation = 7,

    /// <summary>Payment not ready (for COD)</summary>
    CODNotReady = 8,

    /// <summary>Customer wants to change delivery address</summary>
    AddressChangeRequested = 9,

    /// <summary>Product damaged during transit</summary>
    ProductDamaged = 10,

    /// <summary>Customer wants open delivery (to check product)</summary>
    OpenDeliveryRequested = 11,

    /// <summary>Security/access issue at location</summary>
    SecurityRestriction = 12,

    /// <summary>Weather/natural calamity</summary>
    WeatherIssue = 13,

    /// <summary>Other reason</summary>
    Other = 99
}
