namespace SuperEcomManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleViolationException(string ruleName, string message)
        : base("BUSINESS_RULE_VIOLATION", message)
    {
        RuleName = ruleName;
    }

    /// <summary>
    /// Creates exception for order-related business rule violations.
    /// </summary>
    public static BusinessRuleViolationException ForOrder(string orderId, string message)
    {
        return new BusinessRuleViolationException(
            $"OrderRule:{orderId}",
            message);
    }

    /// <summary>
    /// Creates exception for shipment-related business rule violations.
    /// </summary>
    public static BusinessRuleViolationException ForShipment(string message)
    {
        return new BusinessRuleViolationException("ShipmentRule", message);
    }

    /// <summary>
    /// Creates exception for inventory-related business rule violations.
    /// </summary>
    public static BusinessRuleViolationException ForInventory(string sku, string message)
    {
        return new BusinessRuleViolationException(
            $"InventoryRule:{sku}",
            message);
    }
}
