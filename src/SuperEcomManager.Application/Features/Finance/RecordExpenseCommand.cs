using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Finance;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Domain.ValueObjects;

namespace SuperEcomManager.Application.Features.Finance;

/// <summary>
/// Command to record a new expense.
/// </summary>
[RequirePermission("finance.expenses.create")]
[RequireFeature("finance_management")]
public record RecordExpenseCommand : IRequest<Result<ExpenseDetailDto>>, ITenantRequest
{
    public ExpenseCategory Category { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Description { get; init; } = string.Empty;
    public DateTime ExpenseDate { get; init; }
    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? Vendor { get; init; }
    public string? InvoiceNumber { get; init; }
    public string? Notes { get; init; }
    public bool IsRecurring { get; init; }
}

public class RecordExpenseCommandHandler : IRequestHandler<RecordExpenseCommand, Result<ExpenseDetailDto>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RecordExpenseCommandHandler> _logger;

    public RecordExpenseCommandHandler(
        ITenantDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<RecordExpenseCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ExpenseDetailDto>> Handle(
        RecordExpenseCommand request,
        CancellationToken cancellationToken)
    {
        // Validate amount
        if (request.Amount <= 0)
        {
            return Result<ExpenseDetailDto>.Failure("Amount must be positive");
        }

        // Validate description
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return Result<ExpenseDetailDto>.Failure("Description is required");
        }

        // Validate expense date
        if (request.ExpenseDate > DateTime.UtcNow.AddDays(1))
        {
            return Result<ExpenseDetailDto>.Failure("Expense date cannot be in the future");
        }

        // Create the expense
        var money = new Money(request.Amount, request.Currency);
        var expense = Expense.Create(
            request.Category,
            money,
            request.Description,
            request.ExpenseDate,
            _currentUserService.UserId);

        // Set optional fields
        if (!string.IsNullOrWhiteSpace(request.ReferenceType) && request.ReferenceId.HasValue)
        {
            expense.SetReference(request.ReferenceType, request.ReferenceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Vendor) || !string.IsNullOrWhiteSpace(request.InvoiceNumber))
        {
            expense.SetVendorInfo(request.Vendor, request.InvoiceNumber);
        }

        // Handle notes and recurring flag through Update
        if (!string.IsNullOrWhiteSpace(request.Notes) || request.IsRecurring)
        {
            expense.Update(request.Category, money, request.Description, request.ExpenseDate, request.Notes);
        }

        _dbContext.Expenses.Add(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Expense recorded: {Category} - {Amount} {Currency} by user {UserId}",
            request.Category, request.Amount, request.Currency, _currentUserService.UserId);

        // Get user name
        string? recordedByUserName = null;
        if (expense.RecordedByUserId.HasValue)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == expense.RecordedByUserId.Value, cancellationToken);
            recordedByUserName = user?.FullName;
        }

        var dto = new ExpenseDetailDto
        {
            Id = expense.Id,
            Category = expense.Category,
            Amount = expense.Amount.Amount,
            Currency = expense.Amount.Currency,
            Description = expense.Description,
            ExpenseDate = expense.ExpenseDate,
            ReferenceType = expense.ReferenceType,
            ReferenceId = expense.ReferenceId,
            Vendor = expense.Vendor,
            InvoiceNumber = expense.InvoiceNumber,
            Notes = expense.Notes,
            IsRecurring = expense.IsRecurring,
            RecordedByUserId = expense.RecordedByUserId,
            RecordedByUserName = recordedByUserName,
            CreatedAt = expense.CreatedAt,
            UpdatedAt = expense.UpdatedAt
        };

        return Result<ExpenseDetailDto>.Success(dto);
    }
}
