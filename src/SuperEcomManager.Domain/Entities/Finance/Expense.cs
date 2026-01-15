using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Domain.Entities.Finance;

/// <summary>
/// Represents a business expense.
/// </summary>
public class Expense : AuditableEntity, ISoftDeletable
{
    public ExpenseCategory Category { get; private set; }
    public Money Amount { get; private set; } = Money.Zero;
    public string Description { get; private set; } = string.Empty;
    public DateTime ExpenseDate { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Vendor { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public string? Notes { get; private set; }
    public bool IsRecurring { get; private set; }
    public Guid? RecordedByUserId { get; private set; }

    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private Expense() { }

    public static Expense Create(
        ExpenseCategory category,
        Money amount,
        string description,
        DateTime expenseDate,
        Guid? recordedByUserId = null)
    {
        return new Expense
        {
            Id = Guid.NewGuid(),
            Category = category,
            Amount = amount,
            Description = description,
            ExpenseDate = expenseDate,
            RecordedByUserId = recordedByUserId,
            IsRecurring = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetReference(string referenceType, Guid referenceId)
    {
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVendorInfo(string? vendor, string? invoiceNumber)
    {
        Vendor = vendor;
        InvoiceNumber = invoiceNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        ExpenseCategory category,
        Money amount,
        string description,
        DateTime expenseDate,
        string? notes)
    {
        Category = category;
        Amount = amount;
        Description = description;
        ExpenseDate = expenseDate;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
