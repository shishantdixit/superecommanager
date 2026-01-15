namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Categories for business expenses.
/// </summary>
public enum ExpenseCategory
{
    /// <summary>Shipping and courier costs</summary>
    Shipping = 1,

    /// <summary>Platform/marketplace fees</summary>
    PlatformFees = 2,

    /// <summary>Payment gateway charges</summary>
    PaymentProcessing = 3,

    /// <summary>Packaging materials</summary>
    Packaging = 4,

    /// <summary>Returns and refunds</summary>
    Returns = 5,

    /// <summary>RTO (Return to Origin) costs</summary>
    RTO = 6,

    /// <summary>Marketing and advertising</summary>
    Marketing = 7,

    /// <summary>Software and subscriptions</summary>
    Software = 8,

    /// <summary>Warehouse and storage</summary>
    Warehouse = 9,

    /// <summary>Employee salaries</summary>
    Salaries = 10,

    /// <summary>Office and utilities</summary>
    OfficeExpenses = 11,

    /// <summary>Taxes and duties</summary>
    Taxes = 12,

    /// <summary>Miscellaneous expenses</summary>
    Other = 99
}
