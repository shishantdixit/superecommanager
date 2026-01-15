namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Inventory movement/stock change types.
/// </summary>
public enum MovementType
{
    /// <summary>Initial stock entry</summary>
    InitialStock = 1,

    /// <summary>Stock added (purchase/restock)</summary>
    StockIn = 2,

    /// <summary>Stock removed (sale/shipment)</summary>
    StockOut = 3,

    /// <summary>Stock returned (RTO/customer return)</summary>
    Return = 4,

    /// <summary>Stock adjustment (correction, damage, loss)</summary>
    Adjustment = 5,

    /// <summary>Stock reserved for order</summary>
    Reserved = 6,

    /// <summary>Reserved stock released</summary>
    ReserveReleased = 7,

    /// <summary>Stock transferred between locations</summary>
    Transfer = 8,

    /// <summary>Stock damaged/written off</summary>
    Damaged = 9
}
