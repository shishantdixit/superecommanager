namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Types of actions taken for NDR follow-up.
/// </summary>
public enum NdrActionType
{
    /// <summary>Phone call to customer</summary>
    PhoneCall = 1,

    /// <summary>WhatsApp message sent</summary>
    WhatsAppMessage = 2,

    /// <summary>SMS sent</summary>
    SMS = 3,

    /// <summary>Email sent</summary>
    Email = 4,

    /// <summary>Reattempt requested with courier</summary>
    ReattemptRequested = 5,

    /// <summary>Address updated in system</summary>
    AddressUpdated = 6,

    /// <summary>RTO initiated</summary>
    RTOInitiated = 7,

    /// <summary>Case escalated</summary>
    Escalated = 8,

    /// <summary>Internal remark/note added</summary>
    RemarkAdded = 9,

    /// <summary>Case reassigned to another employee</summary>
    Reassigned = 10,

    /// <summary>Customer callback scheduled</summary>
    CallbackScheduled = 11
}
