using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Inventory;

/// <summary>
/// Records stock movement/changes for audit trail.
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid InventoryId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public MovementType MovementType { get; private set; }
    public int Quantity { get; private set; }
    public int QuantityBefore { get; private set; }
    public int QuantityAfter { get; private set; }
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public Guid? PerformedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public InventoryItem? Inventory { get; private set; }

    private StockMovement() { }

    public static StockMovement Create(
        Guid inventoryId,
        string sku,
        MovementType movementType,
        int quantity,
        int quantityBefore,
        int quantityAfter,
        Guid? userId = null,
        string? referenceType = null,
        string? referenceId = null,
        string? notes = null)
    {
        return new StockMovement
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            Sku = sku,
            MovementType = movementType,
            Quantity = quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            PerformedByUserId = userId,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
