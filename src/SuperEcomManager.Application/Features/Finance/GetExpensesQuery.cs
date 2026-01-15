using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Finance;

/// <summary>
/// Query to get paginated list of expenses.
/// </summary>
[RequirePermission("finance.view")]
[RequireFeature("finance_management")]
public record GetExpensesQuery : IRequest<Result<PaginatedResult<ExpenseListDto>>>, ITenantRequest
{
    public ExpenseFilterDto? Filter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool SortDescending { get; init; } = true;
}

public class GetExpensesQueryHandler : IRequestHandler<GetExpensesQuery, Result<PaginatedResult<ExpenseListDto>>>
{
    private readonly ITenantDbContext _dbContext;

    public GetExpensesQueryHandler(ITenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResult<ExpenseListDto>>> Handle(
        GetExpensesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Expenses.AsNoTracking();

        // Apply filters
        if (request.Filter != null)
        {
            var filter = request.Filter;

            if (filter.Category.HasValue)
                query = query.Where(e => e.Category == filter.Category.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(e => e.ExpenseDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(e => e.ExpenseDate <= filter.ToDate.Value);

            if (filter.MinAmount.HasValue)
                query = query.Where(e => e.Amount.Amount >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                query = query.Where(e => e.Amount.Amount <= filter.MaxAmount.Value);

            if (!string.IsNullOrWhiteSpace(filter.Vendor))
                query = query.Where(e => e.Vendor != null && e.Vendor.Contains(filter.Vendor));

            if (filter.IsRecurring.HasValue)
                query = query.Where(e => e.IsRecurring == filter.IsRecurring.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(e =>
                    e.Description.ToLower().Contains(searchTerm) ||
                    (e.Vendor != null && e.Vendor.ToLower().Contains(searchTerm)) ||
                    (e.InvoiceNumber != null && e.InvoiceNumber.ToLower().Contains(searchTerm)));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortDescending
            ? query.OrderByDescending(e => e.ExpenseDate)
            : query.OrderBy(e => e.ExpenseDate);

        // Apply pagination
        var expenses = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = expenses.Select(e => new ExpenseListDto
        {
            Id = e.Id,
            Category = e.Category,
            Amount = e.Amount.Amount,
            Currency = e.Amount.Currency,
            Description = e.Description,
            ExpenseDate = e.ExpenseDate,
            Vendor = e.Vendor,
            InvoiceNumber = e.InvoiceNumber,
            IsRecurring = e.IsRecurring,
            CreatedAt = e.CreatedAt
        }).ToList();

        var result = new PaginatedResult<ExpenseListDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);

        return Result<PaginatedResult<ExpenseListDto>>.Success(result);
    }
}
