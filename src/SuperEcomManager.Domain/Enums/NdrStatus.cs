namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// NDR (Non-Delivery Report) case status.
/// </summary>
public enum NdrStatus
{
    /// <summary>NDR case opened, pending action</summary>
    Open = 0,

    /// <summary>NDR assigned to an employee for follow-up</summary>
    Assigned = 1,

    /// <summary>Customer contacted, awaiting response</summary>
    CustomerContacted = 2,

    /// <summary>Reattempt scheduled with courier</summary>
    ReattemptScheduled = 3,

    /// <summary>Reattempt in progress</summary>
    ReattemptInProgress = 4,

    /// <summary>Successfully delivered on reattempt</summary>
    Delivered = 5,

    /// <summary>Customer refused delivery, RTO initiated</summary>
    RTOInitiated = 6,

    /// <summary>NDR case closed - delivered</summary>
    ClosedDelivered = 7,

    /// <summary>NDR case closed - RTO</summary>
    ClosedRTO = 8,

    /// <summary>NDR case closed - address updated and reattempted</summary>
    ClosedAddressUpdated = 9,

    /// <summary>NDR case escalated to higher level</summary>
    Escalated = 10
}
