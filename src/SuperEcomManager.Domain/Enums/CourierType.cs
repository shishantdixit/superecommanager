namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Courier/shipping provider types.
/// </summary>
public enum CourierType
{
    /// <summary>Shiprocket aggregator (default)</summary>
    Shiprocket = 1,

    /// <summary>Delhivery direct</summary>
    Delhivery = 2,

    /// <summary>BlueDart direct</summary>
    BlueDart = 3,

    /// <summary>DTDC direct</summary>
    DTDC = 4,

    /// <summary>Ecom Express direct</summary>
    EcomExpress = 5,

    /// <summary>XpressBees direct</summary>
    XpressBees = 6,

    /// <summary>Shadowfax direct</summary>
    Shadowfax = 7,

    /// <summary>Custom courier integration</summary>
    Custom = 99
}
